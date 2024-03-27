﻿using System;
using System.Collections.Generic;
using System.ComponentModel;
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
    public partial class VyssualsControl : Window, INotifyPropertyChanged
    {
        public ElementSynchronizer ElementSynchronizer;
        private readonly WebSocketManager _webSocketManager;
        private readonly ElementPainter _elementPainter = new ElementPainter();

        private bool _allowManualSync = true;
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

        public event PropertyChangedEventHandler PropertyChanged;
        protected virtual void OnPropertyChanged(string propertyName)
        {
            PropertyChanged?.Invoke(this, new PropertyChangedEventArgs(propertyName));
        }

        public VyssualsControl(ElementSynchronizer synchronizer)
        {
            InitializeComponent();

            string clientUrl = "ws://localhost:8184";
            string severUrl = "http://localhost:8184/";
            this.ElementSynchronizer = synchronizer;
            this._webSocketManager = new WebSocketManager(clientUrl, severUrl);
            this._webSocketManager.client.MessageReceived += WebSocketClient_MessageReceived;
            this.Closing += VyssualsControl_Closing;
            Task.Run(() => _webSocketManager.StartAsync());

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
                        UpdateType.Auto,
                        "Vyssuals Update Test Name",
                        ElementSynchronizer.ElementProcessor.VisibleElementIds
                    )

                };
                var message = new WebSocketMessage(timestamp, MessageType.Data, payload);
                Task.Run(() => _webSocketManager.client.SendMessageAsync(message));
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
            AllowManualSync = false;
        }

        private void SyncButton_Unchecked(object sender, RoutedEventArgs e)
        {
            ElementSynchronizer.DisableSync();
            AllowManualSync = true;
        }

        private void WebSocketClient_MessageReceived(WebSocketMessage message)
        {
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
