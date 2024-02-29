using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace Vyssuals.ConnectorRevit
{
    [Serializable]
    public class HeaderData
    {
        public string name { get; set; }
        public string type { get; set; }
        public string unitSymbol { get; set; }

    }
}
