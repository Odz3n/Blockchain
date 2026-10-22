using Blockchain.Models;
using System.Net;
using System.Net.Sockets;
using System.Text.Json;

namespace Blockchain.Services;

public class NetworkService
{
    public int Port { get; private set; }
    private List<Node> _nodes;
    public NetworkService(
        int port,
        List<Node> peers)
    {
        Port = port;
        _nodes = new List<Node>(peers);
    }
    public void Start(Func<string, string, Task> onMessage)
    {
        var listener = new TcpListener(IPAddress.Any, Port);
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
                            await onMessage(parts[0], parts[1]);
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
        foreach (var node in _nodes)
        {
            try
            {
                using var client = new TcpClient();
                await client.ConnectAsync(node.IP, node.Port);
                using var writer = new StreamWriter(client.GetStream())
                {
                    AutoFlush = true
                };
                await writer.WriteLineAsync($"{type}|{json}");
            }
            catch (Exception ex)
            {
                Console.WriteLine($"Failed to send message to peer {node.IP}:{node.Port}: {ex.Message}");
            }
        }
    }
}
