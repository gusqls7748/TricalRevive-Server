using Orleans.Runtime;
using StackExchange.Redis;
using TricalRevive.GrainInterfaces;

namespace TricalRevive.Grains;

public class PlayerGrain : Grain, IPlayerGrain {
    private readonly IPersistentState<PlayerState> _state;
    private readonly LeaderboardService _leaderboard;

    public PlayerGrain(
        [PersistentState("player-state", "PlayerStore")] IPersistentState<PlayerState> state,
        IConnectionMultiplexer redis) {
        _state = state;
        _leaderboard = new LeaderboardService(redis);
    }

    public async Task<int> GetGoldAsync() {
        await _leaderboard.TouchSessionAsync(this.GetPrimaryKeyString());
        return _state.State.Gold;
    }

    public async Task<int> AddGoldAsync(int amount) {
        _state.State.Gold += amount;
        await _state.WriteStateAsync();

        var playerId = this.GetPrimaryKeyString();
        await _leaderboard.UpdateGoldAsync(playerId, _state.State.Gold);
        await _leaderboard.TouchSessionAsync(playerId);

        return _state.State.Gold;
    }

    public Task<List<string>> GetOwnedCharactersAsync() {
        return Task.FromResult(_state.State.OwnedCharacters);
    }

    public async Task AddCharacterAsync(string characterName) {
        _state.State.OwnedCharacters.Add(characterName);
        await _state.WriteStateAsync();
    }
}