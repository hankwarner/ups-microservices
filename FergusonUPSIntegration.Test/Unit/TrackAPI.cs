using System;
using Xunit;

namespace UPSTracking.Test
{
    public class UPSTrackingFunctionsTests
    {
        private const string realTrackingNumber = "1Z1F378R0394454252";
        private const string fakeTrackingNumber = "1Z1111111111111111";

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
    }
}
