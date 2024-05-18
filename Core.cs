using CounterStrikeSharp.API;
using CounterStrikeSharp.API.Core;
using CounterStrikeSharp.API.Core.Attributes.Registration;
using CounterStrikeSharp.API.Modules.Entities;
using CounterStrikeSharp.API.Modules.Timers;

namespace Core;

// [MinimumApiVersion(160)]
public partial class Core : BasePlugin, IPluginConfig<CoreConfig>
{
    internal static PostgresService? _postgresService;
    public CounterStrikeSharp.API.Modules.Timers.Timer? _timer;
    public CoreConfig Config { get; set; } = new();

    public override string ModuleName => "Core";
    public override string ModuleDescription => "";
    public override string ModuleAuthor => "Oscar Wos-Szlaga";
    public override string ModuleVersion => "0.0.1";

    private MapSQL _map = new();
    private Dictionary<int, PlayerSQL> _players = [];
    
    public void OnConfigParsed(CoreConfig config)
	{
        _postgresService = new PostgresService(config);
        _postgresService.InitConnectAsync().GetAwaiter().GetResult();
        _timer = AddTimer(1.0f, Timer_Repeat, TimerFlags.REPEAT);

        Config = config;
    }

    public void Timer_Repeat()
    {

    }   

    public override void Load(bool hotReload)
    {
        RegisterListener<Listeners.OnMapStart>(mapName =>
            _map = _postgresService!.GetMapByMapNameAsync(mapName).GetAwaiter().GetResult()
        );

        RegisterListener<Listeners.OnClientAuthorized>((playerSlot, steamId) =>
            OnPlayerConnect(playerSlot, steamId.SteamId64).GetAwaiter().GetResult()
        );
        
        if (!hotReload)
            return;
        
        _map = _postgresService!.GetMapByMapNameAsync(Server.MapName).GetAwaiter().GetResult();

        foreach (CCSPlayerController player in Utilities.GetPlayers())
        {
            if (player == null || player.IsBot)
                continue;

            OnPlayerConnect(player.Slot, player.SteamID).GetAwaiter().GetResult();
        }
    }

    public async Task OnPlayerConnect(int playerSlot, ulong steamId)
    {
        _players[playerSlot] = await _postgresService!.GetPlayerBySteamIdAsync(steamId);
        _players[playerSlot].Session = await _postgresService.GetSessionAsync(_players[playerSlot].Id, _map.Id);
    }

    [GameEventHandler]
    public async void OnClientDisconnect(CCSPlayerController player)
    {
        if (player == null || player.IsBot || !_players.TryGetValue(player.Slot, out PlayerSQL? value))
            return;

        await _postgresService!.UpdateSeenAsync(value.Id);
        _players.Remove(player.Slot);
    }
}