using System;
using System.Collections.Generic;
using System.Net.WebSockets;
using System.Text;
using System.Linq;
using System.Threading;
using System.Threading.Tasks;
using System.Diagnostics;
using System.Text.Json;

namespace Vyssuals.ConnectorRevit
{
    public class WebSocketClient
    {
        public event Action<WebSocketMessage> MessageReceived;
        private ClientWebSocket webSocket = null;
        private string _clientUrl;
        public bool IsConnected
        {
            get
            {
                if (webSocket == null)
                    return false;
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

        public async Task SendMessageAsync(WebSocketMessage message, int retryCount = 3)
        {
            var segment = new ArraySegment<byte>(
                Encoding.UTF8.GetBytes(message.SerializeToJson())
            );

            for (int attempt = 0; attempt < retryCount; attempt++)
            {
                if (this.IsConnected)
                {
                    await SendAsync(segment);
                    return; // If the send is successful, return from the method
                }
                else
                {
                    // Try to open a new connection
                    Debug.WriteLine("wsClient: Reconnecting to server...");
                    bool connected = await TryConnectAsync();
                    if (connected)
                    {
                        await SendAsync(segment);
                        return; // If the send is successful, return from the method
                    }
                }

                // If the send fails, wait a bit before the next attempt
                await Task.Delay(TimeSpan.FromSeconds(1));
            }

            // If all attempts fail, throw an exception or handle the failure in some other way
            throw new Exception("Failed to send message after multiple attempts.");
        }

        public async Task ReceiveMessagesAsync(CancellationToken cancellationToken)
        {
            var buffer = new ArraySegment<byte>(new byte[8192]);
            var messageContents = new List<byte>();

            try
            {
                while (!cancellationToken.IsCancellationRequested)
                {
                    var result = await webSocket.ReceiveAsync(buffer, cancellationToken);

                    messageContents.AddRange(buffer.Array.Take(result.Count));

                    if (result.EndOfMessage)
                    {
                        if (result.MessageType == WebSocketMessageType.Text)
                        {
                            var messageJson = Encoding.UTF8.GetString(messageContents.ToArray());
                            var message = JsonSerializer.Deserialize<WebSocketMessage>(messageJson);

                            // Raise the MessageReceived event
                            MessageReceived?.Invoke(message);
                        }
                        else if (result.MessageType == WebSocketMessageType.Close)
                        {
                            break;
                        }

                        messageContents.Clear();
                    }
                }
            }
            catch (Exception ex)
            {
                // Handle the exception
                Debug.WriteLine($"An error occurred: {ex.Message}");
            }
        }

        public async Task DisconnectAsync()
        {
            if (webSocket.State == WebSocketState.Open)
            {
                string timestamp = TimestampHelper.Now();
                WebSocketMessage message = new WebSocketMessage(timestamp, MessageType.Disconnect, App.DocumentName, new DataPayload());

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
