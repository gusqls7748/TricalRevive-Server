using System.Collections.Concurrent;
using TricalRevive.RealtimeProtocol;

namespace TricalRevive.RealtimeServer;

/// <summary>
/// 길드 ID를 기준으로 접속 중인 클라이언트들을 그룹화하여 관리합니다.
/// ConcurrentDictionary를 사용해 여러 클라이언트 핸들러 Task가
/// 동시에 접근해도 안전하게 등록/제거/브로드캐스트가 가능하도록 합니다.
/// </summary>
public class GuildChatHub {
    private readonly ConcurrentDictionary<string, ConcurrentDictionary<Guid, ConnectedClient>> _guilds = new();

    public void Join(string guildId, Guid clientId, ConnectedClient client) {
        var members = _guilds.GetOrAdd(guildId, _ => new ConcurrentDictionary<Guid, ConnectedClient>());
        members[clientId] = client;
    }

    public void Leave(string guildId, Guid clientId) {
        if (_guilds.TryGetValue(guildId, out var members)) {
            members.TryRemove(clientId, out _);
        }
    }

    public async Task BroadcastAsync(string guildId, ChatMessage message, Guid? excludeClientId = null) {
        if (!_guilds.TryGetValue(guildId, out var members))
            return;

        foreach (var (clientId, client) in members) {
            if (clientId == excludeClientId)
                continue;

            try {
                await TcpFraming.WriteMessageAsync(client.Stream, message);
            } catch {
                // 브로드캐스트 도중 특정 클라이언트 전송이 실패해도
                // 다른 클라이언트에게는 계속 전달되도록 무시하고 넘어갑니다.
                // (실제 서비스라면 여기서 로깅 후 해당 연결 정리 로직을 추가합니다.)
            }
        }
    }
}