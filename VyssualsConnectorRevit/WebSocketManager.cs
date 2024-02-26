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
        private CancellationTokenSource cts;
        public WebSocketClient client;
        private WebSocketServer server;

        public WebSocketManager(string clientUrl, string serverUrl)
        {
            this.clientUrl = clientUrl;
            this.serverUrl = serverUrl;
            this.cts = new CancellationTokenSource();
        }

        public async Task StartAsync()
        {
            var connectTask = TryConnectAsync(this.cts.Token);

            this.cts.Cancel();

            try
            {
                await connectTask;
            }
            catch (OperationCanceledException)
            {
                Console.WriteLine("Operation was cancelled.");
            }
        }

        private async Task TryConnectAsync(CancellationToken cancellationToken)
        {
            this.client = new WebSocketClient();

            while (!cancellationToken.IsCancellationRequested)
            {
                if (!await this.client.TryConnectAsync(this.clientUrl))
                {
                    Console.WriteLine("Could not connect to the server. Starting the server...");

                    this.server = new WebSocketServer();

                    // Start the server first
                    _ = server.RunServerAsync(this.serverUrl);

                    // Wait for the server to start
                    await Task.Delay(1000); // Wait for 1 second

                    Console.WriteLine("Server started. Trying to connect again after a few seconds...");
                    await Task.Delay(TimeSpan.FromSeconds(5), cancellationToken);
                }
                else
                {
                    Console.WriteLine("Connected to the server.");
                    //await SendDataAsync(client, cancellationToken);
                    break;
                }
            }
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