
namespace FergusonUPSIntegration.Models
{
    public class LatestActivity
    {
        public string Status { get; set; }

        public string TimeStamp { get; set; }

        public string Location { get; set; }

        public string? ExceptionReason { get; set; } = null;
    }
}
