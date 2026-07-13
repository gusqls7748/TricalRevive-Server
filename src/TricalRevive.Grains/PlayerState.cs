namespace TricalRevive.Grains;

/// <summary>
/// PlayerGrain이 저장할 데이터 구조.
/// Orleans가 이 클래스를 JSON으로 직렬화해서 OrleansStorage 테이블에 저장합니다.
/// </summary>
[GenerateSerializer]
public class PlayerState {
    [Id(0)]
    public int Gold { get; set; } = 0;

    [Id(1)]
    public List<string> OwnedCharacters { get; set; } = new();
}