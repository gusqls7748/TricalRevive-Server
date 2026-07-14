using Orleans.Runtime;
using TricalRevive.GrainInterfaces;

namespace TricalRevive.Grains;

public class GachaGrain : Grain, IGachaGrain {
    private const int SinglePullCost = 150;
    private const int TenPullCost = 1350; // 150 * 10에서 10% 할인
    private const int PityThreshold = 60; // 60번 안에 SSR 못 뽑으면 다음 뽑기에서 SSR 확정

    private static readonly Random Rng = new();

    private readonly IPersistentState<GachaState> _state;
    private readonly LeaderboardService _leaderboard;

    public GachaGrain(
        [PersistentState("gacha-state", "PlayerStore")] IPersistentState<GachaState> state,
        StackExchange.Redis.IConnectionMultiplexer redis) {
        _state = state;
        _leaderboard = new LeaderboardService(redis);
    }

    public async Task<GachaResult> PullSingleAsync() {
        var playerGrain = GrainFactory.GetGrain<IPlayerGrain>(this.GetPrimaryKeyString());

        var currentGold = await playerGrain.GetGoldAsync();
        if (currentGold < SinglePullCost)
            throw new InvalidOperationException($"골드가 부족합니다. (필요: {SinglePullCost}, 보유: {currentGold})");

        await playerGrain.AddGoldAsync(-SinglePullCost);

        var pulled = await RollOneAsync();
        await playerGrain.AddCharacterAsync(pulled.Name);
        await _state.WriteStateAsync();

        var remainingGold = await playerGrain.GetGoldAsync();

        return new GachaResult {
            Characters = new List<PulledCharacter> { pulled },
            GoldSpent = SinglePullCost,
            RemainingGold = remainingGold
        };
    }

    public async Task<GachaResult> PullTenAsync() {
        var playerGrain = GrainFactory.GetGrain<IPlayerGrain>(this.GetPrimaryKeyString());

        var currentGold = await playerGrain.GetGoldAsync();
        if (currentGold < TenPullCost)
            throw new InvalidOperationException($"골드가 부족합니다. (필요: {TenPullCost}, 보유: {currentGold})");

        await playerGrain.AddGoldAsync(-TenPullCost);

        var results = new List<PulledCharacter>();
        for (var i = 0; i < 10; i++) {
            var pulled = await RollOneAsync();
            results.Add(pulled);
            await playerGrain.AddCharacterAsync(pulled.Name);
        }

        // 10연차 동안은 매번 DB에 쓰지 않고, 다 뽑은 뒤 한 번만 저장합니다.
        // Orleans Grain은 메모리 상태를 우선 갱신하고, WriteStateAsync 호출 시점에만
        // 실제 DB I/O가 발생하므로 이렇게 배치 처리하면 왕복 비용을 줄일 수 있습니다.
        await _state.WriteStateAsync();

        var remainingGold = await playerGrain.GetGoldAsync();

        return new GachaResult {
            Characters = results,
            GoldSpent = TenPullCost,
            RemainingGold = remainingGold
        };
    }

    public Task<int> GetPityCountAsync() {
        return Task.FromResult(_state.State.PityCounter);
    }

    private async Task<PulledCharacter> RollOneAsync() {
        _state.State.TotalPulls++;
        _state.State.PityCounter++;

        var isPityTriggered = _state.State.PityCounter >= PityThreshold;
        var rarity = isPityTriggered ? CharacterRarity.SSR : RollRarity();

        if (rarity == CharacterRarity.SSR) {
            _state.State.PityCounter = 0;
            await _leaderboard.IncrementSsrCountAsync(this.GetPrimaryKeyString());
        }

        var candidates = CharacterCatalog.GetByRarity(rarity);
        var chosen = candidates[Rng.Next(candidates.Length)];

        return new PulledCharacter {
            Name = chosen.Name,
            Rarity = rarity,
            IsPityTriggered = isPityTriggered
        };
    }

    private static CharacterRarity RollRarity() {
        // SSR 3%, SR 15%, R 82%
        var roll = Rng.NextDouble() * 100;
        return roll switch {
            < 3.0 => CharacterRarity.SSR,
            < 18.0 => CharacterRarity.SR,
            _ => CharacterRarity.R
        };
    }
}