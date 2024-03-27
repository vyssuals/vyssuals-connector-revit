using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using System.Text.Json;
using System.Diagnostics;
using System.Text.Json.Serialization;

namespace Vyssuals.ConnectorRevit
{
    public class MessageType
    {
        public const string Data = "data";
        public const string Disconnect = "disconnect";
        public const string Color = "color";
        public const string ColorCleanup = "colorCleanup";
    }

    [Serializable]
    public class WebSocketMessage
    {
        public string type { get; set; }
        public string timestamp { get; set; }
        public string version { get; set; }
        public string sender { get; set; }
        public string senderVersion { get; set; }
        public string senderName { get; set; }
        [JsonConverter(typeof(PayloadConverter))]
        public Payload payload { get; set; }

        public WebSocketMessage(string timestamp, string type, Payload payload)
        {
            this.type = type;
            this.timestamp = timestamp;
            this.version = "1.0";
            this.sender = "Revit";
            this.senderVersion = App.RevitVersion;
            this.senderName = App.DocumentName;
            this.payload = payload;
        }

        public string SerializeToJson()
        {
            Debug.WriteLine(JsonSerializer.Serialize(this.payload));
            if (this.payload is DataPayload)
                Debug.WriteLine("is DataPayload");
                Debug.WriteLine(JsonSerializer.Serialize((this.payload as DataPayload).data));

            var Json = JsonSerializer.Serialize(this);
            Debug.WriteLine(Json);
            return Json;
        }
    }

    [Serializable]
    public abstract class Payload { }

    [Serializable]
    public class DataPayload : Payload
    {
        public List<VyssualsElement> data { get; set; }
        public List<HeaderData> metadata { get; set; }
        public VyssualsUpdate update { get; set; }  
    }

    [Serializable]
    public class ColorPayload : Payload
    {
        public List<ColorInformation> colors { get; set; }
    }

    [Serializable]
    public class ColorInformation
    {
        public string color { get; set; }
        public List<string> ids { get; set; }
    }

    public class PayloadConverter : JsonConverter<Payload>
    {
        public override Payload Read(ref Utf8JsonReader reader, Type typeToConvert, JsonSerializerOptions options)
        {
            // Create a JsonDocument from the reader
            JsonDocument doc = JsonDocument.ParseValue(ref reader);

            try
            {
                // Determine the type of the payload based on the JSON data
                // This will depend on the structure of your JSON data
                // For example, you might check for the presence of certain properties
                if (doc.RootElement.TryGetProperty("data", out _))
                {
                    // If the JSON data has a "data" property, assume it's a DataPayload
                    return JsonSerializer.Deserialize<DataPayload>(doc.RootElement.GetRawText(), options);
                }
                else if (doc.RootElement.TryGetProperty("colors", out _))
                {
                    // If the JSON data has a "colors" property, assume it's a ColorPayload
                    return JsonSerializer.Deserialize<ColorPayload>(doc.RootElement.GetRawText(), options);
                }

                // Add more checks for other Payload subclasses

                // If none of the checks match, return null or throw an exception
                return null;
            }
            finally
            {
                // Dispose the JsonDocument
                doc.Dispose();
            }
        }

        public override void Write(Utf8JsonWriter writer, Payload value, JsonSerializerOptions options)
        {
            // Implement your serialization logic here
            // You can check the type of the value and call the appropriate serialization method
            if (value is DataPayload)
            {
                JsonSerializer.Serialize(writer, value as DataPayload, options);
            }
            else if (value is ColorPayload)
            {
                JsonSerializer.Serialize(writer, value as ColorPayload, options);
            }
            // Add more else if blocks for other Payload subclasses
        }
    }
}
