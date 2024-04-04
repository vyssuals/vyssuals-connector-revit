using System;
using System.Collections.Generic;
using System.ComponentModel;
using System.Diagnostics;
using System.Linq;
using System.Runtime.CompilerServices;
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
    public partial class VyssualsControl : Window, INotifyPropertyChanged
    {
        public ElementSynchronizer ElementSynchronizer;
        private readonly WebSocketManager _webSocketManager;
        private readonly ElementPainter _elementPainter = new ElementPainter();
        private bool _allowManualSync = true;
        private string _updateType = UpdateType.Manual; 
        public bool AllowManualSync
        {
            get { return _allowManualSync; }
            set
            {
                if (_allowManualSync != value)
                {
                    _allowManualSync = value;
                    OnPropertyChanged(nameof(AllowManualSync));
                }
            }
        }
        private bool _isLoading = false;    
        public bool IsLoading
        {
            get { return _isLoading; }
            set
            {
                if (_isLoading != value)
                {
                    _isLoading = value;
                    OnPropertyChanged(nameof(IsLoading));
                }
            }
        }


        public event PropertyChangedEventHandler PropertyChanged;
        protected virtual void OnPropertyChanged(string propertyName)
        {
            PropertyChanged?.Invoke(this, new PropertyChangedEventArgs(propertyName));
        }

        public VyssualsControl(ElementSynchronizer synchronizer)
        {
            string clientUrl = "ws://localhost:8184";
            string severUrl = "http://localhost:8184/";
            this.ElementSynchronizer = synchronizer;
            this._webSocketManager = new WebSocketManager(clientUrl, severUrl);
            this._webSocketManager.client.MessageReceived += WebSocketClient_MessageReceived;
            this.Closing += VyssualsControl_Closing;

            this.ElementSynchronizer.ElementsChanged += (sender, e) =>
            {
                Debug.WriteLine("Sending data");
                var timestamp = TimestampHelper.Now();
                var payload = new DataPayload
                {
                    data = ElementSynchronizer.ElementProcessor.Elements,
                    metadata = ElementSynchronizer.ElementProcessor.HeaderData,
                    update = new VyssualsUpdate
                    (
                        timestamp,
                        _updateType,
                        updateTextBox.Text,
                        ElementSynchronizer.ElementProcessor.VisibleElementIds
                    )

                };
                var message = new WebSocketMessage(timestamp, MessageType.Data, payload);
                Task.Run(() => _webSocketManager.client.SendMessageAsync(message));
            };

            InitializeComponent();
            updateTextBox.Text = "<Update Name>";

            IsLoading = true;
            Task.Run(async () =>
            {
                await _webSocketManager.StartAsync();
                IsLoading = false;
            });

            //while (!_webSocketManager.client.IsConnected)
            //{
            //    Debug.WriteLine("Waiting for connection...");
            //}
        }

        private void HandleSendDataClicked(object sender, RoutedEventArgs e)
        {
            _updateType = UpdateType.Manual;
            this.ElementSynchronizer.EnableSync();
            this.ElementSynchronizer.DisableSync();
        }

        private void Window_MouseLeftButtonDown(object sender, MouseButtonEventArgs e)
        {
            this.DragMove();
        }

        private void SyncButton_Checked(object sender, RoutedEventArgs e)
        {
            _updateType = UpdateType.Auto;
            updateTextBox.Text = "Auto Sync";
            ElementSynchronizer.EnableSync();
            AllowManualSync = false;
        }

        private void SyncButton_Unchecked(object sender, RoutedEventArgs e)
        {
            ElementSynchronizer.DisableSync();
            AllowManualSync = true;
        }

        private void WebSocketClient_MessageReceived(WebSocketMessage message)
        {
            if (message.senderName != App.DocumentName) return;
            switch (message.type)
            {
                case MessageType.Color:
                    if (message.payload is ColorPayload)
                    {
                        Debug.WriteLine("Received color payload", message.payload);
                        List<ColorInformation> colors = (message.payload as ColorPayload).colors;
                        App.EventHandler.Raise(() => _elementPainter.PaintElements(colors));
                    }
                    break;
                case MessageType.ColorCleanup:
                    Debug.WriteLine("Received color cleanup payload", message.payload);
                    App.EventHandler.Raise(() => _elementPainter.ClearPaintedElements());
                    break;
                default:
                    break;
            }
        }

        public void OnWebAppClick(object sender, EventArgs e)
        {
            Process.Start("http://localhost:5173/");
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
