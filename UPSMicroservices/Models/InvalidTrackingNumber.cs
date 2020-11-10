
namespace UPSMicroservices.Models
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
