using System;
using System.Collections.Generic;
using System.Text;

namespace FergusonUPSIntegration.Core.Models
{
    public class ShipToAddress
    {
        public string AddressLine1 { get; set; }

        public string City { get; set; }

        public string State { get; set; }

        public string Zip { get; set; }
    }
}
