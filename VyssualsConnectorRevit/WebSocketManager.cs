using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.Runtime.CompilerServices;
using System.Threading;
using System.Threading.Tasks;
using Vyssuals.ConnectorRevit;
using Vyssuals.WsServer;

namespace Vyssuals.ConnectorRevit
{
    public class WebSocketManager
    {
        private string serverUrl;
        public WebSocketClient client;
        public WebSocketServer server;
        private CancellationTokenSource cts = new CancellationTokenSource();

        public WebSocketManager(string clientUrl, string serverUrl)
        {
            this.serverUrl = serverUrl;
            this.client = new WebSocketClient(clientUrl);
            this.server = null;
        }

        public async Task StartAsync()
        {
            bool success = await this.client.TryConnectAsync();
            if (!success)
            {
                Debug.WriteLine("wsManger: Starting the server...");
                this.server = new WebSocketServer();
                Task.Run(() => this.server.RunServerAsync(this.serverUrl, this.cts.Token));

                // Wait for the server to start
                await Task.Delay(TimeSpan.FromSeconds(3));

                bool clientConnected = false;
                int maxAttempts = 10;
                int attempt = 1;
                while (clientConnected == false && attempt <= maxAttempts)
                {
                    Debug.WriteLine($"wsManger: Trying to connect to the server... attempt {attempt} / {maxAttempts}");
                    clientConnected = await this.client.TryConnectAsync();
                    attempt++;
                }
            }

            if (this.client.IsConnected)
            {
                Debug.WriteLine("wsManger: Client connected to server, awaiting message");
                Task.Run(() => this.client.ReceiveMessagesAsync(CancellationToken.None));
            }
        }

        public void StopServer()
        // this method is not used since we let the server run until the revit is closed
        // but it shows how to stop the server
        {
            Debug.WriteLine("wsManger: Stopping the server...");
            cts.Cancel(); // this will stop the while loop in the server and the method will continue
        }

        public async Task DisconnectClientAsync()
        {
            Debug.WriteLine("wsManger: Disconnecting client from server...");
            await this.client.DisconnectAsync();
            this.client = null;
        }
    }
}