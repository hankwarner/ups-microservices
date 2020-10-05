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

        public string originPostalCode { get; set; }
        public string destinationPostalCode { get; set; }
        private ILogger _logger;


        public class UPSResponse
        {
            public EMSResponse eMSResponse { get; set; }
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
        }


        private IRestRequest CreateNewTimeInTransitRequest()
        {
            var reqBody = new
            {
                originCountryCode = "US",
                originPostalCode,
                destinationCountryCode = "US",
                destinationPostalCode,
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
                // UPS requires these values, but it does effect days in transit so we don't need them:
                .AddHeader("transactionSrc", "HMW")
                .AddHeader("transId", "1234");

            return request;
        }


        public int? GetGroundBusinessDaysInTransit()
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

            var services = response.eMSResponse.services;

            var groundService = services.FirstOrDefault(svc => svc.ServiceLevel == "GND");

            var groundBusinessDaysInTransit = groundService?.BusinessTransitDays;

            return groundBusinessDaysInTransit;
        }
    }
}
