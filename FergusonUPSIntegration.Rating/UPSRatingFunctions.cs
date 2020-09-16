using System;
using System.IO;
using System.Threading.Tasks;
using Microsoft.AspNetCore.Mvc;
using Microsoft.Azure.WebJobs;
using Microsoft.Azure.WebJobs.Extensions.Http;
using Microsoft.AspNetCore.Http;
using Microsoft.Extensions.Logging;
using Newtonsoft.Json;
using Microsoft.Extensions.Configuration;
using RateWebReference;
using FergusonUPSIntegration.Rating.Models;
using FergusonUPSIntegration.Rating.Helpers;
using FergusonUPSIntegration.Core.Models;

namespace FergusonUPSIntegration.Rating
{
    public class UPSRatingFunctions
    {
        public UPSRatingFunctions(IConfiguration config)
        {
            _config = config;
        }

        public static IConfiguration _config { get; set; }
        public string ratingErrorLogsUrl = Environment.GetEnvironmentVariable("UPS_RATING_ERROR_LOG");


        /// <summary>
        ///     Uses the UPS Rating API to provide a Ground ship quote for the request origin and destination location.
        ///     Requires the total package weight, ship to address and ship from address.
        /// </summary>
        /// <param name="req">Post request body containing a Ship Quote Request.</param>
        /// <returns>The total shipment cost to two decimal places.</returns>
        [FunctionName("ShipQuoteGround")]
        public async Task<IActionResult> ShipQuoteGround(
            [HttpTrigger(AuthorizationLevel.Function, "post", Route = "rating/ground")] HttpRequest req,
            ILogger log)
        {
            try
            {
                var jsonReq = new StreamReader(req.Body).ReadToEnd();
                var reqBody = JsonConvert.DeserializeObject<ShipQuoteRequest>(jsonReq);
                log.LogInformation(@"Request body: {ReqBody}", reqBody);

                var client = new RatePortTypeClient();
                var security = UPSRequestHelper.CreateUPSSecurity();

                var upsRating = new UPSRating("Ground", reqBody.OriginAddress, reqBody.DestinationAddress, reqBody.Package);

                upsRating.rateResponse = await client.ProcessRateAsync(security, upsRating.rateRequest);

                log.LogInformation(@"UPS Response: {RateResponse}", upsRating.rateResponse);
                log.LogInformation($"Total ship cost: {upsRating.TotalShipCost}");

                return new OkObjectResult(upsRating.TotalShipCost);
            }
            catch(Exception ex)
            {
                log.LogError(ex.Message);
                log.LogError(ex.StackTrace);

                var title = "Exception in ShipQuoteGround";
                var text = $"Error message: {ex.Message}. Stacktrace: {ex.StackTrace}";
                var teamsMessage = new TeamsMessage(title, text, "red", ratingErrorLogsUrl);
                teamsMessage.LogToTeams(teamsMessage);

                return new BadRequestObjectResult(ex.Message);
            }
        }


        /// <summary>
        ///     Uses the UPS Rating API to provide a 2nd Day Air ship quote for the request origin and destination location.
        ///     Requires the total package weight, ship to address and ship from address.
        /// </summary>
        /// <param name="req">Post request body containing a Ship Quote Request.</param>
        /// <returns>The total shipment cost to two decimal places.</returns>
        [FunctionName("ShipQuoteSecondDay")]
        public async Task<IActionResult> ShipQuoteSecondDay(
            [HttpTrigger(AuthorizationLevel.Function, "post", Route = "rating/second-day")] HttpRequest req,
            ILogger log)
        {
            try
            {
                var jsonReq = new StreamReader(req.Body).ReadToEnd();
                var reqBody = JsonConvert.DeserializeObject<ShipQuoteRequest>(jsonReq);
                log.LogInformation(@"Request body: {ReqBody}", reqBody);

                var client = new RatePortTypeClient();
                var security = UPSRequestHelper.CreateUPSSecurity();

                var upsRating = new UPSRating("2nd Day Air", reqBody.OriginAddress, reqBody.DestinationAddress, reqBody.Package);

                upsRating.rateResponse = await client.ProcessRateAsync(security, upsRating.rateRequest);

                log.LogInformation(@"UPS Response: {RateResponse}", upsRating.rateResponse);
                log.LogInformation($"Total ship cost: {upsRating.TotalShipCost}");

                return new OkObjectResult(upsRating.TotalShipCost);
            }
            catch (Exception ex)
            {
                log.LogError(ex.Message);
                log.LogError(ex.StackTrace);

                var title = "Exception in ShipQuoteSecondDay";
                var text = $"Error message: {ex.Message}. Stacktrace: {ex.StackTrace}";
                var teamsMessage = new TeamsMessage(title, text, "red", ratingErrorLogsUrl);
                teamsMessage.LogToTeams(teamsMessage);

                return new BadRequestObjectResult(ex.Message);
            }
        }


        /// <summary>
        ///     Uses the UPS Rating API to provide a Next Day Air ship quote for the request origin and destination location.
        ///     Requires the total package weight, ship to address and ship from address.
        /// </summary>
        /// <param name="req">Post request body containing a Ship Quote Request.</param>
        /// <returns>The total shipment cost to two decimal places.</returns>
        [FunctionName("ShipQuoteNextDay")]
        public async Task<IActionResult> ShipQuoteNextDay(
            [HttpTrigger(AuthorizationLevel.Function, "post", Route = "rating/next-day")] HttpRequest req,
            ILogger log)
        {
            try
            {
                var jsonReq = new StreamReader(req.Body).ReadToEnd();
                var reqBody = JsonConvert.DeserializeObject<ShipQuoteRequest>(jsonReq);
                log.LogInformation(@"Request body: {ReqBody}", reqBody);

                var client = new RatePortTypeClient();
                var security = UPSRequestHelper.CreateUPSSecurity();

                var upsRating = new UPSRating("Next Day Air", reqBody.OriginAddress, reqBody.DestinationAddress, reqBody.Package);

                upsRating.rateResponse = await client.ProcessRateAsync(security, upsRating.rateRequest);

                log.LogInformation(@"UPS Response: {RateResponse}", upsRating.rateResponse);
                log.LogInformation($"Total ship cost: {upsRating.TotalShipCost}");

                return new OkObjectResult(upsRating.TotalShipCost);
            }
            catch (Exception ex)
            {
                log.LogError(ex.Message);
                log.LogError(ex.StackTrace);

                var title = "Exception in ShipQuoteNextDay";
                var text = $"Error message: {ex.Message}. Stacktrace: {ex.StackTrace}";
                var teamsMessage = new TeamsMessage(title, text, "red", ratingErrorLogsUrl);
                teamsMessage.LogToTeams(teamsMessage);

                return new BadRequestObjectResult(ex.Message);
            }
        }
    }
}
