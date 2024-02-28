using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace Vyssuals.ConnectorRevit
{
    [Serializable]
    public struct VyssualsElement
    {
        public string id { get; set; }
        public decimal length { get; set; }
        public decimal area { get; set; }
        public decimal volume { get; set; }
        public Dictionary<string, object> attributes { get; set; }

        public VyssualsElement(string id,
            Dictionary<string, object> attributes,
            decimal length = 0,
            decimal area = 0,
            decimal volume = 0)
        {
            this.id = id;
            this.length = length;
            this.area = area;
            this.volume = volume;
            this.attributes = attributes;
        }
    }
}
