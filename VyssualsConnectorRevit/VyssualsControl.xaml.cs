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
    public partial class VyssualsControl : Window
    {
        private readonly WebSocketManager _webSocketManager;
        private ElementProcessor _elementProcessor => _elementSynchronizer.ElementProcessor;
        private ElementSynchronizer _elementSynchronizer;
        public VyssualsControl(ElementSynchronizer synchronizer)
        {
            InitializeComponent();

            string clientUrl = "ws://localhost:8184";
            string severUrl = "http://localhost:8184/";
            this._elementSynchronizer = synchronizer;
            this._webSocketManager = new WebSocketManager(clientUrl, severUrl);
            Task.Run(() => _webSocketManager.StartAsync());
        }
        private void StopPlugin(object sender, RoutedEventArgs e)
        {
            Task.Run(() => this._webSocketManager.DisconnectClientAsync());
            this._elementSynchronizer.DisableSync();
            this.Close(); // Close the window
        }

        private void SendData(object sender, RoutedEventArgs e)
        {
            this._elementProcessor.CollectElements();
            var payload = new Payload
            {
                data = _elementProcessor.Elements.ToList(),
                metadata = _elementProcessor.headerData
            };
            Task.Run(() => _webSocketManager.client.SendAsync(new WebSocketMessage("data", payload)));
        }

        private void Window_MouseLeftButtonDown(object sender, MouseButtonEventArgs e)
        {
            this.DragMove();
        }

        private void SyncButton_Checked(object sender, RoutedEventArgs e)
        {
            // Code to start the ElementSynchronizer
            _elementSynchronizer.EnableSync();
        }

        private void SyncButton_Unchecked(object sender, RoutedEventArgs e)
        {
            // Code to stop the ElementSynchronizer
            _elementSynchronizer.DisableSync();
        }

    }
}
