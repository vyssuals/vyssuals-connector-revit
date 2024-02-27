using System;
using System.Collections.Generic;
using System.Net.WebSockets;
using System.Text;
using System.Threading;
using System.Threading.Tasks;
using Speckle.Newtonsoft.Json;
using System.Diagnostics;

namespace Vyssuals.ConnectorRevit
{ 
    public class WebSocketClient
    {
        private ClientWebSocket webSocket = null;

        public async Task<bool> TryConnectAsync(string uri)
        {
            webSocket = new ClientWebSocket();
            Debug.WriteLine("wsClient: Connecting to server...");

            try
            {
                await webSocket.ConnectAsync(new Uri(uri), CancellationToken.None);
                Debug.WriteLine("wsClient: Connected to server.");
                return true;
            }
            catch (WebSocketException)
            {
                Debug.WriteLine("wsClient: Failed to connect to server.");
                return false;
            }
        }

        public async Task SendDataAsync(List<VyssualsElement> data)
        {
            var json = JsonConvert.SerializeObject(data);
            var buffer = Encoding.UTF8.GetBytes(json);
            var segment = new ArraySegment<byte>(buffer);

            try
            {
                await webSocket.SendAsync(segment, WebSocketMessageType.Text, true, CancellationToken.None);
            }
            catch (WebSocketException e)
            {
                Debug.WriteLine("wsClient: Failed to send data to server.");
                Debug.WriteLine(e.Message);
            }
        }
        public async Task DisconnectAsync()
        {
            if (webSocket.State == WebSocketState.Open)
            {
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
