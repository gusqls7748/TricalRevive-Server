using TricalRevive.GrainInterfaces;

namespace TricalRevive.Grains;

/// <summary>
/// 뽑기 대상이 되는 캐릭터 풀.
/// 실제 서비스라면 DB나 외부 설정 파일에서 로드하겠지만,
/// 포트폴리오 단계에서는 정적 데이터로 단순화했습니다.
/// </summary>
public static class CharacterCatalog {
    public static readonly (string Name, CharacterRarity Rarity)[] Pool =
    {
        ("아리아", CharacterRarity.SSR),
        ("루미엘", CharacterRarity.SSR),
        ("셀레스", CharacterRarity.SSR),

        ("비앙카", CharacterRarity.SR),
        ("델피나", CharacterRarity.SR),
        ("이졸데", CharacterRarity.SR),
        ("페넬로프", CharacterRarity.SR),

        ("클로이", CharacterRarity.R),
        ("마리엘", CharacterRarity.R),
        ("소피아", CharacterRarity.R),
        ("에스텔", CharacterRarity.R),
        ("나탈리", CharacterRarity.R),
    };

    public static (string Name, CharacterRarity Rarity)[] GetByRarity(CharacterRarity rarity)
        => Pool.Where(c => c.Rarity == rarity).ToArray();
}