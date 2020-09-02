using System;
using System.Collections.Generic;
using System.Text;

namespace FergusonUPSIntregrationCore.Models
{
    public class TrackingDetails
    {
        public string ShipmentStatus { get; set; }

        public string LastestActivity { get; set; }

        public DateTime EstimatedDeliveryDate { get; set; }

        public DateTime actualDeliveryDateAndTime { get; set; }

        public DateTime PickUpDate { get; set; }

        public DateTime OriginScanDateAndTime { get; set; }

        public double ShippingWeight { get; set; }

        public string Service { get; set; }

        public string ReferenceNumberOne { get; set; }

        public string ReferenceNumberTwo { get; set; }

    }
}
