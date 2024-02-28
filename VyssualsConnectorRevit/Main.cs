using Autodesk.Revit.Attributes;
using Autodesk.Revit.DB;
using Autodesk.Revit.UI;
using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.IO;
using System.Linq;
using System.Reflection;
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

                App.RevitVersion = commandData.Application.Application.VersionNumber;
                App.Doc = commandData.Application.ActiveUIDocument.Document;
                AppDomain.CurrentDomain.AssemblyResolve += CurrentDomain_AssemblyResolve;
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
                Task.Run(() => webSocketManager.DisconnectClientAsync());

                return Result.Succeeded;
            }
            catch (Exception e)
            {
                Debug.WriteLine("main: Failed to run the program.");
                Debug.WriteLine(e.Message);
                return Result.Failed;
            }
        }

        private Assembly CurrentDomain_AssemblyResolve(object sender, ResolveEventArgs args)
        {
            // depending on the build mode, use different paths
            string assemblyFolder = @"C:\Users\admin\Documents\GitHub\vyssuals-connector-revit\VyssualsConnectorRevit\bin\Debug";
            //string assemblyFolder = $"C:\\ProgramData\\Autodesk\\Revit\\Addins\\{App.RevitVersion}\\Vyssuals"; // Specify the directory where your DLLs are located
            string assemblyName = new AssemblyName(args.Name).Name;
            string assemblyPath = Path.Combine(assemblyFolder, assemblyName + ".dll");

            if (File.Exists(assemblyPath))
                return Assembly.LoadFrom(assemblyPath);

            return null;
        }
    }
}
