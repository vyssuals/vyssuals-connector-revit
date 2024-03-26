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
        public static readonly string Data = "data";
        public static readonly string Disconnect = "disconnect";
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
        public string attributeName { get; set; }
        public string attributeValue { get; set; }
        public string[] ids { get; set; }
    }
}
