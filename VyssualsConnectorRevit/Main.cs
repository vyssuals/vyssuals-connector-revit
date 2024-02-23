using Autodesk.Revit.Attributes;
using Autodesk.Revit.DB;
using Autodesk.Revit.UI;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace VyssualsConnectorRevit
{
    [Transaction(TransactionMode.Manual)]
    public class Test : IExternalCommand
    { 
        public Result Execute(ExternalCommandData commandData, ref string message, ElementSet elements)
        {
        TaskDialog.Show("Hello", "World");
            return Result.Succeeded;
        }
    }
}
