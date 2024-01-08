using CounterStrikeSharp.API;
using CounterStrikeSharp.API.Core;
using CounterStrikeSharp.API.Core.Attributes.Registration;
using CounterStrikeSharp.API.Modules.Commands;
using CounterStrikeSharp.API.Modules.Admin;
using CounterStrikeSharp.API.Modules.Utils;
using CounterStrikeSharp.API.Modules.Menu;
using CounterStrikeSharp.API.Core.Attributes;
using CounterStrikeSharp.API.Modules.Timers;
using System.ComponentModel;
using System.Drawing;

namespace SpecialRounds;
[MinimumApiVersion(120)]

public static class GetUnixTime
{
    public static int GetUnixEpoch(this DateTime dateTime)
    {
        var unixTime = dateTime.ToUniversalTime() -
                       new DateTime(1970, 1, 1, 0, 0, 0, DateTimeKind.Utc);

        return (int)unixTime.TotalSeconds;
    }
}
public partial class SpecialRounds : BasePlugin, IPluginConfig<ConfigSpecials>
{
    public override string ModuleName => "SpecialRounds";
    public override string ModuleAuthor => "DeadSwim, Slayer, Audio";
    public override string ModuleDescription => "Special rounds for AWP Server.";
    public override string ModuleVersion => "V. 1.0.0";
    private static readonly int?[] IsVIP = new int?[65];
    public CounterStrikeSharp.API.Modules.Timers.Timer? timer_up;
    public CounterStrikeSharp.API.Modules.Timers.Timer? timer_decoy;



    public ConfigSpecials Config { get; set; }
    public int Round;
    public bool EndRound;
    public bool IsRound;
    public int IsRoundNumber;
    public string NameOfRound = "";
    public bool isset = false;
    public bool[] g_Zoom = new bool[64];
    public bool adminNoscope = false;
    private bool _nsRound;
    private bool _isVotingSucces;
    
    private UsersSettings?[] _users = new UsersSettings?[65];
    private int _roundCount = 0;
    private int _voteCount = 0;
    CCSGameRules? _gameRules = null;

    public void OnConfigParsed(ConfigSpecials config)
    {
        Config = config;
    }

    public bool WarmupPeriod
        {
            get
            {
                if (_gameRules is null)
                    SetGameRules();

                return _gameRules is not null && _gameRules.WarmupPeriod;
            }
        }
    void SetGameRules() => _gameRules = Utilities.FindAllEntitiesByDesignerName<CCSGameRulesProxy>("cs_gamerules").First().GameRules!;

    public override void Load(bool hotReload)
    {
        RegisterListener<Listeners.OnClientConnected>((slot =>
        {
            _users[slot + 1] = new UsersSettings { IsVoted = false };
        }));
        RegisterListener<Listeners.OnClientDisconnectPost>((slot =>
        {
            if (_users[slot + 1]!.IsVoted) _voteCount--;

            _users[slot + 1] = null;
        }));
        WriteColor("Special round is [*Loaded*]", ConsoleColor.Green);
        RegisterListener<Listeners.OnMapStart>(name =>
        {
            EndRound = false;
            IsRound = false;
            NameOfRound = "";
            IsRoundNumber = 0;
            Round = 0;

        });
        RegisterListener<Listeners.OnTick>(() =>
        {
            for (int i = 1; i < Server.MaxPlayers; i++)
            {
                var ent = NativeAPI.GetEntityFromIndex(i);
                if (ent == 0)
                    continue;

                var client = new CCSPlayerController(ent);
                if (client == null || !client.IsValid)
                    continue;
                if (IsRound)
                {
                    client.PrintToCenterHtml(
                    $"<font color='gray'>***</font> <font class='fontSize-m' color='orange'> SPECIAL ROUND </font><font color='gray'>***</font><br>" +
                    $"<font class='fontSize-l' color='Green'>[{NameOfRound}]</font><br>" +
                    $"<font class='fontSize-s' color='Orange'>www.BRUTALCI.info</font>"
                    );
                }
                OnTick(client);
            }
            foreach (var player in Utilities.GetPlayers().Where(player => player is { IsValid: true, PawnIsAlive: true }))
            {
                if (player.Pawn.Value.LifeState == (byte)LifeState_t.LIFE_ALIVE)OnTick(player);
            }
        });
        RegisterEventHandler<EventRoundEnd>(((@event, info) =>
        {
            if (!_isVotingSucces) 
            {
                return HookResult.Continue;
            }
            
            if (_roundCount >= ($"{Config.NSrounds}") && !_nsRound)
            {
                _roundCount++;
                if (_roundCount == ($"{Config.CooldownRounds}") + ($"{Config.NSrounds}"))
                {
                    _voteCount = 0;
                    _roundCount = 0;
                    _isVotingSucces = false;
                    foreach (var player in Utilities.GetPlayers())
                    {
                        _users[player.EntityIndex!.Value.Value]!.IsVoted = false;
                    }
                    Server.PrintToChatAll("The NoZoom battle is over!");
                }
                return HookResult.Continue;
            }
            if (!_nsRound)
            {
                _nsRound = true;
                _roundCount = 0;
                return HookResult.Continue;
            }
            _roundCount++;
            if (_roundCount == {Config.NSrounds})
            {
                _nsRound = false;
            }

            return HookResult.Continue;
        }));
    }
    public static SpecialRounds It;
    public SpecialRounds()
    {
        It = this;
    }
    [ConsoleCommand("css_ns", "Start No Scope Vote")]
    public void starNSvote(CCSPlayerController? player, CommandInfo info)
    {
        if (player == null) 
        {
            return;
        }
        if (_isVotingSucces)
        {
            if (_nsRound)
            {
                Server.PrintToChatAll($" {Config.Prefix} No Scope round is allready playing!");
                return;
            }

            if (_roundCount >= {Config.NSrounds} && !_nsRound)
            {
                Server.PrintToChatAll($" {Config.Prefix} You will be able to vote fore NS round after {({Config.CooldownRounds} + {Config.NSrounds})-_roundCount} rounds!");
                return;
            }
            Server.PrintToChatAll($" {Config.Prefix} Enough votes collected!");
        }
        if (_users[player.EntityIndex!.Value.Value]!.IsVoted)
        {
            Server.PrintToChatAll($" {Config.Prefix} You have already voted for NoScope Round!");
            return;
        }

        _users[player.EntityIndex!.Value.Value]!.IsVoted = true;
        var successfulVoteCount = Utilities.GetPlayers().Count * {Config.VotesNeeded};
        if ((int)successfulVoteCount == 0) successfulVoteCount = 1.0f;
        _voteCount++;
        Server.PrintToChatAll($" {Config.Prefix} {ChatColor.Red}{player.PlayerName} {ChatColor.Default}voted for No Scope round {_voteCount}/{(int)successfulVoteCount}");
        if (_voteCount == (int)successfulVoteCount)
        {
            _isVotingSucces = true;
            Server.PrintToChatAll($" {Config.Prefix} NoScope starts next round!");
        }
    }
    [ConsoleCommand("css_noscope", "Start No Scope Round")]
    public void starNSround(CCSPlayerController? player, CommandInfo info)
    {
        if(AdminManager.PlayerHasPermissions(player, "@css/chat"))
        {
            if (player == null)
            {
                return;
            }
            if (adminNoscope == false)
            {
                // adminNoscope = true;
                EndRound = false;
                IsRound = true;
                IsRoundNumber = 1;
                NameOfRound = "NO SCOPE";
                Server.PrintToChatAll($" {ChatColors.Blue}[BR] {ChatColors.Default}Admin started {ChatColors.Lime}No Scope {ChatColors.Default}round!");
            }
            else
            {
                // adminNoscope = false;
                EndRound = true;
                IsRound = false;
                IsRoundNumber = 0;
                NameOfRound = "NO SCOPE";
                Server.PrintToChatAll($" {ChatColors.Blue}[BR] {ChatColors.Default}Admin disabled {ChatColors.Lime}No Scope {ChatColors.Default}round!");
            }
        }
    }
    [GameEventHandler]
    public HookResult OnRoundEnd(EventRoundEnd @event, GameEventInfo info)
    {   
        if (EndRound)
        {

            WriteColor($"[SPECIALROUNDS] - *SUCCESS*  Disabling Special Round.", ConsoleColor.Green);
            if(IsRoundNumber == 1)
            {

            }
            if (IsRoundNumber == 2)
            {
                change_cvar("sv_autobunnyhopping", "false");
                change_cvar("sv_enablebunnyhopping", "false");
            }
            if (IsRoundNumber == 3)
            {
                foreach (var player in Utilities.GetPlayers())
                {
                    if (!is_alive(player))
                        return HookResult.Continue;
                    foreach (var weapon in player.PlayerPawn.Value.WeaponServices!.MyWeapons)
                    {
                        if (weapon is { IsValid: true, Value.IsValid: true })
                        {

                            if (weapon.Value.DesignerName.Contains("bayonet") || weapon.Value.DesignerName.Contains("knife") || weapon.Value.DesignerName.Contains("awp"))
                            {
                                continue;
                            }
                            weapon.Value.Remove();                          
                        }
                    }
                    if (CheckIsHaveWeapon("awp", player) == false)
                    {
                        player.GiveNamedItem("weapon_awp");
                    }
                }
            }
            IsRound = false;
            EndRound = false;
            isset = false;
            IsRoundNumber = 0;
            NameOfRound = "";

        }
        if (IsRound)
        {
            WriteColor($"[SPECIALROUNDS] - *WARNING*  Cannot start special round while one is active!", ConsoleColor.Yellow);
            return HookResult.Continue;
        }
        if (Round < 0)
        {
            WriteColor("[SPECIALROUNDS] - *WARNING*  Cannot start special round < 5.", ConsoleColor.Yellow);
            return HookResult.Continue;
        }
        Random rnd = new Random();
        int random = rnd.Next(0, 60);
        if (random == 1 || random == 2 || random == 14 || random == 15)
        {
            if (Config.AllowNoScope)
            {
                IsRound = true;
                EndRound = true;
                IsRoundNumber = 1;
                NameOfRound = "NO SCOPE";
            }
        }
        if (random == 21 || random == 22)
        {
            if (Config.AllowBhop)
            {
                IsRound = true;
                EndRound = true;
                IsRoundNumber = 2;
                NameOfRound = "BHOP ROUND";
            }
        }
        if (random == 42 || random == 43)
        {
            if (Config.AllowScout)
            {
                IsRound = true;
                EndRound = true;
                IsRoundNumber = 3;
                NameOfRound = "SCOUT ROUND";
            }
        }
        if (IsRound == true)
        {
            WriteColor($"[SPECIALROUNDS] - Starting special round: {NameOfRound} Number: {random}.", ConsoleColor.Green);
        }
        //Server.PrintToConsole($" Settings : {NameOfRound} / IsRound {IsRound} / IsRoundNumber {IsRoundNumber} / Random number {random}");

        return HookResult.Continue;
    }
    [GameEventHandler]
    public HookResult OnRoundStart(EventRoundStart @event, GameEventInfo info)
    {
        if (WarmupPeriod)
        {
            IsRound = false;
            EndRound = false;
            isset = false;
            IsRoundNumber = 0;
            NameOfRound = "";
        }
        foreach (var l_player in Utilities.GetPlayers())
        {
            CCSPlayerController player = l_player;
            var client = player.Index;
            
            if (IsRoundNumber == 1)
            {
                WriteColor($"[SPECIALROUNDS] - Starting special round {NameOfRound}.", ConsoleColor.Green);

                if (IsRound || Config.AllowNoScope)
                {
                    if (!is_alive(player))
                        return HookResult.Continue;
                    foreach (var weapon in player.PlayerPawn.Value.WeaponServices!.MyWeapons)
                    {
                        if (weapon is { IsValid: true, Value.IsValid: true })
                        {

                            if (weapon.Value.DesignerName.Contains("bayonet") || weapon.Value.DesignerName.Contains("knife") || weapon.Value.DesignerName.Contains("awp"))
                            {
                                continue;
                            }
                            weapon.Value.Remove();
                        }
                    }
                    if (CheckIsHaveWeapon("awp", player) == false)
                    {
                        player.GiveNamedItem("weapon_awp");
                    }
                    if (!EndRound)
                    {
                        EndRound = true;
                    }
                }
            }
            if (IsRoundNumber == 2)
            {
                WriteColor($"[SPECIALROUNDS] - Starting special round {NameOfRound}.", ConsoleColor.Green);

                if (IsRound || Config.AllowBhop)
                {
                    change_cvar("sv_autobunnyhopping", "true");
                    change_cvar("sv_enablebunnyhopping", "true");
                    if (!EndRound)
                    {
                        EndRound = true;
                    }
                }
            }
            if (IsRoundNumber == 3 )
            {
                WriteColor($"[SPECIALROUNDS] - Starting special round {NameOfRound}.", ConsoleColor.Green);

                if (IsRound || Config.AllowScout)
                {
                    if (!is_alive(player))
                        return HookResult.Continue;
                    foreach (var weapon in player.PlayerPawn.Value.WeaponServices!.MyWeapons)
                    {
                        if (weapon is { IsValid: true, Value.IsValid: true })
                        {

                            if (weapon.Value.DesignerName.Contains("bayonet") || weapon.Value.DesignerName.Contains("knife"))
                            {
                                continue;
                            }
                            weapon.Value.Remove();                          
                        }
                    }
                    if (CheckIsHaveWeapon("ssg08", player) == false)
                    {
                        player.GiveNamedItem("weapon_ssg08");
                    }
                    if (!EndRound)
                    {
                        EndRound = true;
                    }
                }
            }
        }
        isset = false;
        return HookResult.Continue;
    }
    private void OnTick(CCSPlayerController player) // by Slayer <3
    {
        if (player.Pawn == null || !player.Pawn.IsValid)
            return;

        if(adminNoscope || IsRoundNumber == 1)
        {
            try
            {
                if(player.PlayerPawn.Value.WeaponServices!.MyWeapons.Count != 0)
                {
                    var ActiveWeaponName = player.PlayerPawn.Value.WeaponServices!.ActiveWeapon.Value.DesignerName;
                    if(ActiveWeaponName.Contains("weapon_ssg08") || ActiveWeaponName.Contains("weapon_awp")
                    || ActiveWeaponName.Contains("weapon_scar20") || ActiveWeaponName.Contains("weapon_g3sg1"))
                    {
                        player.PlayerPawn.Value.WeaponServices!.ActiveWeapon.Value.NextSecondaryAttackTick = Server.TickCount + 500;
                        var buttons = player.Buttons;
                        if((buttons & PlayerButtons.Attack2) != 0)
                        {
                            if(Config.NoScopeMsg)
                            {
                                Server.NextFrame(() => {
                                    player.PrintToCenter($" {ChatColors.Red}!! NO SCOPE !!");
                                });
                            }
                        }  
                    }
                }
            }
            catch(Exception ex)
            {
                WriteColor("Warning: {ex}", ConsoleColor.Green);
            }
        }
    }
    [GameEventHandler(HookMode.Pre)] // by Slayer <3
    public HookResult BulletImpact(EventBulletImpact @event, GameEventInfo info)
    {
        CCSPlayerController player = @event.Userid;
        if (player.Pawn == null || !player.Pawn.IsValid)
            return HookResult.Continue;
        if(adminNoscope || IsRoundNumber == 1)
        {
            if(player.PlayerPawn.Value.WeaponServices!.MyWeapons.Count != 0)
            {
                 try
                {
                    var ActiveWeaponName = player.PlayerPawn.Value.WeaponServices!.ActiveWeapon.Value.DesignerName;
                    if(ActiveWeaponName.Contains("weapon_ssg08") || ActiveWeaponName.Contains("weapon_awp")
                    || ActiveWeaponName.Contains("weapon_scar20") || ActiveWeaponName.Contains("weapon_g3sg1"))
                    {
                        
                        Vector PlayerPosition = player.Pawn.Value.AbsOrigin;
                        Vector BulletOrigin = new Vector(PlayerPosition.X, PlayerPosition.Y, PlayerPosition.Z+64);//bulletOrigin.X += 50.0f;
                        float[] bulletDestination = new float[3];
                        bulletDestination[0] = @event.X;
                        bulletDestination[1] = @event.Y;
                        bulletDestination[2] = @event.Z;
                        if(player.TeamNum == 3)DrawLaserBetween(player, BulletOrigin, new Vector(bulletDestination[0], bulletDestination[1], bulletDestination[2]), Color.Blue, 1.0f, 2.0f);
                        else if(player.TeamNum == 2)DrawLaserBetween(player, BulletOrigin, new Vector(bulletDestination[0], bulletDestination[1], bulletDestination[2]), Color.Red, 1.0f, 2.0f);
                    }
                }
                catch(Exception ex)
                {
                    // Logger.LogWarning($"[SLAYER Noscope] Warning: {ex}");
                }
            }
        }
        return HookResult.Continue;
    }
    // by Slayer <3
    public void DrawLaserBetween(CCSPlayerController player, Vector startPos, Vector endPos, Color color, float life, float width)
    {
        CBeam beam = Utilities.CreateEntityByName<CBeam>("beam");
        if (beam == null)
        {
            // Logger.LogError($"Failed to create beam...");
            return;
        }
        beam.Render = color;
        beam.Width = width;
        
        beam.Teleport(startPos, player.PlayerPawn.Value.AbsRotation, player.PlayerPawn.Value.AbsVelocity);
        beam.EndPos.X = endPos.X;
        beam.EndPos.Y = endPos.Y;
        beam.EndPos.Z = endPos.Z;
        beam.DispatchSpawn();
        AddTimer(life, () => { beam.Remove(); }); // destroy beam after specific time
    }
    [GameEventHandler(HookMode.Post)]
    public HookResult OnPlayerHurt(EventPlayerHurt @event, GameEventInfo info)
    {   
        CCSPlayerController player = @event.Userid;
        if (player == null || !player.Pawn.IsValid) 
        { 
            return HookResult.Continue; 
        }
        if (IsRoundNumber != 1)
        { 
            return HookResult.Continue; 
        }
        var attacker = @event.Attacker;
        var client = player.Index;
        if (@event.Weapon == "knife")
            {
                if (IsRoundNumber == 1 && @event.Userid.PlayerPawn.Value.Health + @event.DmgHealth <= 100)
                {
                    @event.Userid.PlayerPawn.Value.Health = @event.Userid.PlayerPawn.Value.Health += @event.DmgHealth;
                    if (attacker.IsValid)
                    {
                        attacker.PrintToChat($" {ChatColors.Green}SPECIAL ROUND! {ChatColors.Default}You can't {ChatColors.Red}Knife {ChatColors.Default}players!");
                    }
                }
                else
                {
                    @event.Userid.PlayerPawn.Value.Health = 100;
                }
            }
        @event.Userid.PlayerPawn.Value.VelocityModifier = 1;
        return HookResult.Continue;
    }
}
