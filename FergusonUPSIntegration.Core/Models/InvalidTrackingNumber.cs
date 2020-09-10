using System;
using System.Collections.Generic;
using System.Text;
using CsvHelper.Configuration.Attributes;
using CsvHelper.Configuration;

namespace FergusonUPSIntegration.Core.Models
{
    public class InvalidTrackingNumber
    {
        public InvalidTrackingNumber(string TrackingNumber, string Exception)
        {
            this.TrackingNumber = TrackingNumber;
            this.Exception = Exception;
        }

        public string TrackingNumber { get; set; }

        public string Exception { get; set; }
    }
}
