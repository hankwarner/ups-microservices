

namespace UPSMicroservices.Models
{
    public class UPSTracking
    {
        public string TrackingNumber { get; set; }

        public string? ReferenceNumber { get; set; } = null;

        public string? OriginAddress { get; set; } = null;

        public string? OriginCity { get; set; } = null;

        public string? OriginState { get; set; } = null;

        public string? OriginZip { get; set; } = null;

        public string? DestinationCity { get; set; } = null;

        public string? DestinationState { get; set; } = null;

        public string? DestinationZip { get; set; } = null;

        public string? Status { get; set; } = null;

        public string? Location { get; set; } = null;

        public string TimeStamp { get; set; }

        public string? ExceptionReason { get; set; } = null;
    }
}
