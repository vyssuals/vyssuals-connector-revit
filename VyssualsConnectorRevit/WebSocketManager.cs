using System;
using System.Collections.Generic;
using System.Diagnostics;
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
        private bool isServerRunning = false;

        public WebSocketManager(string clientUrl, string serverUrl)
        {
            this.clientUrl = clientUrl;
            this.serverUrl = serverUrl;
            this.client = new WebSocketClient();
        }

        public async Task<bool> StartAsync()
        {
            try
            {

            if (await this.client.TryConnectAsync(this.clientUrl))
            {
                Debug.WriteLine("Connected to the server.");
                return true;
            }
            }
            catch (Exception e)
            {
                Debug.WriteLine("ws manager: Failed to connect to the server.");
                Debug.WriteLine(e.Message);
            }

            Debug.WriteLine("Could not connect to the server. Starting the server...");


            // Start the server first if it is not already running
            if (!this.isServerRunning)
            {
                try
                {

                this.server = new WebSocketServer();
                await server.RunServerAsync(this.serverUrl); // Use await to wait for the server to start
                this.isServerRunning = true;
                }
                catch (Exception e)
                {
                    Debug.WriteLine("ws manager: Failed to start the server.");
                    Debug.WriteLine(e.Message);
                }
                
            }
            return this.isServerRunning;
        }

        public void StopServer()
        {
            this.server?.Stop();
            this.isServerRunning = false;
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