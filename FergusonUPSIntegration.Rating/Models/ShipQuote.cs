using System;
using System.Collections.Generic;
using System.Text;

namespace FergusonUPSIntegration.Rating.Models
{
    public class ShipQuoteRequest
    {
        public ShipToAddress DestinationAddress { get; set; }

        public ShipToAddress OriginAddress { get; set; }

        public Package Package { get; set; }
    }


    //public class ShipQuoteResponse
    //{
        
    //}
}
