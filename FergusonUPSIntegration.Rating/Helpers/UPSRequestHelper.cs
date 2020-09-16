using System;
using System.Collections.Generic;
using System.Text;
using RateWebReference;

namespace FergusonUPSIntegration.Rating.Helpers
{
    public class UPSRequestHelper
    {

        public static UPSSecurity CreateUPSSecurity()
        {
            
            var svcAccessToken = new UPSSecurityServiceAccessToken()
            {
                AccessLicenseNumber = Environment.GetEnvironmentVariable("UPS_API_LICENSE")
            };

            var userNameToken = new UPSSecurityUsernameToken()
            {
                Username = Environment.GetEnvironmentVariable("UPS_API_USER"),
                Password = Environment.GetEnvironmentVariable("UPS_API_PW")
            };

            var security = new UPSSecurity()
            {
                ServiceAccessToken = svcAccessToken,
                UsernameToken = userNameToken
            };

            return security;
        }
    }
}
