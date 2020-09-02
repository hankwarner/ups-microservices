using System;
using System.Collections.Generic;
using System.Globalization;
using System.IO;
using System.Linq;
using FergusonUPSIntregrationCore.Models;
using CsvHelper;
using Microsoft.Extensions.Logging;

namespace TrackingNumbers.Controllers
{
    public class FileController
    {
        public FileController(ILogger logger)
        {
            _logger = logger;
        }


        private ILogger _logger { get; set; }


        /// <summary>
        ///     Reads the CSV file containing new UPS tracking numbers to be added to the UPS Integration tables.
        /// </summary>
        /// <param name="blob">The Azure Blob stream which has been triggered by a CSV add.</param>
        /// <returns>UPS Tracking data objects with the tracking number property assigned for each tracking number from the CSV.</returns>
        public IEnumerable<UPSTracking> GetTrackingNumbersFromFile(Stream blob)
        {
            try
            {
                List<TrackingNumberFile> trackingNumbers = new List<TrackingNumberFile>();

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
    }
}
