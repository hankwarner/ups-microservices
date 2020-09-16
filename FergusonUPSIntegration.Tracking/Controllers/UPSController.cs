using System;
using System.Collections.Generic;
using System.Data.SqlClient;
using System.Text;
using Dapper;
using FergusonUPSIntegrationCore;
using Polly;
using Microsoft.Extensions.Logging;
using FergusonUPSIntegration.Core.Models;

namespace TrackingNumbers.Controllers
{
    public class UPSController
    {
        public UPSController(ILogger logger)
        {
            _logger = logger;
        }

        public string connectionString = Environment.GetEnvironmentVariable("UPS_SQL_CONN");
        public string trackingErrorLogsUrl = Environment.GetEnvironmentVariable("UPS_TRACKING_ERROR_LOG");
        public List<InvalidTrackingNumber> invalidTrackingNumbers { get; set; } = new List<InvalidTrackingNumber>();
        private ILogger _logger { get; set; }
        

        /// <summary>
        ///     For each tracking number provided, adds a new line to the Tracking Numbers table (with address and ref number) and to the
        ///     Status Updates table (with current status and location of the package).
        /// </summary>
        /// <param name="trackingDataRecords">Tracking data objects that includes a tracking number. Used in the UPS Track API request.</param>
        public void AddTrackingNumbersToDB(IEnumerable<UPSTracking> trackingDataRecords)
        {
            foreach (var trackingRecord in trackingDataRecords)
            {
                try
                {
                    var trackingNumber = trackingRecord.TrackingNumber;
                    _logger.LogInformation($"Tracking Number: {trackingNumber}");

                    // Call the Track API
                    var upsRequest = new UPSRequest(trackingNumber);
                    var upsResponse = TrackAPI.GetUPSTrackingData(upsRequest);

                    if (upsResponse.Fault != null)
                    {
                        HandleTrackAPIFault(upsResponse, trackingNumber);
                        continue;
                    }

                    SetOriginAddress(trackingRecord, upsResponse);

                    SetDestinationAddress(trackingRecord, upsResponse);

                    trackingRecord.ReferenceNumber = TrackAPI.GetReferenceNum(upsResponse);

                    // Insert line to UPSIntegration.tracking.TrackingNumbers
                    InsertLineToUPSTable(trackingRecord, "TrackingNumbers");

                    // Get latest status, location and timestamp
                    SetLatestActivity(trackingRecord, upsResponse);

                    // Insert to UPSIntegration.tracking.StatusUpdates
                    InsertLineToUPSTable(trackingRecord, "StatusUpdates");
                }
                catch (SqlException sqlEx)
                {
                    _logger.LogWarning(sqlEx, "Duplicate line was not added to TrackingNumbers table.");
                }
                catch (Exception ex)
                {
                    var title = "Error in AddTrackingNumbersToDB";
                    var text = $"Error message: {ex.Message}";
                    var color = "red";
                    var teamsMessage = new TeamsMessage(title, text, color, trackingErrorLogsUrl);
                    teamsMessage.LogToTeams(teamsMessage);
                    _logger.LogError(ex, title);
                }
            }
        }


        /// <summary>
        ///     Sets the sender address on the tracking record based on the UPS Track API response.
        /// </summary>
        /// <param name="trackingRecord">Object containing tracking data that will be written to UPS Integration DB.</param>
        /// <param name="upsResponse">Response object from the UPS Track API.</param>
        public void SetOriginAddress(UPSTracking trackingRecord, UPSResponse upsResponse)
        {
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
        }


        /// <summary>
        ///     Sets the receiving address on the tracking record based on the UPS Track API response.
        /// </summary>
        /// <param name="trackingRecord">Object containing tracking data that will be written to UPS Integration DB.</param>
        /// <param name="upsResponse">Response object from the UPS Track API.</param>
        public void SetDestinationAddress(UPSTracking trackingRecord, UPSResponse upsResponse)
        {
            var destinationAddress = TrackAPI.GetAddressByType(upsResponse, "ShipTo Address");
            
            trackingRecord.DestinationCity = destinationAddress?.Address.City;
            trackingRecord.DestinationState = destinationAddress?.Address.StateProvinceCode;
            trackingRecord.DestinationZip = destinationAddress?.Address.PostalCode;
        }


        /// <summary>
        ///     Sets the package's current status, location, timestamp and exception (if applicable) on the tracking record based on the 
        ///     UPS Track API response.
        /// </summary>
        /// <param name="trackingRecord">Object containing tracking data that will be written to UPS Integration DB.</param>
        /// <param name="upsResponse">Response object from the UPS Track API.</param>
        public void SetLatestActivity(UPSTracking trackingRecord, UPSResponse upsResponse)
        {
            var latestActivity = TrackAPI.GetLatestActivity(upsResponse);

            trackingRecord.Status = latestActivity.Status;
            trackingRecord.Location = latestActivity.Location;
            trackingRecord.TimeStamp = latestActivity.TimeStamp;
            trackingRecord.ExceptionReason = latestActivity.ExceptionReason;
        }


        /// <summary>
        ///     Parses the fault message from the UPS Track API response and logs error. If tracking number is invalid, it is added to the 
        ///     invalid tracking numbers list that is used in exception reporting.
        /// </summary>
        /// <param name="upsResponse">Response object from the UPS Track API.</param>
        /// <param name="trackingNumber">Tracking number of the package being tracked.</param>
        public void HandleTrackAPIFault(UPSResponse upsResponse, string trackingNumber)
        {
            var errMessage = upsResponse.Fault.detail?.Errors.ErrorDetail.PrimaryErrorCode.Description;
            _logger.LogError($"{trackingNumber}: {errMessage}");

            // These will be written to an exception report in the invalid-tracking-numbers container
            invalidTrackingNumbers.Add(new InvalidTrackingNumber(trackingNumber, errMessage));
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
                    var teamsMessage = new TeamsMessage(title, text, color, trackingErrorLogsUrl);
                    teamsMessage.LogToTeams(teamsMessage);
                }
            }
        }
    }
}
