namespace TricalRevive.GrainInterfaces;

[GenerateSerializer]
public class PulledCharacter {
    [Id(0)] public string Name { get; set; } = string.Empty;
    [Id(1)] public CharacterRarity Rarity { get; set; }
    [Id(2)] public bool IsPityTriggered { get; set; }
}

[GenerateSerializer]
public class GachaResult {
    [Id(0)] public List<PulledCharacter> Characters { get; set; } = new();
    [Id(1)] public int GoldSpent { get; set; }
    [Id(2)] public int RemainingGold { get; set; }
}