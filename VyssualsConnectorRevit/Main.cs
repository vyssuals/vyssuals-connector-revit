using Autodesk.Revit.Attributes;
using Autodesk.Revit.DB;
using Autodesk.Revit.UI;
using System;
using System.Diagnostics;
using System.IO;
using System.Reflection;


namespace Vyssuals.ConnectorRevit
{
    [Transaction(TransactionMode.Manual)]
    public class Programm : IExternalCommand
    {
        private VyssualsControl _vyssualsControl;
        private readonly ExternalEventHandler _externalEventHandler = new ExternalEventHandler();

        public Result Execute(ExternalCommandData commandData, ref string message, ElementSet elements)
        {
            try
            {
                Debug.WriteLine("Starting the program...");
                App.CommandData = commandData;

                AppDomain.CurrentDomain.AssemblyResolve += CurrentDomain_AssemblyResolve;
                App.EventHandler = new ExternalEventHandler();

                if (!(App.Doc.ActiveView is View3D))
                {
                    TaskDialog.Show("Error", "Please select a 3D view or click inside it again.");
                    return Result.Failed;
                }

                this._vyssualsControl = new VyssualsControl(new Synchronizer())
                {
                    Topmost = true
                };
                this._vyssualsControl.Closed += VyssualsControl_Closed;
                this._vyssualsControl.Show();

                return Result.Succeeded;
            }
            catch (Exception e)
            {
                Debug.WriteLine("main: Failed to run the program.");
                Debug.WriteLine(e.Message);
                return Result.Failed;
            }
        }

        private void VyssualsControl_Closed(object sender, EventArgs e)
        {
            _externalEventHandler.Raise(() => this._vyssualsControl.Synchronizer.UnsubscribeFromEvents());
            AppDomain.CurrentDomain.AssemblyResolve -= CurrentDomain_AssemblyResolve;
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
