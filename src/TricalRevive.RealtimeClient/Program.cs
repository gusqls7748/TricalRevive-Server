using System.Net.Sockets;
using TricalRevive.RealtimeProtocol;

Console.Write("플레이어 이름 입력: ");
var playerName = Console.ReadLine() ?? "익명";

Console.Write("길드 ID 입력 (예: guild-01): ");
var guildId = Console.ReadLine() ?? "guild-01";

using var tcpClient = new TcpClient();
await tcpClient.ConnectAsync("127.0.0.1", 9000);
var stream = tcpClient.GetStream();

Console.WriteLine($"\n서버에 연결되었습니다. '{guildId}' 길드로 입장합니다.\n");

// 입장 메시지 전송
await TcpFraming.WriteMessageAsync(stream, new ChatMessage {
    Type = ChatMessageType.Join,
    GuildId = guildId,
    PlayerName = playerName
});

// 서버로부터 오는 메시지를 계속 수신하는 백그라운드 태스크
_ = Task.Run(async () => {
    while (true) {
        var message = await TcpFraming.ReadMessageAsync(stream);
        if (message is null) {
            Console.WriteLine("\n서버와의 연결이 끊어졌습니다.");
            break;
        }

        var prefix = message.Type == ChatMessageType.System ? "[시스템]" : $"[{message.PlayerName}]";
        Console.WriteLine($"{prefix} {message.Content}");
    }
});

Console.WriteLine("메시지를 입력하고 Enter를 누르세요. (종료: exit)\n");

while (true) {
    var input = Console.ReadLine();
    if (string.IsNullOrEmpty(input) || input == "exit")
        break;

    await TcpFraming.WriteMessageAsync(stream, new ChatMessage {
        Type = ChatMessageType.Chat,
        GuildId = guildId,
        PlayerName = playerName,
        Content = input
    });
}