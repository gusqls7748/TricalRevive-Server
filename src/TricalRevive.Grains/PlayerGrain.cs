using Orleans.Runtime;
using TricalRevive.GrainInterfaces;

namespace TricalRevive.Grains;

public class PlayerGrain : Grain, IPlayerGrain {
    private readonly IPersistentState<PlayerState> _state;

    // "player-state"는 이 그레인 안에서 상태를 구분하는 이름,
    // "PlayerStore"는 Program.cs에서 등록한 스토리지 프로바이더 이름과 일치해야 합니다.
    public PlayerGrain(
        [PersistentState("player-state", "PlayerStore")] IPersistentState<PlayerState> state) {
        _state = state;
    }

    public Task<int> GetGoldAsync() {
        return Task.FromResult(_state.State.Gold);
    }

    public async Task<int> AddGoldAsync(int amount) {
        _state.State.Gold += amount;
        await _state.WriteStateAsync(); // DB에 실제로 저장
        return _state.State.Gold;
    }

    public Task<List<string>> GetOwnedCharactersAsync() {
        return Task.FromResult(_state.State.OwnedCharacters);
    }

    public async Task AddCharacterAsync(string characterName) {
        _state.State.OwnedCharacters.Add(characterName);
        await _state.WriteStateAsync(); // DB에 실제로 저장
    }
}