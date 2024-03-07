using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;


namespace Vyssuals.ConnectorRevit
{
    public class DataUpdate
    {
        public List<VyssualsElement> Elements { get; set; }
        public List<HeaderData> HeaderData { get; set; }
        public List<string> VisibleElements { get; set; }
    }
}
