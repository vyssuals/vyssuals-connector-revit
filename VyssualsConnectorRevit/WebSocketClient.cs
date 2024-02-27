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
        Debug.WriteLine("Connecting to server...");

        try
        {
            await webSocket.ConnectAsync(new Uri(uri), CancellationToken.None);
            Debug.WriteLine("Connected to server.");
            return true;
        }
        catch (WebSocketException)
        {
            Debug.WriteLine("ws client: Failed to connect to server.");
            return false;
        }
    }

    public async Task SendDataAsync(List<VyssualsElement> data)
    {
        var json = JsonConvert.SerializeObject(data);
        var buffer = Encoding.UTF8.GetBytes(json);
        var segment = new ArraySegment<byte>(buffer);

        await webSocket.SendAsync(segment, WebSocketMessageType.Text, true, CancellationToken.None);
        // Console.WriteLine($"Sent data: {json}");
    }
}
}
