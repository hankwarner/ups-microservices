using System;
using System.IO;
using Microsoft.Azure.WebJobs;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.Logging;
using System.Threading.Tasks;
using Microsoft.AspNetCore.Mvc;
using Microsoft.Azure.WebJobs.Extensions.Http;
using AzureFunctions.Extensions.Swashbuckle.Attribute;
using Newtonsoft.Json;
using Microsoft.AspNetCore.Http;
using RateWebService;
using UPSMicroservices.Helpers;
using UPSMicroservices.Models;

namespace UPSMicroservices
{
    public class RatingFunctions
    {
        public RatingFunctions(IConfiguration config)
        {
            _config = config;
        }

        public static IConfiguration _config { get; set; }
        public string ratingErrorLogsUrl = Environment.GetEnvironmentVariable("UPS_RATING_ERROR_LOG");


        /// <summary>
        ///     Uses the UPS Rating API to provide a Ground, 2nd Day Air or Next Day Air ship quote for the request origin and destination location.
        ///     Requires the total package weight, ship to address and ship from address.
        /// </summary>
        /// <param name="req">Post request body containing a Ship Quote Request.</param>
        /// <returns>The total shipment cost to two decimal places.</returns>
        [ProducesResponseType(typeof(double), 200)]
        [ProducesResponseType(typeof(BadRequestObjectResult), 400)]
        [ProducesResponseType(typeof(NotFoundObjectResult), 404)]
        [ProducesResponseType(typeof(StatusCodeResult), 500)]
        [FunctionName("QuoteShipment")]
        public async Task<IActionResult> QuoteShipment(
            [HttpTrigger(AuthorizationLevel.Function, "post", Route = "rating"), RequestBodyType(typeof(ShipQuoteRequest), "failed order")] HttpRequest req,
            ILogger log)
        {
            try
            {
                var jsonReq = new StreamReader(req.Body).ReadToEnd();
                log.LogInformation(@"Request body: {0}", jsonReq);
                
                var reqBody = ShipQuoteRequest.FromJson(jsonReq);
                var rateType = reqBody.RateType.ToLower();

                if (!UPSRating.rateTypes.Contains(rateType))
                {
                    log.LogWarning("Invalid rate type.");

                    return new BadRequestObjectResult("Invalid rateType")
                    {
                        Value = "rateType must be one of the following: " + string.Join(", ", UPSRating.rateTypes),
                        StatusCode = 404
                    };
                }

                var client = new RatePortTypeClient();
                var security = UPSRequestHelper.CreateUPSSecurity();

                var upsRating = new UPSRating(rateType, reqBody.OriginAddress, reqBody.DestinationAddress, reqBody.Package);

                upsRating.rateResponse = await client.ProcessRateAsync(security, upsRating.rateRequest);

                log.LogInformation(@"UPS Response: {0}", upsRating.rateResponse.ToJson());
                log.LogInformation($"Total ship cost: {upsRating.TotalShipCost}");

                return new OkObjectResult(upsRating.TotalShipCost);
            }
            catch (Exception ex)
            {
                log.LogError(ex.Message);
                log.LogError(ex.StackTrace);

                var title = "Exception in QuoteShipment";
                var text = $"Error message: {ex.Message}. Stacktrace: {ex.StackTrace}";
                var teamsMessage = new TeamsMessage(title, text, "red", ratingErrorLogsUrl);
                teamsMessage.LogToTeams(teamsMessage);
                return new StatusCodeResult(500);
            }
        }
    }
}
