using System;
using System.Collections.Generic;
using System.Net.WebSockets;
using System.Text;
using System.Threading;
using System.Threading.Tasks;
using System.Diagnostics;

namespace Vyssuals.ConnectorRevit
{
    public class WebSocketClient
    {
        private ClientWebSocket webSocket = null;
        private string _clientUrl;
        public bool IsConnected
        {
            get
            {
                return webSocket.State == WebSocketState.Open;
            }
        }

        public WebSocketClient(string clientUrl)
        {
            this._clientUrl = clientUrl;
        }

        public async Task<bool> TryConnectAsync()
        {
            webSocket = new ClientWebSocket();
            Debug.WriteLine("wsClient: Connecting to server...");

            try
            {
                await webSocket.ConnectAsync(new Uri(this._clientUrl), CancellationToken.None);
                Debug.WriteLine("wsClient: Connected to server.");
                return true;
            }
            catch (WebSocketException)
            {
                Debug.WriteLine("wsClient: Failed to connect to server.");
                return false;
            }
        }

        public async Task SendAsync(ArraySegment<byte> segment)
        {
            try
            {
                Debug.WriteLine("wsClient: Sending message to server...");
                await webSocket.SendAsync(segment, WebSocketMessageType.Text, true, CancellationToken.None);
            }
            catch (WebSocketException e)
            {
                Debug.WriteLine("wsClient: Failed to send message to server.");
                Debug.WriteLine(e.Message);
            }
        }

        public async Task SendMessageAsync(WebSocketMessage message)
        {
            var segment = new ArraySegment<byte>(
            Encoding.UTF8.GetBytes(message.SerializeToJson())
            );
            if (this.IsConnected)
            {
                await SendAsync(segment);
            }
            else
            {
                // open a new connection
                Debug.WriteLine("wsClient: Reconnecting to server...");
                await TryConnectAsync();
                await SendAsync(segment);
            }
        }

        public async Task DisconnectAsync()
        {
            if (webSocket.State == WebSocketState.Open)
            {
                WebSocketMessage message = new WebSocketMessage("disconnect", new Payload());

                var buffer = Encoding.UTF8.GetBytes(message.SerializeToJson());
                var segment = new ArraySegment<byte>(buffer);
                await webSocket.SendAsync(segment, WebSocketMessageType.Text, true, CancellationToken.None);

                try
                {
                    await webSocket.CloseAsync(WebSocketCloseStatus.NormalClosure, "Revit_wsClient: Disconnecting from server", CancellationToken.None);
                    Debug.WriteLine("wsClient: Disconnected from server.");
                }
                catch (WebSocketException e)
                {
                    Debug.WriteLine("wsClient: Failed to disconnect from server.");
                    Debug.WriteLine(e.Message);
                }
            }
            Debug.WriteLine("wsClient: WebSocket state: " + webSocket.State);
        }
    }
}
