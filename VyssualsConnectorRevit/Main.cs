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
                //App.RevitVersion = commandData.Application.Application.VersionNumber;
                App.Doc = commandData.Application.ActiveUIDocument.Document;
                //App.EventHandler = new ExternalEventHandler();
                if (!(App.Doc.ActiveView is View3D))
                {
                    TaskDialog.Show("Error", "Please select a 3D view or click inside it again.");
                    return Result.Failed;
                }


                // create an instance of RevitElementFilter and log the elements
                ElementManager elementManager = new ElementManager();
                elementManager.GatherInitialData();
                // log elementManager.elements.count
                Debug.WriteLine($"Elements count: {elementManager.elements.Count}");
                // log elementManager.parametersInfo
                //foreach (var item in elementManager.parametersInfo)
                //{
                //    Debug.WriteLine($"{item.Key} - {item.Value}");
                //}
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
