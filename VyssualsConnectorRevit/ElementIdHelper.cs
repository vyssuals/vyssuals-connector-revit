using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using Autodesk.Revit.DB;
using System.Reflection;

namespace Vyssuals.ConnectorRevit
{
    public class ElementIdHelper
    {
        public static ElementId CreateElementId(string id)
        {
            int revitVersion;
            if (int.TryParse(App.RevitVersion, out revitVersion) && revitVersion >= 2024)
            {
                // Get the ElementId type
                Type elementIdType = typeof(ElementId);

                // Get the constructor that takes a long
                ConstructorInfo constructor = elementIdType.GetConstructor(new[] { typeof(long) });

                // Call the constructor and return the new ElementId
                return (ElementId)constructor.Invoke(new object[] { long.Parse(id) });
            }
            else
            {
                return new ElementId(int.Parse(id));
            }
        }
    }
}
