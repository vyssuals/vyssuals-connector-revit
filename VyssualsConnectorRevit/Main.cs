using Autodesk.Revit.Attributes;
using Autodesk.Revit.DB;
using Autodesk.Revit.UI;
using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.Linq;
using System.Security.Cryptography;
using System.Text;
using System.Threading.Tasks;

namespace VyssualsConnectorRevit
{
    [Transaction(TransactionMode.Manual)]
    public class Test : IExternalCommand
    {
        public Result Execute(ExternalCommandData commandData, ref string message, ElementSet elements)
        {
            try
            {
                App.RevitVersion = commandData.Application.Application.VersionNumber;
                App.CurrentDoc = commandData.Application.ActiveUIDocument.Document;
                App.EventHandler = new ExternalEventHandler();

                // create an instance of RevitElementFilter and log the elements

                List<VyssualsElement> calcElements = RevitElementFilter.CreateVyssualsElements();

                return Result.Succeeded;
            }
            catch (Exception ex)
            {
                Debug.WriteLine(ex);
                return Result.Failed;
            }
        }


    }
}
