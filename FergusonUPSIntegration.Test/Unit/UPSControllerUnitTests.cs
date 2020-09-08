using System;
using System.Collections.Generic;
using System.Text;
using Xunit;
using TrackingNumbers.Controllers;
using Microsoft.Extensions.Logging;
using FergusonUPSIntegration.Test.Helpers;
using FergusonUPSIntregrationCore.Models;
using System.Data.SqlClient;
using Dapper;
using System.Linq;

namespace FergusonUPSIntegration.Test.Unit
{
    public class UPSControllerUnitTests
    {
        private readonly ILogger logger = TestHelpers.CreateLogger();

        [Fact]
        public void Test_AddTrackingNumbersToDB()
        {
            var trackingNumbers = new List<string>(){ "1ZE7313W0317984577" };
            var trackingData = CreateUPSTrackingData(trackingNumbers);

            var upsController = new UPSController(logger);
            
            upsController.AddTrackingNumbersToDB(trackingData);

            // Get records from Azure DB
            var trackingNumResult = GetLinesByTrackingNumber(trackingNumbers, "TrackingNumbers");
            var statusUpdatesResult = GetLinesByTrackingNumber(trackingNumbers, "StatusUpdates");

            Assert.Single(trackingNumResult);
            Assert.Single(statusUpdatesResult);

            Assert.Equal("7097 HARRIS RD", trackingNumResult[0].OriginAddress);
            Assert.Equal("CELINA", trackingNumResult[0].OriginCity);
            Assert.Equal("OH", trackingNumResult[0].OriginState);
            Assert.Equal("45822-9399", trackingNumResult[0].OriginZip);
            Assert.Equal("MUNISING", trackingNumResult[0].DestinationCity);
            Assert.Equal("MI", trackingNumResult[0].DestinationState);
            Assert.Equal("49862", trackingNumResult[0].DestinationZip);

            DeleteTrackingNumbers(trackingNumbers, "TrackingNumbers");
            DeleteTrackingNumbers(trackingNumbers, "StatusUpdates");
        }


        private List<UPSTracking> CreateUPSTrackingData(List<string> trackingNumbers)
        {
            var trackingData = new List<UPSTracking>();

            trackingNumbers.ForEach(trackingNum =>
            {
                trackingData.Add(new UPSTracking()
                {
                    TrackingNumber = trackingNum
                });
            });

            return trackingData;
        }


        private static List<UPSTracking> GetLinesByTrackingNumber(List<string> trackingNumbers, string tableName)
        {
            using (var conn = new SqlConnection(Environment.GetEnvironmentVariable("UPS_SQL_CONN")))
            {
                conn.Open();

                var query = $@"
                        SELECT *
                        FROM UPSIntegration.tracking.{tableName} 
                        WHERE TrackingNumber in @trackingNumbers";

                var result = conn.Query<UPSTracking>(query, new { trackingNumbers }).ToList();

                conn.Close();

                return result;
            }
        }


        private static void DeleteTrackingNumbers(List<string> trackingNumbers, string tableName)
        {
            using (var conn = new SqlConnection(Environment.GetEnvironmentVariable("UPS_SQL_CONN")))
            {
                conn.Open();

                var query = $@"
                        DELETE FROM UPSIntegration.tracking.{tableName} 
                        WHERE TrackingNumber in @trackingNumbers";

                conn.Execute(query, new { trackingNumbers }, commandTimeout: 3);

                conn.Close();
            }
        }
    }
}
