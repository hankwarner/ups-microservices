using CsvHelper.Configuration.Attributes;

namespace FergusonUPSIntregrationCore.Models
{
    public class TrackingNumberFile
    {
        [Name("trackingNum")]
        public string TrackingNumber { get; set; }
    }
}
