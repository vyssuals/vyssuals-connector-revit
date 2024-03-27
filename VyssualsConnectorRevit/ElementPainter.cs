using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace Vyssuals.ConnectorRevit
{
    public class ElementPainter
    {

        public static void PaintElements(List<ColorInformation> colors)
        { 
            // iterate over the dictionary, use keys and values to paint elements
            foreach (var color in colors)
            {
                Debug.WriteLine(color.color);
                Debug.WriteLine(color.attributeName);
                Debug.WriteLine(color.attributeValue);
                Debug.WriteLine(color.ids);
                
            }
        }

    }
}
