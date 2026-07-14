using System.Buffers.Binary;
using System.Net.Sockets;
using System.Text;
using System.Text.Json;

namespace TricalRevive.RealtimeProtocol;

/// <summary>
/// TCP는 스트림 기반 프로토콜이라 "메시지 하나의 경계"가 없습니다.
/// 예를 들어 클라이언트가 100바이트짜리 메시지를 보내도,
/// 수신 측에서는 그게 30바이트+70바이트로 쪼개져서 도착할 수도 있고
/// 반대로 여러 메시지가 한 번에 뭉쳐서 도착할 수도 있습니다.
///
/// 이를 해결하기 위해 "길이 프리픽스(length-prefix)" 프레이밍을 사용합니다:
/// [4바이트: 본문 길이] + [본문 바이트열]
/// 수신 측은 먼저 4바이트를 읽어 본문 길이를 알아낸 뒤,
/// 그 길이만큼 정확히 채워질 때까지 읽어서 메시지 하나를 완성합니다.
/// </summary>
public static class TcpFraming {
    public static async Task WriteMessageAsync(NetworkStream stream, ChatMessage message, CancellationToken ct = default) {
        var json = JsonSerializer.Serialize(message);
        var payload = Encoding.UTF8.GetBytes(json);

        var lengthPrefix = new byte[4];
        BinaryPrimitives.WriteInt32BigEndian(lengthPrefix, payload.Length);

        await stream.WriteAsync(lengthPrefix, ct);
        await stream.WriteAsync(payload, ct);
    }

    public static async Task<ChatMessage?> ReadMessageAsync(NetworkStream stream, CancellationToken ct = default) {
        var lengthPrefix = new byte[4];
        if (!await ReadExactAsync(stream, lengthPrefix, ct))
            return null; // 연결이 끊긴 경우

        var length = BinaryPrimitives.ReadInt32BigEndian(lengthPrefix);
        var payload = new byte[length];
        if (!await ReadExactAsync(stream, payload, ct))
            return null;

        var json = Encoding.UTF8.GetString(payload);
        return JsonSerializer.Deserialize<ChatMessage>(json);
    }

    /// <summary>
    /// stream.ReadAsync는 요청한 만큼을 다 채워준다는 보장이 없으므로,
    /// buffer가 완전히 채워질 때까지 반복해서 읽습니다.
    /// </summary>
    private static async Task<bool> ReadExactAsync(NetworkStream stream, byte[] buffer, CancellationToken ct) {
        var totalRead = 0;
        while (totalRead < buffer.Length) {
            var read = await stream.ReadAsync(buffer.AsMemory(totalRead), ct);
            if (read == 0)
                return false; // 상대방이 연결을 닫음

            totalRead += read;
        }
        return true;
    }
}