using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace Vyssuals.ConnectorRevit
{
    public class UpdateType
    {
        public static readonly string Auto = "auto";
        public static readonly string Manual = "manual";
    }

    [Serializable]
    public class VyssualsUpdate
    {
        public string timestamp { get; set; }
        public string type { get; set; }
        public string name { get; set; }
        public List<string> visibleItemIds { get; set; }

        public VyssualsUpdate(string timestamp, string type, string name, List<string> visibleItemIds)
        {
            this.timestamp = timestamp;
            this.type = type;
            this.name = name;
            this.visibleItemIds = visibleItemIds;
        }
    }
}