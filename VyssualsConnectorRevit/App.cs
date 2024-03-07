using Autodesk.Revit.ApplicationServices;
using Autodesk.Revit.DB;
using Autodesk.Revit.UI;
using System.Linq;

namespace Vyssuals.ConnectorRevit
{
    public class App : IExternalApplication
    {
        public static ExternalCommandData CommandData { get; set; }
        public static Document Doc => CommandData.Application.ActiveUIDocument.Document;
        public static View ActiveView => Doc.ActiveView;
        public static UIApplication UiApp => CommandData.Application;
        public static  string RevitVersion => CommandData.Application.Application.VersionNumber;
        public static string DocumentName => GetDocumentName(Doc);

        public static ExternalEventHandler EventHandler;
        public Result OnStartup(UIControlledApplication application)
        {
            RibbonMaker.Create(application);
            return Result.Succeeded;
        }
        public Result OnShutdown(UIControlledApplication application)
        {
            return Result.Succeeded;
        }

        private static string GetDocumentName(Document doc)
        {
            // split the path and get the last part
            string name = doc.PathName.Split('\\').LastOrDefault();
            // sanitize the name
            name = name.Replace(".rvt", "");
            name = name.Replace(".rfa", "");
            // remove illegal characters
            name = name.Replace(" ", "-");
            name = name.Replace(".", "-");
            return name;
        }

    }
}