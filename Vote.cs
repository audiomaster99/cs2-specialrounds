using System.Text.Json;
using CounterStrikeSharp.API;
using CounterStrikeSharp.API.Core;

namespace SpecialRounds;

public partial class SpecialRounds
{
    private UsersSettings?[] _users = new UsersSettings?[65];
    private Config _config;

    private bool _nzRound;
    private bool _isVoteSuccessful;
    
    private int _countRound = 0;
    private int _countVote = 0; 

    private Config LoadConfig()
    {
        var configPath = Path.Combine(ModuleDirectory, "settings.json");

        if (!File.Exists(configPath)) return CreateConfig(configPath);

        var config = JsonSerializer.Deserialize<Config>(File.ReadAllText(configPath))!;

        return config;
    }

    private Config CreateConfig(string configPath)
    {
        var config = new Config
        {
            NzNeed = 0.6f,
            NzRounds = 4,
            NzCooldownRounds = 4,
            DisableDeagle = false
        };

        File.WriteAllText(configPath,
            JsonSerializer.Serialize(config, new JsonSerializerOptions { WriteIndented = true }));

        Console.ForegroundColor = ConsoleColor.DarkGreen;
        Console.WriteLine("[NoZoom] The configuration was successfully saved to a file: " + configPath);
        Console.ResetColor();

        return config;
    }
}

public class Config
{
    public float NzNeed { get; set; }
    public int NzRounds { get; set; }
    public int NzCooldownRounds { get; set; }
    public bool DisableDeagle { get; set; }
}

public class UsersSettings
{
    public bool IsVoted { get; set; }
}