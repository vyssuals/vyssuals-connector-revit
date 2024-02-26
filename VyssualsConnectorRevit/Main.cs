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

namespace Vyssuals.ConnectorRevit
{
    [Transaction(TransactionMode.Manual)]
    public class Programm : IExternalCommand
    {
        public Result Execute(ExternalCommandData commandData, ref string message, ElementSet elements)
        {
            string clientUrl = "ws://localhost:8184";
            string severUrl = "http://localhost:8184/";

            var webSocketManager = new WebSocketManager(clientUrl, severUrl);
            Task.Run(() => webSocketManager.StartAsync()).Wait();

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
            Task.Run(() => webSocketManager.client.SendDataAsync(elementManager.elements));

            TaskDialog.Show("Information", "Click OK to continue.");
            return Result.Succeeded;
        }
    }
}
