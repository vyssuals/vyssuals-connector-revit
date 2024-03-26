using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace Vyssuals.ConnectorRevit
{
    public class ElementPainter
    {
        public Dictionary<string, List<string>> ColorMessage = new Dictionary<string, List<string>>();

        public void PaintElements()
        { 
            // iterate over the dictionary, use keys and values to paint elements
            foreach (KeyValuePair<string, List<string>> entry in ColorMessage)
            {
                string color = entry.Key;
                List<string> elementIds = entry.Value;

                // paint element with elementId using color
            }
        }

    }
}
