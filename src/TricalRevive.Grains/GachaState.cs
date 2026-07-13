namespace TricalRevive.Grains;

[GenerateSerializer]
public class GachaState {
    [Id(0)] public int PityCounter { get; set; } = 0;
    [Id(1)] public int TotalPulls { get; set; } = 0;
}