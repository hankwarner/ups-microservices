
using static UPSMicroservices.Models.UPSTimeInTransit;

namespace UPSMicroservices.Models
{
    public class TimeInTransitResponse
    {
        public TimeInTransitResponse(Service groundService)
        {
            BusinessTransitDays = groundService.BusinessTransitDays;
            SaturdayDelivery = groundService.SaturdayDeliveryIndicator == "1"; 
        }

        public int BusinessTransitDays { get; set; }

        public bool SaturdayDelivery { get; set; }
    }
}
