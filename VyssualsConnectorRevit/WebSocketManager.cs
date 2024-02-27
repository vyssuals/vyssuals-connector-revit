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
        private string clientUrl;
        private string serverUrl;
        public WebSocketClient client;
        public WebSocketServer server;
        private CancellationTokenSource cts = new CancellationTokenSource();

        public WebSocketManager(string clientUrl, string serverUrl)
        {
            this.clientUrl = clientUrl;
            this.serverUrl = serverUrl;
            this.client = new WebSocketClient();
            this.server = null;

        }

        public async Task StartAsync()
        {
            if (await this.client.TryConnectAsync(this.clientUrl)) return;

            Debug.WriteLine("wsManger: Starting the server...");
            this.server = new WebSocketServer();
            Task.Run(() => server.RunServerAsync(this.serverUrl, cts.Token));

            bool clientConnected = false;
            int maxAttempts = 10;
            int attempts = 1;
            while (clientConnected == false && attempts <= maxAttempts)
            {
                Debug.WriteLine($"wsManger: Trying to connect to the server... attempt {attempts} / {maxAttempts}");
                clientConnected = await this.client.TryConnectAsync(this.clientUrl);
                attempts++;
            }
        }

        public void StopServer()
        {
            Debug.WriteLine("wsManger: Stopping the server...");
            cts.Cancel(); // this will stop the while loop in the server and the method will continue
        }

        public async Task DisconnectClientAsync()
        {
            Debug.WriteLine("wsManger: Disconnecting client from server...");
            await this.client.DisconnectAsync();
        }

        public async Task Shutdown()
        {
            Debug.WriteLine("wsManger: Shutting down...");
            await this.DisconnectClientAsync();
            this.StopServer();
        }
    }

    //private async Task SendDataAsync(WebSocketClient client, CancellationToken cancellationToken)
    //{
    //    while (!cancellationToken.IsCancellationRequested)
    //    {
    //        //var data = DummyDataGenerator.GenerateDummyData("MyDataSource", 10);

    //        await client.SendDataAsync(data);
    //        await Task.Delay(TimeSpan.FromSeconds(10), cancellationToken);
    //    }

}