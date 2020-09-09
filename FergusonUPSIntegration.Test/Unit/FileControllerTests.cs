using System;
using System.Collections.Generic;
using System.Text;
using Xunit;
using TrackingNumbers.Controllers;
using Microsoft.Extensions.Logging;
using FergusonUPSIntegration.Test.Helpers;
using Microsoft.Azure.Storage;
using Microsoft.Azure.Storage.Blob;
using System.IO;
using System.Linq;
using System.Threading.Tasks;

namespace FergusonUPSIntegration.Test.Unit
{
    public class FileControllerTests
    {
        public FileControllerTests()
        {
            fileController = new FileController(logger, "");
        }

        private readonly ILogger logger = TestHelpers.CreateLogger();
        private FileController fileController { get; set; }


        [Fact]
        public void Test_CreateInvalidTrackingNumberReport()
        {
            var invalidTrackingNumbers = new List<string>() { "1Z78451497898", "1Z89725179614", "1Z7845348122" };

            var reportName = fileController.CreateInvalidTrackingNumberReport(invalidTrackingNumbers);

            var blob = GetBlobByName("invalid-tracking-numbers", reportName);

            var trackingNumbers = GetTrackingNumbersFromFile(blob);

            Assert.Equal(invalidTrackingNumbers, trackingNumbers);

            blob.Delete();
        }


        [Fact]
        public async void Test_MoveProcessedFileToArchive()
        {
            // create file in ups-tracking
            var newTrackingNumbers = new List<string>() { "1Z78451497898", "1Z89725179614", "1Z7845348122" };
            var fileName = "Tracking Numbers Test.csv";
            var trackingNumbersBlob = GetBlobByName("ups-tracking-numbers", fileName);

            WriteCSVBlobFromList(trackingNumbersBlob, newTrackingNumbers);

            // move to archived
            var archiveBlob = await fileController.MoveProcessedFileToArchive(fileName.Replace(".csv", ""));

            // get file from archive
            var archivedTrackingNumbers = GetTrackingNumbersFromFile(archiveBlob);

            Assert.Equal(newTrackingNumbers, archivedTrackingNumbers);

            archiveBlob.Delete();
        }


        private CloudBlockBlob GetBlobByName(string containerName, string blobName)
        {
            var storageAccount = CloudStorageAccount.Parse(Environment.GetEnvironmentVariable("UPS_BLOB_CONN"));
            var blobClient = storageAccount.CreateCloudBlobClient();

            var container = blobClient.GetContainerReference(containerName);

            var blob = container.GetBlockBlobReference(blobName);

            return blob;
        }


        private void WriteCSVBlobFromList(CloudBlockBlob blob, List<string> content)
        {
            blob.Properties.ContentType = "text/csv";

            var csvFileContent = string.Join("\n", content);

            blob.UploadText(csvFileContent);
        }


        private List<string> GetTrackingNumbersFromFile(CloudBlockBlob blob)
        {
            var memoryStream = new MemoryStream();

            // Downloads blob's content to a stream
            blob.DownloadToStream(memoryStream);

            // Puts the byte arrays to a string
            var csvText = Encoding.UTF8.GetString(memoryStream.ToArray());

            var trackingNumbers = csvText.Split("\n").ToList();

            return trackingNumbers;
        }
    }
}
