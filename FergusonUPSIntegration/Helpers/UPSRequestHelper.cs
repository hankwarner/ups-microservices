//using System;
//using RateWebService;

//namespace FergusonUPSIntegration.Helpers
//{
//    public class UPSRequestHelper
//    {

//        public static UPSSecurity CreateUPSSecurity()
//        {
            
//            var svcAccessToken = new UPSSecurityServiceAccessToken()
//            {
//                AccessLicenseNumber = Environment.GetEnvironmentVariable("UPS_API_LICENSE")
//            };

//            var userNameToken = new UPSSecurityUsernameToken()
//            {
//                Username = Environment.GetEnvironmentVariable("UPS_API_USER"),
//                Password = Environment.GetEnvironmentVariable("UPS_API_PW")
//            };

//            var security = new UPSSecurity()
//            {
//                ServiceAccessToken = svcAccessToken,
//                UsernameToken = userNameToken
//            };

//            return security;
//        }
//    }
//}
