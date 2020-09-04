using FergusonUPSIntegrationCore.Models;
using FergusonUPSIntegrationCore;
using System;
using Xunit;

namespace FergusonUPSIntegration.Test
{
    public class TrackAPITests
    {
        private const string realTrackingNumber = "1Z1F378R0394454252";
        private const string fakeTrackingNumber = "1Z1111111111111111";


        [Fact]
        public void Test_GetAddressByType_Origin()
        {
            var addresses = CreateAddresses();

            var upsResponse = CreateUPSResponse(addresses);

            var originAddress = TrackAPI.GetAddressByType(upsResponse, "Shipper Address");

            Assert.Equal(addresses[0], originAddress);
        }


        [Fact]
        public void Test_GetAddressByType_Desitation()
        {
            var addresses = CreateAddresses();

            var upsResponse = CreateUPSResponse(addresses);

            var destinationAddress = TrackAPI.GetAddressByType(upsResponse, "ShipTo Address");

            Assert.Equal(addresses[1], destinationAddress);
        }


        [Fact]
        public void Test_GetReferenceNum()
        {
            var refNum = "T3348-1122734";

            var upsResponse = CreateUPSResponse(refNum);

            var refNumResponse = TrackAPI.GetReferenceNum(upsResponse);

            Assert.Equal(refNum, refNumResponse);
        }


        [Fact]
        public void Test_InsertTrackingNumberLines_TrackingNumber()
        {
            //var upsData = CreateUPSTrackingData(fakeTrackingNumber);
            //upsData.ReferenceNumber = "RTM-3108";
            //upsData.OriginAddress = "W 990 ALGONQUIN RD";
            //upsData.OriginCity = "ARLINGTON HEIGHTS";
            //upsData.OriginState = "IL";
            //upsData.OriginZip = "600053503";

            //UPSHelpers.TrackAPI.InsertLineToUPSTable(upsData, "TrackingNumbers");

            //Assert.NotNull(GetLineByTrackingNumber(fakeTrackingNumber, "TrackingNumbers"));

            //DeleteTrackingDataLine(fakeTrackingNumber, "TrackingNumbers");
        }


        public ShipmentAddress[] CreateAddresses()
        {
            var originAddress = new ShipmentAddress()
            {
                Type = new ResponseStatus() { Description = "Shipper Address" },
                Address = new Address()
                {
                    AddressLine = "1076 De Haro St",
                    City = "San Francisco",
                    StateProvinceCode = "CA",
                    PostalCode = "94107"
                }
            };

            var destinationAddress = new ShipmentAddress()
            {
                Type = new ResponseStatus() { Description = "ShipTo Address" },
                Address = new Address()
                {
                    AddressLine = "1602 Adams St",
                    City = "Denver",
                    StateProvinceCode = "CO",
                    PostalCode = "80206"
                }
            };

            return new ShipmentAddress[2] { originAddress, destinationAddress };
        }


        public UPSResponse CreateUPSResponse(ShipmentAddress[] addresses)
        {
            var upsResponse = new UPSResponse()
            {
                TrackResponse = new TrackResponse()
                {
                    Shipment = new Shipment()
                    {
                        ShipmentAddress = CreateAddresses(),
                        ReferenceNumber = new ReferenceNumber()
                        {
                            Code = "01",
                            Value = "T3348-1122734"
                        }
                    }
                }
            };

            return upsResponse;
        }


        public UPSResponse CreateUPSResponse(string refNum)
        {
            var upsResponse = new UPSResponse()
            {
                TrackResponse = new TrackResponse()
                {
                    Shipment = new Shipment()
                    {
                        ReferenceNumber = new ReferenceNumber()
                        {
                            Code = "01",
                            Value = refNum
                        }
                    }
                }
            };

            return upsResponse;
        }
    }
}
