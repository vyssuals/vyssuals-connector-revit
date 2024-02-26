using System;
using System.Reflection;
using System.Windows.Media.Imaging;
using Autodesk.Revit.UI;


namespace Vyssuals.ConnectorRevit
{
    public class RibbonMaker
    {
        public static void Create(UIControlledApplication uiCtrlApp)
        {
            RibbonPanel panel = uiCtrlApp.CreateRibbonPanel("Vyssuals");
            PushButtonData buttonData = new PushButtonData(
                "VyssualsButton",
                "Start Vyssuals",
                Assembly.GetExecutingAssembly().Location,
                "VyssualsConnectorRevit.StartCommand");
            PushButton button = panel.AddItem(buttonData) as PushButton;
        }
    }
}

