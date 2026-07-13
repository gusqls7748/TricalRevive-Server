namespace TricalRevive.GrainInterfaces;

/// <summary>
/// 플레이어 1명을 나타내는 Grain 인터페이스.
/// Orleans에서는 이 인터페이스가 클라이언트와 서버 간의 "계약" 역할을 합니다.
/// </summary>
public interface IPlayerGrain : IGrainWithStringKey {
    /// <summary>현재 보유 골드를 조회합니다.</summary>
    Task<int> GetGoldAsync();

    /// <summary>골드를 지급합니다. (마이너스 값이면 차감)</summary>
    Task<int> AddGoldAsync(int amount);

    /// <summary>보유 캐릭터 목록을 조회합니다.</summary>
    Task<List<string>> GetOwnedCharactersAsync();

    /// <summary>캐릭터를 인벤토리에 추가합니다. (뽑기 결과 등)</summary>
    Task AddCharacterAsync(string characterName);
}