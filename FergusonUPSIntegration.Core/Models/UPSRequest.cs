using System;
using System.Collections.Generic;
using System.Dynamic;
using System.Text;

namespace FergusonUPSIntegration.Core.Models
{
    public class UPSRequest
    {
        public UPSRequest(string trackingNumber)
        {
            TrackRequest = new TrackRequest(trackingNumber);
            TrackRequest.Request = new Request();
        }
        
        public UPSSecurity UPSSecurity { get; set; } = new UPSSecurity();

        public TrackRequest TrackRequest { get; set; }
    }


    public class UPSSecurity
    {
        public UsernameToken UsernameToken { get; set; } = new UsernameToken();

        public ServiceAccessToken ServiceAccessToken { get; set; } = new ServiceAccessToken();
    }


    public class UsernameToken
    {
        public string Username { get; set; } = Environment.GetEnvironmentVariable("UPS_API_USER");

        public string Password { get; set; } = Environment.GetEnvironmentVariable("UPS_API_PW");
    }


    public class ServiceAccessToken
    {
        public string AccessLicenseNumber { get; set; } = Environment.GetEnvironmentVariable("UPS_API_LICENSE");
    }


    public class TrackRequest
    {
        public TrackRequest(string trackingNumber)
        {
            InquiryNumber = trackingNumber;
        }
        
        public Request Request { get; set; }
        
        public string InquiryNumber { get; set; }
    }


    public class Request
    {        
        public string RequestOption { get; set; } = "1"; // all activity

        public TransactionReference TransactionReference { get; set; } = new TransactionReference();
    }


    public class TransactionReference
    {
        public string CustomerContext { get; set; } = "Description";
    }
}
