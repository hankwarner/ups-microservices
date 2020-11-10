using System;
using System.Collections.Generic;
using System.Globalization;
using System.IO;
using System.Linq;
using CsvHelper;
using Microsoft.Extensions.Logging;
using Microsoft.Azure.Storage;
using Microsoft.Azure.Storage.Blob;
using System.Threading.Tasks;
using UPSMicroservices.Models;

namespace UPSMicroservices.Controllers
{
    public class FileController
    {
        public FileController(ILogger logger, string logsUrl)
        {
            _logger = logger;
            errorLogsUrl = logsUrl;
            storageAccount = CloudStorageAccount.Parse(Environment.GetEnvironmentVariable("UPS_BLOB_CONN"));
        }

        public FileController()
        {
            storageAccount = CloudStorageAccount.Parse(Environment.GetEnvironmentVariable("UPS_BLOB_CONN"));
        }

        private ILogger _logger { get; set; }
        public string errorLogsUrl { get; set; }
        CloudStorageAccount storageAccount { get; set; }


        /// <summary>
        ///     Reads the CSV file containing new UPS tracking numbers to be added to the UPS Integration tables.
        /// </summary>
        /// <param name="blob">The Azure Blob stream which has been triggered by a CSV add.</param>
        /// <returns>UPS Tracking data objects with the tracking number property assigned for each tracking number from the CSV.</returns>
        public IEnumerable<UPSTracking> GetTrackingNumbersFromFile(Stream blob)
        {
            try
            {
                var trackingNumbers = new List<TrackingNumberFile>();

                using (var reader = new StreamReader(blob))
                using (var csv = new CsvReader(reader, CultureInfo.InvariantCulture))
                {
                    trackingNumbers = csv.GetRecords<TrackingNumberFile>().ToList();
                }

                var trackingRecords = trackingNumbers
                    .Select(tn => new UPSTracking()
                    {
                        TrackingNumber = tn.TrackingNumber
                    });

                return trackingRecords;
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error in GetTrackingNumbersFromFile");
                throw;
            }
        }



        /// <summary>
        ///     Writes a new CSV file to invalid-tracking-numbers blob container.
        /// </summary>
        /// <param name="invalidTrackingNumbers">List of tracking numbers that were not recognized by UPS to write to CSV file.</param>
        public string CreateInvalidTrackingNumberReport(List<InvalidTrackingNumber> invalidTrackingNumbers)
        {
            var fileName = "";

            try
            {
                var client = storageAccount.CreateCloudBlobClient();
                var container = client.GetContainerReference("invalid-tracking-numbers");
                
                var today = DateTime.Now.ToString("MM.dd.yyyy.HH.mm.ss");
                fileName = $"InvalidTrackingNumbers{today}.csv";

                var blob = container.GetBlockBlobReference(fileName);

                // Format list as text for csv of rows of tracking numbers
                blob.Properties.ContentType = "text/csv";
                var csvFileContent = "TrackingNumber,Exception\n";

                invalidTrackingNumbers.ForEach(line => csvFileContent += line.TrackingNumber + "," + line.Exception + "\n");

                blob.UploadText(csvFileContent);
            }
            catch (Exception ex)
            {
                var title = "Error in CreateInvalidTrackingNumberReport";
                var text = $"Error message: {ex.Message}. Stacktrace: {ex.StackTrace}";
                var teamsMessage = new TeamsMessage(title, text, "yellow", errorLogsUrl);
                teamsMessage.LogToTeams(teamsMessage);
                _logger.LogError(ex, title);
            }

            return fileName;
        }


        /// <summary>
        ///     Moves the processed file from the ups-tracking-numbers container to the ups-tracking-numbers-archive container.
        /// </summary>
        /// <param name="fileName">Name of the file that was added to the source container.</param>
        public async Task<CloudBlockBlob> MoveProcessedFileToArchive(string fileName)
        {
            try
            {
                var blobClient = storageAccount.CreateCloudBlobClient();

                var trackingNumbersContainer = blobClient.GetContainerReference("ups-tracking-numbers");
                var tn = trackingNumbersContainer.Exists();
                var archiveContainer = blobClient.GetContainerReference("ups-tracking-numbers-archive");
                var arch = archiveContainer.Exists();

                var srcBlob = trackingNumbersContainer.GetBlockBlobReference(fileName + ".csv");
                var destBlob = archiveContainer.GetBlockBlobReference(fileName + ".csv");

                var srcExists = srcBlob.Exists();
                var destExists = destBlob.Exists();

                // Copy the file from the source container to the ups-tracking-numbers-archive container
                await destBlob.StartCopyAsync(srcBlob);

                // Delete file from ups-tracking-numbers container
                await srcBlob.DeleteAsync();

                return destBlob;

            }
            catch (Exception ex)
            {
                var title = "Error in AddNewTrackingNumbers";
                var text = $"Error message: {ex.Message}. Stacktrace: {ex.StackTrace}";
                var teamsMessage = new TeamsMessage(title, text, "yellow", errorLogsUrl);
                teamsMessage.LogToTeams(teamsMessage);
                _logger.LogError(ex, title);
                throw;
            }
        }
    }
}
