using CsvHelper.Configuration.Attributes;

namespace FergusonUPSIntegration.Models
{
    public class TrackingNumberFile
    {
        [Name("trackingNum")]
        public string TrackingNumber { get; set; }
    }
}
