using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace Vyssuals.ConnectorRevit
{
    public struct VyssualsElement
    {
        public string id;
        public decimal length;
        public decimal area;
        public decimal volume;
        public Dictionary<string, object> attributes;

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
