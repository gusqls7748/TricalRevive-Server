namespace TricalRevive.RealtimeProtocol;

public enum ChatMessageType {
    Join,    // 길드 채팅방 입장
    Chat,    // 일반 채팅 메시지
    Leave,   // 퇴장
    System   // 서버가 보내는 시스템 알림 (입장/퇴장 알림 등)
}

public class ChatMessage {
    public ChatMessageType Type { get; set; }
    public string GuildId { get; set; } = string.Empty;
    public string PlayerName { get; set; } = string.Empty;
    public string Content { get; set; } = string.Empty;
}