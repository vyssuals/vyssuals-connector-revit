using System;
using System.Collections.Generic;
using System.Diagnostics;
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

            this._elementSynchronizer.ElementsChanged += (sender, e) =>
            {
                Debug.WriteLine("Sending data");
                var payload = new Payload
                {
                    data = _elementProcessor.Elements,
                    metadata = _elementProcessor.headerData
                };
                Task.Run(() => _webSocketManager.client.SendMessageAsync(new WebSocketMessage("data", payload)));
            };

        }
        private void StopPlugin(object sender, RoutedEventArgs e)
        {
            Task.Run(() => this._webSocketManager.DisconnectClientAsync());
            this._elementSynchronizer.DisableSync();
            this.Close(); // Close the window
        }

        private void HandleSendDataClicked(object sender, RoutedEventArgs e)
        {
            this._elementSynchronizer.EnableSync();
            this._elementSynchronizer.DisableSync();
        }

        private void Window_MouseLeftButtonDown(object sender, MouseButtonEventArgs e)
        {
            this.DragMove();
        }

        private void SyncButton_Checked(object sender, RoutedEventArgs e)
        {
            _elementSynchronizer.EnableSync();
        }

        private void SyncButton_Unchecked(object sender, RoutedEventArgs e)
        {
            _elementSynchronizer.DisableSync();
        }

    }
}
