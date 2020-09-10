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
using FergusonUPSIntegration.Core.Models;
using System.Runtime.Serialization.Formatters.Binary;
using System.Net;
using CsvHelper;
using System.Globalization;

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
            var invalidTrackingNumbers = CreateInvalidTrackingNumbers();

            var reportName = fileController.CreateInvalidTrackingNumberReport(invalidTrackingNumbers);

            var blob = GetBlobByName("invalid-tracking-numbers", reportName);

            var invalidTrackingNumberData = GetInvalidTrackingNumberDataFromBlob(blob);

            Assert.Equal(invalidTrackingNumbers, invalidTrackingNumberData);

            blob.Delete();
        }


        [Fact]
        public async void Test_MoveProcessedFileToArchive()
        {
            // create file in ups-tracking
            var newTrackingNumbers = CreateInvalidTrackingNumbers();
            var fileName = "Tracking Numbers Test.csv";
            var trackingNumbersBlob = GetBlobByName("ups-tracking-numbers", fileName);

            WriteCSVBlobFromList(trackingNumbersBlob, newTrackingNumbers);

            // move to archived
            var archiveBlob = await fileController.MoveProcessedFileToArchive(fileName.Replace(".csv", ""));

            // get file from archive
            var archivedTrackingNumberData = GetInvalidTrackingNumberDataFromBlob(archiveBlob);

            Assert.Equal(newTrackingNumbers, archivedTrackingNumberData);

            archiveBlob.Delete();
        }


        private List<InvalidTrackingNumber> CreateInvalidTrackingNumbers()
        {
            var invalidTrackingNumbers = new List<InvalidTrackingNumber>()
            {
                new InvalidTrackingNumber("1Z78451497898", "No tracking information available."),
                new InvalidTrackingNumber("1Z7845348122", "Tracking number does not exist.")
            };

            return invalidTrackingNumbers;
        }


        private CloudBlockBlob GetBlobByName(string containerName, string blobName)
        {
            var storageAccount = CloudStorageAccount.Parse(Environment.GetEnvironmentVariable("UPS_BLOB_CONN"));
            var blobClient = storageAccount.CreateCloudBlobClient();

            var container = blobClient.GetContainerReference(containerName);

            var blob = container.GetBlockBlobReference(blobName);

            return blob;
        }


        private void WriteCSVBlobFromList(CloudBlockBlob blob, List<InvalidTrackingNumber> content)
        {
            blob.Properties.ContentType = "text/csv";

            var csvFileContent = string.Join("\n", content);

            blob.UploadText(csvFileContent);
        }


        private List<InvalidTrackingNumber> GetInvalidTrackingNumberDataFromBlob(CloudBlockBlob blob)
        {
            var wc = new WebClient();
            var invalidTrackingNumberData = new List<InvalidTrackingNumber>();

            using (var sourceStream = wc.OpenRead(blob.Uri))
            using (var reader = new StreamReader(sourceStream))
            using (var csv = new CsvReader(reader, CultureInfo.InvariantCulture))
            {
                invalidTrackingNumberData = csv.GetRecords<InvalidTrackingNumber>().ToList();
            }

            return invalidTrackingNumberData;
        }
    }
}
