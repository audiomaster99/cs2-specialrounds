using CounterStrikeSharp.API;
using CounterStrikeSharp.API.Core;
using CounterStrikeSharp.API.Modules.Utils;
using System.Text.Json.Serialization;

namespace SpecialRounds;

public class ConfigSpecials : BasePluginConfig
{
    [JsonPropertyName("Prefix")] public string Prefix { get; set; } = $" {ChatColors.Red}[BR]{ChatColors.Default}";
    [JsonPropertyName("mp_buytime")] public int mp_buytime { get; set; } = 15;
    [JsonPropertyName("AllowNoScope")] public bool AllowNoScope { get; set; } = true;
    [JsonPropertyName("AllowScout")] public bool AllowScout { get; set; } = true;
    [JsonPropertyName("AllowBhop")] public bool AllowBhop { get; set; } = true;
    [JsonPropertyName("NoScopeMsg")] public bool NoScopeMsg { get; set; } = true;
    // [JsonPropertyName("NSrounds")] public int NSrounds { get; set; } = 1;
    // [JsonPropertyName("CooldownRounds")] public int CooldownRounds { get; set; } = 2;
    // [JsonPropertyName("VotesNeeded")] public int VotesNeeded { get; set; } = 2;
}
