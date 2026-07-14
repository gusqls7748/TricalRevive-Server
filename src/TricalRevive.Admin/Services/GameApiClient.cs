using System.Net.Http.Json;

namespace TricalRevive.Admin.Services;

public class PlayerGoldResponse {
    public string PlayerId { get; set; } = string.Empty;
    public int Gold { get; set; }
}

public class PlayerCharactersResponse {
    public string PlayerId { get; set; } = string.Empty;
    public List<string> Characters { get; set; } = new();
}

public class PulledCharacterDto {
    public string Name { get; set; } = string.Empty;
    public int Rarity { get; set; } // 0=R, 1=SR, 2=SSR
    public bool IsPityTriggered { get; set; }
}

public class GachaResultDto {
    public List<PulledCharacterDto> Characters { get; set; } = new();
    public int GoldSpent { get; set; }
    public int RemainingGold { get; set; }
}

/// <summary>
/// Blazor 어드민이 TricalRevive.Api를 호출하기 위한 클라이언트.
/// Admin은 Silo나 Grain을 직접 참조하지 않고, REST API만 바라보도록 설계했습니다.
/// 이렇게 하면 어드민 도구가 서버 내부 구현과 완전히 분리됩니다.
/// </summary>
public class GameApiClient {
    private readonly HttpClient _http;

    public GameApiClient(IHttpClientFactory factory) {
        _http = factory.CreateClient("TricalReviveApi");
    }

    public async Task<PlayerGoldResponse?> GetGoldAsync(string playerId)
        => await _http.GetFromJsonAsync<PlayerGoldResponse>($"/players/{playerId}/gold");

    public async Task<PlayerGoldResponse?> AddGoldAsync(string playerId, int amount) {
        var response = await _http.PostAsync($"/players/{playerId}/gold?amount={amount}", null);
        return await response.Content.ReadFromJsonAsync<PlayerGoldResponse>();
    }

    public async Task<PlayerCharactersResponse?> GetCharactersAsync(string playerId)
        => await _http.GetFromJsonAsync<PlayerCharactersResponse>($"/players/{playerId}/characters");

    public async Task<GachaResultDto?> PullSingleAsync(string playerId) {
        var response = await _http.PostAsync($"/gacha/{playerId}/single", null);
        return await response.Content.ReadFromJsonAsync<GachaResultDto>();
    }

    public async Task<GachaResultDto?> PullTenAsync(string playerId) {
        var response = await _http.PostAsync($"/gacha/{playerId}/ten", null);
        return await response.Content.ReadFromJsonAsync<GachaResultDto>();
    }
}