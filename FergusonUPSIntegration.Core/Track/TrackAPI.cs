using System;
using FergusonUPSIntregrationCore.Models;
using RestSharp;
using Polly;
using Serilog;
using TeamsHelper;
using System.Text.RegularExpressions;
using Newtonsoft.Json;
using FergusonUPSIntegrationCore.Models;
using System.Data.SqlClient;
using System.Linq;


namespace FergusonUPSIntegrationCore
{
    public class TrackAPI
    {
        public const string upsTrackingApiUrl = @"https://onlinetools.ups.com/rest/Track";
        public const string teamsUrl = @"";


        /// <summary>
        ///     Sends a request to the UPS Track API to get detailed tracking information on a specific package.
        /// </summary>
        /// <param name="upsRequest">The required object to receive a response from UPS. Must include tracking number and API keys (username, pw and access number).</param>
        /// <returns>UPS Track API response.</returns>
        public static UPSResponse GetUPSTrackingData(UPSRequest upsRequest)
        {
            var retryPolicy = Policy.Handle<Exception>()
                .WaitAndRetry(4, _ => TimeSpan.FromSeconds(30), (ex, ts, count, context) =>
                {
                    string errorMessage = "Error in GetUPSTrackingData";
                    Log.Warning(ex, $"{errorMessage} . Retrying...");
                    if (count == 4) { Log.Error(ex, errorMessage); }
                });

            return retryPolicy.Execute(() =>
            {
                var client = new RestClient(upsTrackingApiUrl);

                var jsonRequest = JsonConvert.SerializeObject(upsRequest);

                var req = new RestRequest(Method.POST);
                req.AddHeader("Content-Type", "application/json");
                req.AddParameter("application/json; charset=utf-8", jsonRequest, ParameterType.RequestBody);

                var jsonResponse = client.Execute(req).Content;

                var upsResponse = JsonConvert.DeserializeObject<UPSResponse>(jsonResponse);

                return upsResponse;
            });
        }


        /// <summary>
        ///     Parses the origin (Shipper Address) or destination address (ShipTo Address) from the UPS Track API response.
        /// </summary>
        /// <param name="upsResponse">The UPS Track API response</param>
        /// <param name="addressType">Origin or Destination</param>
        /// <returns>Address of provided type (origin or destination)</returns>
        public static ShipmentAddress GetAddressByType(UPSResponse upsResponse, string addressType)
        {
            try
            {
                var address = upsResponse.TrackResponse.Shipment.ShipmentAddress
                    .Where(a => a.Type.Description == addressType)
                    .SingleOrDefault();

                return address;
            }
            catch (Exception ex)
            {
                Log.Error(ex, $"Error in GetAddressByType. Address type {addressType}.");
                var title = "Error in GetAddressByType";
                var text = $"Address type {addressType}. Error message: {ex.Message}";
                var color = "red";
                var teamsMessage = new TeamsMessage(title, text, color, teamsUrl);
                teamsMessage.LogToMicrosoftTeams(teamsMessage);
            }

            Log.Warning($"No address was found for type {addressType}. Tracking Number: {upsResponse.TrackResponse.Shipment.InquiryNumber.Value}");
            return new ShipmentAddress();
        }


        /// <summary>
        ///     Parses the first reference number from the UPS Track API response.
        /// </summary>
        /// <param name="upsResponse"></param>
        /// <returns>First reference number or empty string.</returns>
        public static string GetReferenceNum(UPSResponse upsResponse)
        {
            try
            {
                var refNum = upsResponse.TrackResponse.Shipment.ReferenceNumber?.Value;

                return refNum;
            }
            // Null reference will throw if reference # is not available
            catch (NullReferenceException ex)
            {
                Log.Warning($"No reference number found. Tracking Number: {upsResponse.TrackResponse.Shipment.InquiryNumber.Value}");
            }
            catch (Exception ex)
            {
                Log.Error(ex, "Error in GetReferenceNum.");
                var title = "Error in GetReferenceNum";
                var text = $"Error message: {ex.Message}";
                var color = "yellow";
                var teamsMessage = new TeamsMessage(title, text, color, teamsUrl);
                teamsMessage.LogToMicrosoftTeams(teamsMessage);
            }

            return "";
        }


        /// <summary>
        ///     Parses the package's most recent status, location and timestamp from the UPS Track API response.
        /// </summary>
        /// <param name="upsResponse">The UPS Track API response.</param>
        /// <returns>Most recent status, location and timestamp of package.</returns>
        public static LatestActivity GetLatestActivity(UPSResponse upsResponse)
        {
            var activity = new LatestActivity();

            try
            {
                // The most recent activity will be at the first index
                var latestActivity = upsResponse.TrackResponse.Shipment.Package.Activity;

                var latestActivityCode = latestActivity.Status.Type;

                activity.Status = MapActivityCodeToStatus(latestActivityCode);

                if (activity.Status == "Exception")
                {
                    activity.ExceptionReason = latestActivity.Status?.Description.Replace("'", ""); ;
                }

                // Remove seconds from timestamp
                var time = latestActivity.Time.Substring(0, 4);

                // Format time in hh:mm:ss for sql insert
                activity.TimeStamp = latestActivity.Date + " " + Regex.Replace(time, ".{2}", "$0:") + "00";

                // Get the location
                var city = latestActivity.ActivityLocation?.Address.City;
                var state = latestActivity.ActivityLocation?.Address.StateProvinceCode;

                activity.Location = city != null ? city + ", " + state : "";
            }
            catch (Exception ex)
            {
                Log.Error(ex, "Error in ParseShipmentStatus.");
                var title = $"Error in ParseShipmentStatus. Tracking Number: {upsResponse.TrackResponse.Shipment.InquiryNumber.Value}";
                var text = $"Error message: {ex.Message}";
                var color = "yellow";
                var teamsMessage = new TeamsMessage(title, text, color, teamsUrl);
                teamsMessage.LogToMicrosoftTeams(teamsMessage);
            }

            return activity;
        }


        /// <summary>
        ///     Maps the activity code to the activity description.
        /// </summary>
        /// <param name="activityCode">Activity code provided in the latest package activity from the UPS Track API response.</param>
        /// <returns>Activity description</returns>
        public static string MapActivityCodeToStatus(string activityCode)
        {
            string status;

            switch (activityCode)
            {
                case "D":
                    status = "Delivered";
                    break;
                case "I":
                    status = "In Transit";
                    break;
                case "M":
                    status = "Billing Information Received";
                    break;
                case "MV":
                    status = "Billing Information Voided";
                    break;
                case "P":
                    status = "Pickup";
                    break;
                case "X":
                    status = "Exception";
                    break;
                case "RS":
                    status = "Returned to Shipper";
                    break;
                case "DO":
                    status = "Delivered Origin CF";
                    break;
                case "DD":
                    status = "Delivered Destination CFS";
                    break;
                case "W":
                    status = "Warehousing";
                    break;
                case "NA":
                    status = "Not Available";
                    break;
                case "O":
                    status = "Out for Delivery";
                    break;
                default:
                    status = "Not Available";
                    break;
            }

            return status;
        }
    }
}
