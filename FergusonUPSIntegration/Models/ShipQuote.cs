using System.Collections.Generic;

namespace FergusonUPSIntegration.Models
{
    public class ShipQuoteRequest
    {
        public string RateType { get; set; }

        public ShipToAddress DestinationAddress { get; set; }

        public ShipToAddress OriginAddress { get; set; }

        public PackageRequest Package { get; set; }
    }


    public class ShipQuoteResponse
    {
        // Key = rate type (ground, second day air, next day air), Value = estimated cost of shipment
        public Dictionary<string, double> Rates { get; set; }
    }
}
