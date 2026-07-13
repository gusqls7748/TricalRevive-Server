namespace TricalRevive.GrainInterfaces;

/// <summary>
/// 뽑기(가챠)를 담당하는 Grain 인터페이스.
/// 플레이어 ID를 키로 사용하여, 플레이어별 천장(pity) 카운트를 독립적으로 관리합니다.
/// </summary>
public interface IGachaGrain : IGrainWithStringKey {
    Task<GachaResult> PullSingleAsync();
    Task<GachaResult> PullTenAsync();
    Task<int> GetPityCountAsync();
}