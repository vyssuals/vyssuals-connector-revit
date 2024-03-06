using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace Vyssuals.ConnectorRevit
{
    [Serializable]
    public class VyssualsElement
    {
        public string id { get; set; }
        public Dictionary<string, object> attributes { get; set; }

        public VyssualsElement(string id, Dictionary<string, object> attributes)
        {
            this.id = id;
            this.attributes = attributes;
        }
    }
}
