namespace Blockchain.Models;

public class WalletState
{
    public string Name { get; set; } = string.Empty;
    public string Address { get; set; } = string.Empty;
    public byte[] PublicKey { get; set; }
}
