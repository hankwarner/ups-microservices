using System;
using System.Collections.Generic;
using System.Data.SqlClient;
using System.Text;
using Dapper;
using FergusonUPSIntregrationCore.Models;
using FergusonUPSIntegrationCore;
using Polly;
using Microsoft.Extensions.Logging;
using TeamsHelper;


namespace TrackingNumbers.Controllers
{
    public class UPSController
    {
        public UPSController(ILogger logger)
        {
            _logger = logger;
        }


        public string connectionString = Environment.GetEnvironmentVariable("SQL_CONN");
        public string devTeamsUrl = Environment.GetEnvironmentVariable("DEV_TEAMS_URL");
        public List<string> invalidTrackingNumbers { get; set; } = new List<string>();
        private ILogger _logger { get; set; }
        

        /// <summary>
        ///     For each tracking number provided, adds a new line to the Tracking Numbers table (with address and ref number) and to the
        ///     Status Updates table (with current status and location of the package).
        /// </summary>
        /// <param name="trackingDataRecords">Tracking data objects that include a tracking number. Used in the UPS Track API request.</param>
        public void AddTrackingNumbersToDB(IEnumerable<UPSTracking> trackingDataRecords)
        {
            foreach (var trackingRecord in trackingDataRecords)
            {
                try
                {
                    var currTrackingNum = trackingRecord.TrackingNumber;
                    _logger.LogInformation($"Tracking Number: {currTrackingNum}");

                    // Call the Track API
                    var upsRequest = new UPSRequest(currTrackingNum);
                    var upsResponse = TrackAPI.GetUPSTrackingData(upsRequest);

                    if (upsResponse.Fault != null)
                    {
                        var errMessage = upsResponse.Fault.detail?.Errors.ErrorDetail.PrimaryErrorCode.Description;
                        _logger.LogError($"{currTrackingNum}: {errMessage}");

                        // Keep track of these for exception report
                        if (errMessage.Contains("Invalid tracking number"))
                        {
                            invalidTrackingNumbers.Add(currTrackingNum);
                        }

                        continue;
                    }

                    // Set origin
                    var originAddress = TrackAPI.GetAddressByType(upsResponse, "Shipper Address");
                    trackingRecord.OriginAddress = originAddress?.Address.AddressLine;
                    trackingRecord.OriginCity = originAddress?.Address.City;
                    trackingRecord.OriginState = originAddress?.Address.StateProvinceCode;
                    var originZip = originAddress?.Address.PostalCode;

                    // Format with hyphen after first 5 digits
                    if (originZip.Length > 5)
                    {
                        originZip = originZip.Insert(5, "-");
                    }

                    trackingRecord.OriginZip = originZip;

                    // Set destination
                    var destinationAddress = TrackAPI.GetAddressByType(upsResponse, "ShipTo Address");
                    trackingRecord.DestinationCity = destinationAddress?.Address.City;
                    trackingRecord.DestinationState = destinationAddress?.Address.StateProvinceCode;
                    trackingRecord.DestinationZip = destinationAddress?.Address.PostalCode;

                    // Set Reference Number
                    trackingRecord.ReferenceNumber = TrackAPI.GetReferenceNum(upsResponse);

                    // Insert line to UPS.tracking.TrackingNumbers
                    InsertLineToUPSTable(trackingRecord, "TrackingNumbers");

                    // Get latest status, location and timestamp
                    var latestActivity = TrackAPI.GetLatestActivity(upsResponse);
                    trackingRecord.Status = latestActivity.Status;
                    trackingRecord.Location = latestActivity.Location;
                    trackingRecord.TimeStamp = latestActivity.TimeStamp;
                    trackingRecord.ExceptionReason = latestActivity.ExceptionReason;

                    // Insert to UPS.tracking.StatusUpdates
                    InsertLineToUPSTable(trackingRecord, "StatusUpdates");

                }
                catch (SqlException sqlEx)
                {
                    _logger.LogWarning(sqlEx, "Duplicate line was not added to TrackingNumbers table.");
                }
                catch (Exception ex)
                {
                    _logger.LogError(ex, "Error in AddAllTrackingNumbers.");
                    var title = "Error in AddAllTrackingNumbers";
                    var text = $"Error message: {ex.Message}";
                    var color = "red";
                    var teamsMessage = new TeamsMessage(title, text, color, devTeamsUrl);
                    teamsMessage.LogToMicrosoftTeams(teamsMessage);
                }
            }
        }


        /// <summary>
        ///     Handles inserts to the TrackingNumber or StatusUpdates by creating a SQL query dynamically based on the table name provided.
        /// </summary>
        /// <param name="trackingData">UPS Tracking data to insert</param>
        /// <param name="tableName">Name of the UPSIntegration table to insert to. Currently handles TrackingNumber and StatusUpdates.</param>
        public void InsertLineToUPSTable(UPSTracking trackingData, string tableName)
        {
            var retryPolicy = Policy.Handle<SqlException>(
                e => !e.Message.Contains("Violation of PRIMARY KEY constraint"))
                .WaitAndRetry(4, _ => TimeSpan.FromMilliseconds(30), (ex, ts, count, context) =>
                {
                    string errorMessage = $"Error in InsertLineToUPSTable. Table name {tableName}";
                    _logger.LogWarning(ex, $"{errorMessage} . Retrying...");
                    if (count == 4) { _logger.LogError(ex, errorMessage); }
                });

            retryPolicy.Execute(() =>
            {
                using (var conn = new SqlConnection(connectionString))
                {
                    conn.Open();

                    var query = $"INSERT INTO UPSIntegration.tracking.{tableName} ";

                    if (tableName == "StatusUpdates")
                    {
                        query += $@"
                            VALUES (
                                @trackingNum, @status, @time, @location, @exception)";

                        conn.Execute(
                            query,
                            new 
                            {
                                trackingNum = trackingData.TrackingNumber,
                                status = trackingData.Status,
                                time = trackingData.TimeStamp,
                                location = trackingData.Location,
                                exception = trackingData.ExceptionReason
                            },
                            commandTimeout: 3);
                    }

                    if (tableName == "TrackingNumbers")
                    {
                        query += $@"
                            VALUES ( 
                                @trackingNum, @refNum, @originAddress, @originCity, @originState, 
                                @originZip, @destinationCity, @destinationState, @destinationZip)";

                        conn.Execute(
                            query,
                            new
                            {
                                trackingNum = trackingData.TrackingNumber,
                                refNum = trackingData.ReferenceNumber,
                                originAddress = trackingData.OriginAddress,
                                originCity = trackingData.OriginCity,
                                originState = trackingData.OriginState,
                                originZip = trackingData.OriginZip,
                                destinationCity = trackingData.DestinationCity,
                                destinationState = trackingData.DestinationState,
                                destinationZip = trackingData.DestinationZip
                            },
                            commandTimeout: 3);
                    }

                    conn.Close();
                }
            });
        }


        /// <summary>
        ///     Queries the StatusUpdates table and selects tracking numbers that are currently in transit.
        /// </summary>
        /// <returns>Tracking numbers currently in transit.</returns>
        public IEnumerable<UPSTracking> GetTrackingNumbersInTransit()
        {
            var retryPolicy = Policy.Handle<SqlException>()
                .WaitAndRetry(4, _ => TimeSpan.FromSeconds(30), (ex, ts, count, context) =>
                {
                    string errorMessage = $"Error in GetTrackingNumbersInTransit.";
                    _logger.LogWarning(ex, $"{errorMessage} . Retrying...");
                    if (count == 4) { _logger.LogError(ex, errorMessage); }
                });

            return retryPolicy.Execute(() =>
            {
                using (var conn = new SqlConnection(connectionString))
                {
                    conn.Open();

                    var query = @"
                        SELECT TrackingNumber
                        FROM UPSIntegration.tracking.StatusUpdates
                        GROUP BY TrackingNumber
                        HAVING SUM(
	                        CASE WHEN 
                                Status IS NOT NULL OR 	                    
                                Status LIKE '%Delivered%' OR 
		                        Status IN ('Exception', 'Returned to Shipper', 'Returned')
		                    THEN 1 ELSE 0 END) = 0";

                    var trackingNumbersInTransit = conn.Query<UPSTracking>(query, commandTimeout: 10);

                    conn.Close();

                    return trackingNumbersInTransit;
                }
            });
        }


        /// <summary>
        ///     Calls the UPS Track API for each tracking number provided to get the updated status and location of the package. 
        ///     Inserts a new line to the StatusUpdates table with the latest tracking information and timestamp.
        /// </summary>
        /// <param name="trackingNumbersInTransit">Tracking numbers which require a status and location update.</param>
        public void UpdateCurrentStatusOfTrackingNumbers(IEnumerable<UPSTracking> trackingNumbersInTransit)
        {
            foreach (var trackingRecord in trackingNumbersInTransit)
            {
                try
                {
                    var currTrackingNumber = trackingRecord.TrackingNumber;
                    _logger.LogInformation($"Tracking Number: {currTrackingNumber}");

                    // Call UPS Track API
                    var upsRequest = new UPSRequest(currTrackingNumber);
                    var upsResponse = TrackAPI.GetUPSTrackingData(upsRequest);

                    if (upsResponse.Fault != null)
                    {
                        var errMessage = upsResponse.Fault.detail?.Errors.ErrorDetail.PrimaryErrorCode.Description;
                        _logger.LogWarning($"{errMessage} {trackingRecord}");
                        
                        if (errMessage.Contains("host you are trying to connect to")) continue;

                        trackingRecord.Status = "Exception";
                        trackingRecord.ExceptionReason = errMessage;
                        trackingRecord.TimeStamp = DateTime.Now.ToString();
                    }
                    else
                    {
                        // Get latest status, location and timestamp
                        var latestActivity = TrackAPI.GetLatestActivity(upsResponse);
                        trackingRecord.Status = latestActivity.Status;
                        trackingRecord.Location = latestActivity.Location;
                        trackingRecord.TimeStamp = latestActivity.TimeStamp;
                        trackingRecord.ExceptionReason = latestActivity.ExceptionReason;

                        _logger.LogInformation("Tracking Record: {@TrackingRecord}", trackingRecord);
                    }

                    // Insert to UPS.tracking.StatusUpdates
                    InsertLineToUPSTable(trackingRecord, "StatusUpdates");
                }
                catch (SqlException sqlEx)
                {
                    _logger.LogInformation(sqlEx, "Duplicate line was not added to StatusUpdates table.");
                }
                catch (Exception ex)
                {
                    _logger.LogError(ex, "Error in UpdateCurrentStatusOfTrackingNumbers. Tracking Record: {@trackingRecord}", trackingRecord);
                    var title = "Error in UpdateCurrentStatusOfTrackingNumbers";
                    var text = $"Error message: {ex.Message}";
                    var color = "yellow";
                    var teamsMessage = new TeamsMessage(title, text, color, devTeamsUrl);
                    teamsMessage.LogToMicrosoftTeams(teamsMessage);
                }
            }
        }
    }
}
