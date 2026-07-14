using System.Net.Sockets;

namespace TricalRevive.RealtimeServer;

public class ConnectedClient {
    public required TcpClient TcpClient { get; init; }
    public required NetworkStream Stream { get; init; }
    public string PlayerName { get; set; } = string.Empty;
    public string GuildId { get; set; } = string.Empty;
}