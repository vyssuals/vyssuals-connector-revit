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
            try
            {
                Debug.WriteLine("Starting the program...");
                string clientUrl = "ws://localhost:8184";
                string severUrl = "http://localhost:8184/";
                var webSocketManager = new WebSocketManager(clientUrl, severUrl);
                Task.Run(() => webSocketManager.StartAsync());

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
                Debug.WriteLine($"Elements count: {elementManager.elements.Count}");

                //if (serverStarted)
                //{
                //    try
                //    {
                //        Task.Run(() => webSocketManager.client.SendDataAsync(elementManager.elements));
                //    }
                //    catch (Exception e)
                //    {
                //        Debug.WriteLine("main: Failed to send data to the server.");
                //        Debug.WriteLine(e.Message);
                //    }
                //}
                //else
                //{
                //    // wait and try again
                //    Task.Delay(3000);
                //    Task.Run(() => webSocketManager.client.SendDataAsync(elementManager.elements));
                //}

                TaskDialog.Show("Information", "Click OK to continue.");
                Task.Run(() => webSocketManager.Shutdown());

                return Result.Succeeded;
            }
            catch (Exception e)
            {
                Debug.WriteLine("main: Failed to run the program.");
                Debug.WriteLine(e.Message);
                return Result.Failed;
            }
        }
    }
}
