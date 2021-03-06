using System;
using Microsoft.AspNetCore.Mvc;
using Microsoft.Azure.WebJobs;
using Microsoft.Azure.WebJobs.Extensions.Http;
using Microsoft.AspNetCore.Http;
using Microsoft.Extensions.Logging;
using AzureFunctions.Extensions.Swashbuckle.Attribute;
using UPSMicroservices.Models;
using System.Net;

namespace UPSMicroservices
{
    public static class TimeInTransitFunctions
    {
        [FunctionName("GetBusinessDaysInTransit")]
        [QueryStringParameter("originZip", "The ship from zip code.", Required = true)]
        [QueryStringParameter("destinationZip", "The ship to zip code.", Required = true)]
        [ProducesResponseType((int)HttpStatusCode.OK, Type = typeof(TimeInTransitResponse))]
        [ProducesResponseType((int)HttpStatusCode.BadRequest, Type = typeof(BadRequestObjectResult))]
        [ProducesResponseType((int)HttpStatusCode.InternalServerError, Type = typeof(StatusCodeResult))]
        public static IActionResult GetBusinessDaysInTransit(
            [HttpTrigger(AuthorizationLevel.Function, "get", Route = "tnt")] HttpRequest req,
            ILogger log)
        {
            try
            {
                var originZip = req.Query["originZip"];
                var destinationZip = req.Query["destinationZip"];
                var missingField = "";

                if (string.IsNullOrEmpty(originZip))
                {
                    missingField = "originZip";
                }
                else if (string.IsNullOrEmpty(destinationZip))
                {
                    missingField = "destinationZip";
                }

                if (!string.IsNullOrEmpty(missingField))
                {
                    log.LogWarning($"Required field is missing: {missingField}");
                    return new BadRequestObjectResult($"Invalid {missingField}")
                    {
                        Value = $"{missingField} is a required parameter.",
                        StatusCode = 404
                    };
                }

                var timeInTransitService = new UPSTimeInTransit(originZip[0], destinationZip[0], log);

                var groundService = timeInTransitService.GetUPSGroundService(0);

                var response = new TimeInTransitResponse(groundService);

                return new OkObjectResult(response);
            }
            catch (ArgumentException ex)
            {
                log.LogWarning(ex, "ArgumentException throw for invalid fields.");
                return new BadRequestObjectResult(ex.Message)
                {
                    Value = ex.Message,
                    StatusCode = 404
                };
            }
            catch (Exception ex)
            {
                log.LogError(ex, "Exception in GetBusinessDaysInTransit. Returning 500.");
                return new StatusCodeResult(500);
            }
        }
    }
}
