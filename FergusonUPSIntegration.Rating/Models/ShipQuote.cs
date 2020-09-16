using System;
using System.Collections.Generic;
using System.Text;

namespace FergusonUPSIntegration.Rating.Models
{
    public class ShipQuoteRequest
    {
        public Address DestinationAddress { get; set; }

        public Address OriginAddress { get; set; }

        public Package Package { get; set; }
    }


    //public class ShipQuoteResponse
    //{
        
    //}
}
