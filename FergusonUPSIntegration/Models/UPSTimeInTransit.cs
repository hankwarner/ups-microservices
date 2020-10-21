using System;
using System.Collections.Generic;
using RestSharp;
using Polly;
using Newtonsoft.Json;
using System.Linq;
using Microsoft.Extensions.Logging;

namespace FergusonUPSIntegration.Models
{
    public class UPSTimeInTransit
    {
        public UPSTimeInTransit(string originZip, string destinationZip, ILogger logger)
        {
            originPostalCode = originZip;
            destinationPostalCode = destinationZip;
            _logger = logger;
        }

        public string originCityName { get; set; }
        public string originStateProvince { get; set; }
        public string originPostalCode { get; set; }

        public string destinationCityName { get; set; }
        public string destinationStateProvince { get; set; }
        public string destinationPostalCode { get; set; }
        
        private ILogger _logger;


        public class UPSResponse
        {
            public EMSResponse eMSResponse { get; set; }
            public Validation validationList { get; set; }
            public List<AmbiguousLocation> destinationPickList { get; set; }
            public List<AmbiguousLocation> originPickList { get; set; }
        }

        public class EMSResponse
        {
            public List<Service> services { get; set; }
        }

        public class Service
        {
            public string ServiceLevel { get; set; }
            public string ServiceLevelDescription { get; set; }
            public int BusinessTransitDays { get; set; }
            public string SaturdayDeliveryIndicator { get; set; }
        }

        public class Validation
        {
            public List<string> invalidFieldList { get; set; }

            public bool destinationAmbiguous { get; set; }

            public bool originAmbiguous { get; set; }
        }

        public class AmbiguousLocation
        {
            public string postalCode { get; set; }

            public string city { get; set; }

            public string stateProvince { get; set; }
        }


        /// <summary>
        ///     Calls the UPS Time in Transit API using the origin and destination zip codes. If the destination zip code is determined to be 
        ///     ambiguous by UPS, calls the TNT API agani using the suggested similar zip code.
        /// </summary>
        /// <param name="retryCount">Helps prevent infinite loops. Suggested value is 0, max number of retries is 2.</param>
        /// <returns>Business days in transit for UPS Ground service between the origin and destination.</returns>
        public Service GetUPSGroundService(int retryCount)
        {
            var retryPolicy = Policy.Handle<JsonReaderException>().Retry(5, (ex, count) =>
            {
                var errorTitle = "JsonReaderException in GetGroundBusinessDaysInTransit";
                _logger.LogWarning(ex, $"{errorTitle} . Retrying...");

                if (count == 5) _logger.LogError(ex, errorTitle);
            });

            var url = @"https://onlinetools.ups.com/ship/v1/shipments/transittimes";
            var client = new RestClient(url);

            var request = CreateNewTimeInTransitRequest();

            var jsonResponse = client.Execute(request).Content;

            if (string.IsNullOrEmpty(jsonResponse)) throw new JsonReaderException("UPS returned null response.");

            var response = JsonConvert.DeserializeObject<UPSResponse>(jsonResponse);

            if(response.validationList != null)
            {
                if ((!response.validationList.destinationAmbiguous && !response.validationList.originAmbiguous) || retryCount > 1)
                {
                    var invalidField = response.validationList.invalidFieldList[0];
                    throw new ArgumentException($"{invalidField} is invalid.");
                }

                if (response.validationList.destinationAmbiguous)
                {
                    destinationPostalCode = response.destinationPickList[0].postalCode;
                    destinationCityName = response.destinationPickList[0].city;
                    destinationStateProvince = response.destinationPickList[0].stateProvince;
                }

                if (response.validationList.originAmbiguous)
                {
                    originPostalCode = response.originPickList[0].postalCode;
                    originCityName = response.originPickList[0].city;
                    originStateProvince = response.originPickList[0].stateProvince;
                }

                // Re-run using the UPS suggested values
                return GetUPSGroundService(retryCount++);
            }

            var services = response.eMSResponse.services;
            var groundService = services.FirstOrDefault(svc => svc.ServiceLevel == "GND");

            return groundService;
        }


        /// <summary>
        ///     Creates the JSON body, params and headers to call the UPS Time in Transit API. 
        /// </summary>
        /// <returns>Restsharp request to use to call the UPS TNT API.</returns>
        private IRestRequest CreateNewTimeInTransitRequest()
        {
            var reqBody = new
            {
                originCountryCode = "US",
                originPostalCode,
                originCityName,
                originStateProvince,
                destinationCountryCode = "US",
                destinationPostalCode,
                destinationCityName,
                destinationStateProvince,
                residentialIndicator = "01",
                returnUnfilteredServices = false,
                returnHeavyGoodsServices = true
            };

            var jsonRequest = JsonConvert.SerializeObject(reqBody);

            var request = new RestRequest(Method.POST)
                .AddParameter("application/json; charset=utf-8", jsonRequest, ParameterType.RequestBody)
                .AddHeader("Content-Type", "application/json")
                .AddHeader("Username", Environment.GetEnvironmentVariable("UPS_API_USER"))
                .AddHeader("Password", Environment.GetEnvironmentVariable("UPS_API_PW"))
                .AddHeader("AccessLicenseNumber", Environment.GetEnvironmentVariable("UPS_API_LICENSE"))
                // UPS requires these values, but it doesn't effect days in transit so we don't need them:
                .AddHeader("transactionSrc", "HMW")
                .AddHeader("transId", "1234");

            return request;
        }
    }
}
