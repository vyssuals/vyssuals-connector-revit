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
        public Versions versions { get; set; }

        public VyssualsElement(string id, string timestamp, Dictionary<string, object> attributes)
        {
            this.id = id;
            this.versions = new Versions
            {
                { timestamp, attributes }
            };
        }
    }

    public class Versions : Dictionary<string, object>
    {
    }

}
