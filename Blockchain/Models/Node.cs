namespace Blockchain.Models;

public class Node
{
    public string IP { get; set; }
    public int Port { get; set; }
    public Node(
        string ip,
        int port)
    {
        this.IP = ip;
        this.Port = port;
    }
}
