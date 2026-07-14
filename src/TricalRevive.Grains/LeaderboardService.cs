using StackExchange.Redis;

namespace TricalRevive.Grains;

/// <summary>
/// Redis Sorted Set을 이용한 리더보드 관리.
/// Sorted Set은 멤버(플레이어 ID)마다 점수(score)를 갖고 항상 정렬된 상태를 유지하므로,
/// "상위 N명 조회" 같은 연산이 O(log N)으로 매우 빠릅니다.
/// PostgreSQL로 매번 ORDER BY + LIMIT 쿼리를 날리는 것보다 훨씬 효율적입니다.
/// </summary>
public class LeaderboardService {
    private const string GoldLeaderboardKey = "leaderboard:gold";
    private const string SsrLeaderboardKey = "leaderboard:ssr";
    private const string SessionKeyPrefix = "session:";

    private readonly IDatabase _redis;

    public LeaderboardService(IConnectionMultiplexer redis) {
        _redis = redis.GetDatabase();
    }

    public Task UpdateGoldAsync(string playerId, int currentGold)
        => _redis.SortedSetAddAsync(GoldLeaderboardKey, playerId, currentGold);

    public Task<double> IncrementSsrCountAsync(string playerId)
        => _redis.SortedSetIncrementAsync(SsrLeaderboardKey, playerId, 1);

    public async Task<List<(string PlayerId, double Score)>> GetTopGoldAsync(int count = 10) {
        var entries = await _redis.SortedSetRangeByScoreWithScoresAsync(
            GoldLeaderboardKey, order: Order.Descending, take: count);
        return entries.Select(e => (e.Element.ToString(), e.Score)).ToList();
    }

    public async Task<List<(string PlayerId, double Score)>> GetTopSsrAsync(int count = 10) {
        var entries = await _redis.SortedSetRangeByScoreWithScoresAsync(
            SsrLeaderboardKey, order: Order.Descending, take: count);
        return entries.Select(e => (e.Element.ToString(), e.Score)).ToList();
    }

    /// <summary>
    /// 플레이어가 방금 활동했음을 기록합니다. TTL(5분)이 지나면 자동으로 키가 사라지므로,
    /// 이 키의 존재 여부만 확인하면 "최근 온라인 상태"를 별도의 로그아웃 처리 없이 판단할 수 있습니다.
    /// </summary>
    public Task TouchSessionAsync(string playerId)
        => _redis.StringSetAsync(SessionKeyPrefix + playerId, "active", TimeSpan.FromMinutes(5));

    public Task<bool> IsRecentlyActiveAsync(string playerId)
        => _redis.KeyExistsAsync(SessionKeyPrefix + playerId);
}