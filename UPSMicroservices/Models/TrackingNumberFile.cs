using CsvHelper.Configuration.Attributes;

namespace UPSMicroservices.Models
{
    public class TrackingNumberFile
    {
        [Name("trackingNum")]
        public string TrackingNumber { get; set; }
    }
}
