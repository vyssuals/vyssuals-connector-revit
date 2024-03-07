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
        private readonly WebSocketManager _webSocketManager;
        public Synchronizer Synchronizer;

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

        public VyssualsControl(Synchronizer synchronizer)
        {
            InitializeComponent();

            string clientUrl = "ws://localhost:8184";
            string severUrl = "http://localhost:8184/";
            this.Synchronizer = synchronizer;
            this._webSocketManager = new WebSocketManager(clientUrl, severUrl);
            this.Closing += VyssualsControl_Closing;
            Task.Run(() => _webSocketManager.StartAsync());

            this.Synchronizer.ElementsChanged += (sender, e) =>
            {
                Debug.WriteLine("Sending data");
                var payload = new Payload
                {
                    data = this.Synchronizer.DataUpdate.Elements,
                    metadata = this.Synchronizer.DataUpdate.HeaderData,
                    visibleElements = this.Synchronizer.DataUpdate.VisibleElements
                };
                Task.Run(() => _webSocketManager.client.SendMessageAsync(new WebSocketMessage("data", payload)));
            };

        }

        private void HandleSendDataClicked(object sender, RoutedEventArgs e)
        {
            this.Synchronizer.EnableSync();
            this.Synchronizer.DisableSync();
        }

        private void Window_MouseLeftButtonDown(object sender, MouseButtonEventArgs e)
        {
            this.DragMove();
        }

        private void SyncButton_Checked(object sender, RoutedEventArgs e)
        {
            this.Synchronizer.EnableSync();
            AllowManualSync = false;
        }

        private void SyncButton_Unchecked(object sender, RoutedEventArgs e)
        {
            this.Synchronizer.DisableSync();
            AllowManualSync = true;
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
            this.Synchronizer.DisableSync();
            Task.Run(() => this._webSocketManager.DisconnectClientAsync());
        }
    }
}
