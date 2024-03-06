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
        private ElementProcessor _elementProcessor => ElementSynchronizer.ElementProcessor;
        public ElementSynchronizer ElementSynchronizer;
        public VyssualsControl(ElementSynchronizer synchronizer)
        {
            InitializeComponent();

            string clientUrl = "ws://localhost:8184";
            string severUrl = "http://localhost:8184/";
            this.ElementSynchronizer = synchronizer;
            this._webSocketManager = new WebSocketManager(clientUrl, severUrl);
            this.Closing += VyssualsControl_Closing;
            Task.Run(() => _webSocketManager.StartAsync());

            this.ElementSynchronizer.ElementsChanged += (sender, e) =>
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

        private void HandleSendDataClicked(object sender, RoutedEventArgs e)
        {
            this.ElementSynchronizer.EnableSync();
            this.ElementSynchronizer.DisableSync();
        }

        private void Window_MouseLeftButtonDown(object sender, MouseButtonEventArgs e)
        {
            this.DragMove();
        }

        private void SyncButton_Checked(object sender, RoutedEventArgs e)
        {
            ElementSynchronizer.EnableSync();
        }

        private void SyncButton_Unchecked(object sender, RoutedEventArgs e)
        {
            ElementSynchronizer.DisableSync();
        }

        private void StopPlugin(object sender, RoutedEventArgs e)
        {
            this.Close();
        }

        private void VyssualsControl_Closing(object sender, System.ComponentModel.CancelEventArgs e)
        {
            this.ElementSynchronizer.DisableSync();
            Task.Run(() => this._webSocketManager.DisconnectClientAsync());
        }
    }
}
