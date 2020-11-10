using Newtonsoft.Json;
using Newtonsoft.Json.Linq;
using System;

namespace UPSMicroservices.Models
{
    public class UPSResponse
    {
        public TrackResponse? TrackResponse { get; set; }

        public Fault? Fault { get; set; }
    }

    public class TrackResponse
    {
        public Response Response { get; set; }

        public Shipment Shipment { get; set; }
    }

    public partial class Response
    {
        public ResponseStatus ResponseStatus { get; set; }
    }

    public class ResponseStatus
    {
        public string Code { get; set; }

        public string Description { get; set; }
    }

    public class Shipment
    {
        public InquiryNumber InquiryNumber { get; set; }

        public string ShipperNumber { get; set; }

        [JsonConverter(typeof(ShipmentAddressConverter))]
        public ShipmentAddress[] ShipmentAddress { get; set; }

        public Weight ShipmentWeight { get; set; }

        public ResponseStatus Service { get; set; }

        public string PickupDate { get; set; }

        [JsonConverter(typeof(PackageConverter))]
        public Package Package { get; set; }

        [JsonConverter(typeof(ReferenceNumberConverter))]
        public ReferenceNumber ReferenceNumber { get; set; }

    }

    public partial class InquiryNumber
    {
        public string Code { get; set; }

        public string Description { get; set; }

        public string Value { get; set; }
    }

    public partial class Package
    {
        public string TrackingNumber { get; set; }

        [JsonConverter(typeof(ActivityConverter))]
        public Activity Activity { get; set; }

        public Weight PackageWeight { get; set; }
    }

    public partial class Activity
    {
        public ActivityLocation ActivityLocation { get; set; }

        public Status Status { get; set; }

        public string Date { get; set; }

        public string Time { get; set; }
    }

    public partial class ActivityLocation
    {
        public Address Address { get; set; }

        public string Code { get; set; }

        public string Description { get; set; }

        public string SignedForByName { get; set; }
    }

    public class Address
    {
        public string City { get; set; }

        public string StateProvinceCode { get; set; }

        public string PostalCode { get; set; }

        public string CountryCode { get; set; }

        public string AddressLine { get; set; }
    }

    public class Status
    {
        public string Type { get; set; }

        public string Description { get; set; }

        public string Code { get; set; }
    }

    public class Weight
    {
        public UnitOfMeasurement UnitOfMeasurement { get; set; }

        public string WeightWeight { get; set; }
    }

    public class UnitOfMeasurement
    {
        public string Code { get; set; }
    }

    public class ReferenceNumber
    {
        public string Code { get; set; }

        public string Value { get; set; }
    }

    public class ShipmentAddress
    {
        public ResponseStatus Type { get; set; }

        public Address Address { get; set; }
    }

    public class PrimaryErrorCode
    {
        public string Code { get; set; }
        public string Description { get; set; }
    }

    public class ErrorDetail
    {
        public string Severity { get; set; }
        public PrimaryErrorCode PrimaryErrorCode { get; set; }
    }

    public class Errors
    {
        public ErrorDetail ErrorDetail { get; set; }
    }

    public class Detail
    {
        public Errors Errors { get; set; }
    }

    public class Fault
    {
        public string faultcode { get; set; }

        public string faultstring { get; set; }

        public Detail detail { get; set; }
    }


    public class ActivityConverter : JsonConverter
    {
        public override bool CanConvert(Type objectType)
        {
            return (objectType == typeof(Activity) || objectType == typeof(Activity[]));
        }

        public override object ReadJson(JsonReader reader, Type objectType, object existingValue, JsonSerializer serializer)
        {
            var token = JToken.Load(reader);

            if (token.Type == JTokenType.Object)
            {
                return token.ToObject<Activity>();
            }

            // If activity is an array, return the most recent activity object.
            if (token.Type == JTokenType.Array)
            {
                var activities = JArray.Parse(token.ToString());

                return activities[0].ToObject<Activity>();
            }

            return null;
        }

        public override void WriteJson(JsonWriter writer, object value, JsonSerializer serializer)
        {
            serializer.Serialize(writer, value);
        }
    }


    // When there are multiple reference numbers, the API returns an array instead of an object
    public class ReferenceNumberConverter : JsonConverter
    {
        public override bool CanConvert(Type objectType)
        {
            return (objectType == typeof(ReferenceNumber) || objectType == typeof(ReferenceNumber[]));
        }

        public override object ReadJson(JsonReader reader, Type objectType, object existingValue, JsonSerializer serializer)
        {
            var token = JToken.Load(reader);

            if (token.Type == JTokenType.Object)
            {
                return token.ToObject<ReferenceNumber>();
            }

            // If multiple reference numbers, get the first one
            if (token.Type == JTokenType.Array)
            {
                var refNumArray = JArray.Parse(token.ToString());

                return refNumArray[0].ToObject<ReferenceNumber>();
            }

            return null;
        }

        public override void WriteJson(JsonWriter writer, object value, JsonSerializer serializer)
        {
            serializer.Serialize(writer, value);
        }
    }


    /// <summary>
    ///     When there are multiple packages, the API returns an array instead of an object.
    /// </summary>
    public class PackageConverter : JsonConverter
    {
        public override bool CanConvert(Type objectType)
        {
            return (objectType == typeof(Package) || objectType == typeof(Package[]));
        }

        public override object ReadJson(JsonReader reader, Type objectType, object existingValue, JsonSerializer serializer)
        {
            var token = JToken.Load(reader);

            if (token.Type == JTokenType.Object)
            {
                return token.ToObject<Package>();
            }

            // If multiple packages, get the first one
            if (token.Type == JTokenType.Array)
            {
                var packageArray = JArray.Parse(token.ToString());

                return packageArray[0].ToObject<Package>();
            }

            return null;
        }

        public override void WriteJson(JsonWriter writer, object value, JsonSerializer serializer)
        {
            serializer.Serialize(writer, value);
        }
    }


    public class ShipmentAddressConverter : JsonConverter
    {
        public override bool CanConvert(Type objectType)
        {
            return (objectType == typeof(ShipmentAddress) || objectType == typeof(ShipmentAddress[]));
        }

        public override object ReadJson(JsonReader reader, Type objectType, object existingValue, JsonSerializer serializer)
        {
            var token = JToken.Load(reader);

            if (token.Type == JTokenType.Object)
            {
                var address = token.ToObject<ShipmentAddress>();

                return new ShipmentAddress[1] { address };
            }

            // If multiple shipment addresses exists:
            if (token.Type == JTokenType.Array)
            {
                return token.ToObject<ShipmentAddress[]>();
            }

            return null;
        }

        public override void WriteJson(JsonWriter writer, object value, JsonSerializer serializer)
        {
            serializer.Serialize(writer, value);
        }
    }
}