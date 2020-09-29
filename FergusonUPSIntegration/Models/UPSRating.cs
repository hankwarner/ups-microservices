using System;
using System.Collections.Generic;
using RateWebService;

namespace FergusonUPSIntegration.Models
{
    public class UPSRating
    {
        public UPSRating(string rateType, ShipToAddress origin, ShipToAddress destination, PackageRequest package)
        {
            rateRequest = CreateNewRateRequest(rateType, origin, destination, package);
        }

        public RateRequest rateRequest { get; set; }
        public RateResponse1 rateResponse { get; set; } = new RateResponse1();
        public static List<string> rateTypes = new List<string>() { "next day air", "second day air", "ground", "next day air saver", "next day air early", "second day air a.m." };


        public double TotalShipCost
        {
            get
            {
                if (rateResponse.RateResponse.RatedShipment.Length == 0)
                {
                    throw new ArgumentNullException("RatedShipment", "Please set the rateResponse to the UPS Rating API response.");
                }

                return double.Parse(rateResponse.RateResponse.RatedShipment[0].TotalCharges.MonetaryValue);
            }
        }


        private static RateRequest CreateNewRateRequest(string rateType, ShipToAddress origin, ShipToAddress destination, PackageRequest package)
        {
            var request = new RequestType()
            {
                RequestOption = new string[] { "Rate" }
            };

            var shipperAddress = new AddressType()
            {
                City = origin.City,
                PostalCode = origin.Zip,
                StateProvinceCode = origin.State,
                CountryCode = "US"
            };

            var shipper = new ShipperType()
            {
                ShipperNumber = Environment.GetEnvironmentVariable("UPS_ACCT_NUM"),
                Address = shipperAddress
            };

            var shipToAddress = new ShipToAddressType()
            {
                AddressLine = new string[] { destination.AddressLine1 },
                City = destination.City,
                PostalCode = destination.Zip,
                StateProvinceCode = destination.State,
                CountryCode = "US"
            };

            var shipTo = new ShipToType()
            {
                Address = shipToAddress
            };

            var service = new CodeDescriptionType()
            {
                Code = GetServiceCode(rateType)
            };

            var pkgArray = CreatePackage(package);

            var shipment = new ShipmentType()
            {
                Shipper = shipper,
                ShipTo = shipTo,
                Service = service,
                Package = pkgArray
            };

            var rateRequest = new RateRequest()
            {
                Request = request,
                Shipment = shipment
            };

            return rateRequest;
        }


        private static PackageType[] CreatePackage(PackageRequest package)
        {
            var packageWeight = new PackageWeightType()
            {
                Weight = package.Weight.ToString(),
                UnitOfMeasurement = new CodeDescriptionType()
                {
                    Code = "LBS"
                }
            };

            var packageType = new PackageType()
            {
                PackageWeight = packageWeight,
                PackagingType = new CodeDescriptionType()
                {
                    Code = "02", // standard package
                }
            };

            PackageType[] pkgArray = { packageType };

            return pkgArray;
        }


        private static string GetServiceCode(string rateType)
        {
            string serviceCode;

            switch (rateType)
            {
                case "next day air":
                    serviceCode = "01";
                    break;
                case "second day air":
                    serviceCode = "02";
                    break;
                case "ground":
                    serviceCode = "03";
                    break;
                case "next day air saver":
                    serviceCode = "13";
                    break;
                case "next day air early":
                    serviceCode = "14";
                    break;
                case "second day air a.m.":
                    serviceCode = "59";
                    break;
                default:
                    serviceCode = "03";
                    break;
            }

            return serviceCode;
        }
    }
}
