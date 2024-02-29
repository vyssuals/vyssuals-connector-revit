using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using System.Text.Json;
using System.Diagnostics;

namespace Vyssuals.ConnectorRevit
{
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

        public WebSocketMessage(string type, Payload payload)
        {
            this.type = type;
            this.timestamp = DateTime.Now.ToString("yyyy-MM-ddTHH:mm:ss.fffffffZ");
            this.version = "1.0";
            this.sender = "Revit";
            this.senderVersion = App.RevitVersion;
            this.senderName = App.DocumentName;
            this.payload = payload;
        }

        public string SerializeToJson()
        {
            return JsonSerializer.Serialize(this);
        }
    }

    [Serializable]
    public class Payload
    {
        public List<VyssualsElement> data { get; set; }
        public List<HeaderData> metadata { get; set; }
    }
}
