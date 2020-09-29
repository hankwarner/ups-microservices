//using System;
//using System.IO;
//using System.Linq;
//using Microsoft.Azure.WebJobs;
//using Microsoft.Extensions.Configuration;
//using Microsoft.Extensions.Logging;
//using FergusonUPSIntegration.Controllers;
//using System.Threading.Tasks;
//using Microsoft.AspNetCore.Mvc;
//using Microsoft.Azure.WebJobs.Extensions.Http;
//using AzureFunctions.Extensions.Swashbuckle.Attribute;
//using Newtonsoft.Json;
//using Microsoft.AspNetCore.Http;
//using RateWebService;
//using FergusonUPSIntegration.Helpers;
//using FergusonUPSIntegration.Models;
//using System.Net;

//namespace FergusonUPSIntegration
//{
//    public class RatingFunctions
//    {
//        public RatingFunctions(IConfiguration config)
//        {
//            _config = config;
//        }

//        public static IConfiguration _config { get; set; }
//        public string ratingErrorLogsUrl = Environment.GetEnvironmentVariable("UPS_RATING_ERROR_LOG");


//        /// <summary>
//        ///     Uses the UPS Rating API to provide a Ground, 2nd Day Air or Next Day Air ship quote for the request origin and destination location.
//        ///     Requires the total package weight, ship to address and ship from address.
//        /// </summary>
//        /// <param name="req">Post request body containing a Ship Quote Request.</param>
//        /// <returns>The total shipment cost to two decimal places.</returns>
//        [ProducesResponseType((int)HttpStatusCode.OK, Type = typeof(double))]
//        [ProducesResponseType((int)HttpStatusCode.NotFound, Type = typeof(NotFoundObjectResult))]
//        [ProducesResponseType((int)HttpStatusCode.BadRequest, Type = typeof(BadRequestObjectResult))]
//        [ProducesResponseType((int)HttpStatusCode.InternalServerError, Type = typeof(StatusCodeResult))]
//        [FunctionName("QuoteShipment")]
//        public async Task<IActionResult> QuoteShipment(
//            [HttpTrigger(AuthorizationLevel.Function, "post", Route = "rating"), RequestBodyType(typeof(ShipQuoteRequest), "failed order")] HttpRequest req,
//            ILogger log)
//        {
//            try
//            {
//                var jsonReq = new StreamReader(req.Body).ReadToEnd();
//                var reqBody = JsonConvert.DeserializeObject<ShipQuoteRequest>(jsonReq);
//                var rateType = reqBody.RateType.ToLower();
//                log.LogInformation(@"Request body: {ReqBody}", reqBody);
//                log.LogInformation(@"Rate type: {RateType}", rateType);

//                if (!UPSRating.rateTypes.Contains(rateType))
//                {
//                    log.LogWarning("Invalid rate type.");

//                    return new BadRequestObjectResult("Invalid rateType")
//                    {
//                        Value = "rateType must be one of the following: " + string.Join(", ", UPSRating.rateTypes),
//                        StatusCode = 404
//                    };
//                }

//                var client = new RatePortTypeClient();
//                var security = UPSRequestHelper.CreateUPSSecurity();

//                var upsRating = new UPSRating(rateType, reqBody.OriginAddress, reqBody.DestinationAddress, reqBody.Package);

//                upsRating.rateResponse = await client.ProcessRateAsync(security, upsRating.rateRequest);

//                log.LogInformation(@"UPS Response: {RateResponse}", upsRating.rateResponse);
//                log.LogInformation($"Total ship cost: {upsRating.TotalShipCost}");

//                return new OkObjectResult(upsRating.TotalShipCost);
//            }
//            catch (Exception ex)
//            {
//                log.LogError(ex.Message);
//                log.LogError(ex.StackTrace);

//                var title = "Exception in QuoteShipment";
//                var text = $"Error message: {ex.Message}. Stacktrace: {ex.StackTrace}";
//                var teamsMessage = new TeamsMessage(title, text, "red", ratingErrorLogsUrl);
//                teamsMessage.LogToTeams(teamsMessage);
//                return new StatusCodeResult(500);
//            }
//        }
//    }
//}
