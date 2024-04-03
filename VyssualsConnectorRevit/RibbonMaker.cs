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
            // Create a ribbon panel in the Add-Ins tab
            RibbonPanel panel = uiCtrlApp.CreateRibbonPanel("Vyssuals");

            // Create button data
            PushButtonData buttonData = new PushButtonData(
                "VyssualsButton",
                "Start",
                Assembly.GetExecutingAssembly().Location,
                "Vyssuals.ConnectorRevit.StartCommand");

            // Add the button to the panel
            PushButton button = panel.AddItem(buttonData) as PushButton;
            Uri uriImage = new Uri("pack://application:,,,/VyssualsConnectorRevit;component/VyssualsLogo.png", UriKind.Absolute);
            button.LargeImage = new BitmapImage(uriImage);
        }
    }
}

