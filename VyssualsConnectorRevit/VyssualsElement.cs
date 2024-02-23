using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace VyssualsConnectorRevit
{
    public struct VyssualsElement
    {
        public string Id;
        public decimal Length;
        public decimal Area;
        public decimal Volume;
        public Dictionary<string, object> Attributes;

        public VyssualsElement(string id,
            Dictionary<string, object> attributes,
            decimal length = 0,
            decimal area = 0,
            decimal volume = 0)
        {
            this.Id = id;
            this.Length = length;
            this.Area = area;
            this.Volume = volume;
            this.Attributes = attributes;
        }
    }
}
