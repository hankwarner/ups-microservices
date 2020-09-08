using FergusonUPSIntegrationCore.Models;
using FergusonUPSIntegrationCore;
using System;
using Xunit;
using FergusonUPSIntregrationCore.Models;

namespace FergusonUPSIntegration.Test
{
    public class TrackAPITests
    {
        [Fact]
        public void Test_GetAddressByType_Origin()
        {
            var addresses = CreateAddresses();

            var upsResponse = CreateUPSResponse();

            var originAddress = TrackAPI.GetAddressByType(upsResponse, "Shipper Address");

            Assert.Equal(addresses[0], originAddress);
        }


        [Fact]
        public void Test_GetAddressByType_Desitation()
        {
            var addresses = CreateAddresses();

            var upsResponse = CreateUPSResponse();

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
        public void Test_GetLatestActivity()
        {
            var upsResponse = CreateUPSResponse();

            var status = CreateInTransitStatus();

            AddShipmentActivity(upsResponse, status);

            var latestActivity = TrackAPI.GetLatestActivity(upsResponse);

            Assert.Equal("San Francisco, CA", latestActivity.Location);
            Assert.Equal("In Transit", latestActivity.Status);
        }


        [Fact]
        public void Test_GetLatestActivity_Exception()
        {
            var upsResponse = CreateUPSResponse();

            var status = CreateExceptionStatus();

            AddShipmentActivity(upsResponse, status);

            var latestActivity = TrackAPI.GetLatestActivity(upsResponse);

            Assert.Equal("Were attempting to verify the package location. / Claim issued.", latestActivity.ExceptionReason);
        }


        [Fact]
        public void Test_GetUPSTrackingData()
        {
            var trackingNumber = "1ZV637F80311397302";

            var upsRequest = new UPSRequest(trackingNumber);

            var upsResponse = TrackAPI.GetUPSTrackingData(upsRequest);

            Assert.Equal("Success", upsResponse.TrackResponse.Response.ResponseStatus.Description);
        }


        private ShipmentAddress[] CreateAddresses()
        {
            var originAddress = new ShipmentAddress()
            {
                Type = new ResponseStatus() { Description = "Shipper Address" },
                Address = CreateOriginAddress()
            };

            var destinationAddress = new ShipmentAddress()
            {
                Type = new ResponseStatus() { Description = "ShipTo Address" },
                Address = CreateDestinationAddress()
            };

            return new ShipmentAddress[2] { originAddress, destinationAddress };
        }


        private Address CreateOriginAddress()
        {
            var originAddress = new Address()
            {
                AddressLine = "1076 De Haro St",
                City = "San Francisco",
                StateProvinceCode = "CA",
                PostalCode = "94107"
            };

            return originAddress;
        }


        private Address CreateDestinationAddress()
        {
            var destinationAddress = new Address()
            {
                AddressLine = "1602 Adams St",
                City = "Denver",
                StateProvinceCode = "CO",
                PostalCode = "80206"
            };

            return destinationAddress;
        }


        private void AddShipmentActivity(UPSResponse upsResponse, Status status)
        {
            var package = new Package()
            {
                Activity = new Activity()
                {
                    Status = status,
                    Date = "20200907",
                    Time = "183500",
                    ActivityLocation = new ActivityLocation()
                    {
                        Address = CreateOriginAddress()
                    }
                }
            };

            upsResponse.TrackResponse.Shipment.Package = package;
        }


        private Status CreateInTransitStatus()
        {
            var inTransitStatus = new Status()
            {
                Type = "I",
                Description = "Arrived at Facility",
            };

            return inTransitStatus;
        }


        private Status CreateDeliveredStatus()
        {
            var deliveredStatus = new Status()
            {
                Type = "D",
                Description = "Delivered",
            };

            return deliveredStatus;
        }


        private Status CreateExceptionStatus()
        {
            var exceptionStatus = new Status()
            {
                Type = "X",
                Description = "Were attempting to verify the package location. / Claim issued.",
            };

            return exceptionStatus;
        }


        private UPSResponse CreateUPSResponse()
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


        private UPSResponse CreateUPSResponse(string refNum)
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
