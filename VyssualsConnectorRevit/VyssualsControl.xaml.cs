using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using System.Windows;
using System.Windows.Controls;
using System.Windows.Data;
using System.Windows.Documents;
using System.Windows.Input;
using System.Windows.Media;
using System.Windows.Media.Imaging;
using System.Windows.Navigation;
using System.Windows.Shapes;

namespace Vyssuals.ConnectorRevit
{
    /// <summary>
    /// Interaction logic for VyssualsControl.xaml
    /// </summary>
    public partial class VyssualsControl : Window
    {
        private WebSocketManager webSocketManager;
        private ElementManager elementManager;
        public VyssualsControl()
        {
            InitializeComponent();

            string clientUrl = "ws://localhost:8184";
            string severUrl = "http://localhost:8184/";
            this.webSocketManager = new WebSocketManager(clientUrl, severUrl);
            Task.Run(() => webSocketManager.StartAsync());
            this.elementManager = new ElementManager();
        }
        private void StopPlugin(object sender, RoutedEventArgs e)
        {
            Task.Run(() => this.webSocketManager.DisconnectClientAsync());
            this.Close(); // Close the window
        }

        private void SendData(object sender, RoutedEventArgs e)
        {
            elementManager.GatherInitialData();
            var payload = new Payload
            {
                data = elementManager.elements,
                metadata = elementManager.headerData
            };
            Task.Run(() => webSocketManager.client.SendAsync(new WebSocketMessage("data", payload)));
        }

        private void Window_MouseLeftButtonDown(object sender, MouseButtonEventArgs e)
        {
            this.DragMove();
        }
    }
}
