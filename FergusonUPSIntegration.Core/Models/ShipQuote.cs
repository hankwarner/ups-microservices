using System;
using System.Collections.Generic;
using System.Text;

namespace FergusonUPSIntegration.Core.Models
{
    public class ShipQuoteRequest
    {
        public ShipToAddress DestinationAddress { get; set; }

        public ShipToAddress OriginAddress { get; set; }

        public PackageRequest Package { get; set; }
    }


    //public class ShipQuoteResponse
    //{
        
    //}
}
