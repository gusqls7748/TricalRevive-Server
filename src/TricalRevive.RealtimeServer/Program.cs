using System.Net;
using System.Net.Sockets;
using TricalRevive.RealtimeProtocol;
using TricalRevive.RealtimeServer;

const int port = 9000;

var hub = new GuildChatHub();
var listener = new TcpListener(IPAddress.Any, port);
listener.Start();

Console.WriteLine($"=== TricalRevive Realtime Server (TCP) 시작됨 - 포트 {port} ===");
Console.WriteLine("클라이언트 연결을 기다립니다...\n");

while (true) {
    var tcpClient = await listener.AcceptTcpClientAsync();
    // 클라이언트 하나당 별도 Task를 할당해서 비동기로 동시에 여러 연결을 처리합니다.
    _ = Task.Run(() => HandleClientAsync(tcpClient, hub));
}

static async Task HandleClientAsync(TcpClient tcpClient, GuildChatHub hub) {
    var clientId = Guid.NewGuid();
    var stream = tcpClient.GetStream();
    var client = new ConnectedClient { TcpClient = tcpClient, Stream = stream };

    try {
        // 첫 메시지는 반드시 Join이어야 합니다 (길드 ID, 플레이어 이름 포함).
        var joinMessage = await TcpFraming.ReadMessageAsync(stream);
        if (joinMessage is null || joinMessage.Type != ChatMessageType.Join) {
            Console.WriteLine($"[{clientId}] 잘못된 첫 메시지 - 연결 종료");
            return;
        }

        client.PlayerName = joinMessage.PlayerName;
        client.GuildId = joinMessage.GuildId;
        hub.Join(client.GuildId, clientId, client);

        Console.WriteLine($"[입장] {client.PlayerName} → 길드 '{client.GuildId}' (연결 {clientId})");

        await hub.BroadcastAsync(client.GuildId, new ChatMessage {
            Type = ChatMessageType.System,
            GuildId = client.GuildId,
            Content = $"{client.PlayerName}님이 입장했습니다."
        });

        // 이후로는 계속 Chat 메시지를 읽어서 같은 길드에 브로드캐스트합니다.
        while (true) {
            var message = await TcpFraming.ReadMessageAsync(stream);
            if (message is null)
                break; // 연결 끊김

            if (message.Type == ChatMessageType.Chat) {
                Console.WriteLine($"[{client.GuildId}] {client.PlayerName}: {message.Content}");
                await hub.BroadcastAsync(client.GuildId, message);
            }
        }
    } catch (Exception ex) {
        Console.WriteLine($"[{clientId}] 오류: {ex.Message}");
    } finally {
        hub.Leave(client.GuildId, clientId);
        Console.WriteLine($"[퇴장] {client.PlayerName} (연결 {clientId})");

        if (!string.IsNullOrEmpty(client.GuildId)) {
            await hub.BroadcastAsync(client.GuildId, new ChatMessage {
                Type = ChatMessageType.System,
                GuildId = client.GuildId,
                Content = $"{client.PlayerName}님이 퇴장했습니다."
            });
        }

        tcpClient.Close();
    }
}