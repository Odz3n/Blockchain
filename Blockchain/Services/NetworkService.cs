using System.Net;
using System.Net.Sockets;
using System.Text.Json;

namespace Blockchain.Services;

public class NetworkService
{
    private List<int> _peers;
    public int Port { get; private set; }
    public NetworkService(
        int port,
        List<int> peers)
    {
        Port = port;
        _peers = new List<int>(peers);
    }
    public void Start(Action<string, string> onMessage)
    {
        var listener = new TcpListener(IPAddress.Loopback, Port);
        listener.Start();

        _ = Task.Run(async () =>
        {
            while (true)
            {
                try
                {
                    using var client = await listener.AcceptTcpClientAsync();
                    using var reader = new StreamReader(client.GetStream());

                    string? message = await reader.ReadLineAsync();
                    if (message != null)
                    {
                        var parts = message.Split('|');
                        if (parts.Length == 2)
                            onMessage(parts[0], parts[1]);
                    }
                }
                catch (Exception ex)
                {
                    System.Diagnostics.Debug.WriteLine(ex.Message);
                }
            }
        });
    }
    public async Task BroadcastAsync<T>(string type, T data)
    {
        var json = JsonSerializer.Serialize(data);
        foreach (var peerPort in _peers)
        {
            try
            {
                using var client = new TcpClient();
                await client.ConnectAsync("127.0.0.1", peerPort);
                using var writer = new StreamWriter(client.GetStream())
                {
                    AutoFlush = true
                };
                await writer.WriteLineAsync($"{type}|{json}");
            }
            catch (Exception ex)
            {
                Console.WriteLine($"Failed to send message to peer {peerPort}: {ex.Message}");
            }
        }
    }
}
