using CounterStrikeSharp.API;
using CounterStrikeSharp.API.Core;
using CounterStrikeSharp.API.Modules.Cvars;
using CounterStrikeSharp.API.Modules.Timers;

namespace Core;

// [MinimumApiVersion(160)]
public partial class Core : BasePlugin, IPluginConfig<CoreConfig>
{
    internal static PostgresService? _postgresService;
    public CoreConfig Config { get; set; } = new();

    public override string ModuleName => "Sessions";
    public override string ModuleDescription => "Track player sessions";
    public override string ModuleAuthor => "Oscar Wos-Szlaga";
    public override string ModuleVersion => "1.1.0";

    private readonly Ip _ip = new();
    private ServerSQL _server = new();
    public Dictionary<int, PlayerSQL> _players = [];
    public CounterStrikeSharp.API.Modules.Timers.Timer? _timer;
    
    public void OnConfigParsed(CoreConfig config)
	{
        _postgresService = new PostgresService(config);
        _postgresService.InitConnectAsync().GetAwaiter().GetResult();

        Config = config;
    }

    public override void Load(bool hotReload)
    {
        string ip = _ip.GetPublicIp();
        ushort port = (ushort)ConVar.Find("hostport")!.GetPrimitiveValue<int>();
        _server = _postgresService!.GetServerAsync(ip, port).GetAwaiter().GetResult();

        RegisterListener<Listeners.OnMapStart>(mapName =>
            _server.Map = _postgresService!.GetMapByMapNameAsync(mapName).GetAwaiter().GetResult()
        );

        RegisterListener<Listeners.OnClientAuthorized>((playerSlot, steamId) =>
            OnPlayerConnect(playerSlot, steamId.SteamId64).GetAwaiter().GetResult()
        );

        RegisterListener<Listeners.OnClientDisconnect>(playerSlot =>
        {
            _postgresService!.UpdateSeenAsync(_players[playerSlot].Id);
            _players.Remove(playerSlot);
        });
        
        _timer = AddTimer(1.0f, Timer_Repeat, TimerFlags.REPEAT);

        if (!hotReload)
            return;
        
        _server.Map = _postgresService!.GetMapByMapNameAsync(Server.MapName).GetAwaiter().GetResult();
        Utilities.GetPlayers().Where(player => !player.IsBot).ToList().ForEach(player => OnPlayerConnect(player.Slot, player.SteamID).GetAwaiter().GetResult());
    }
    
    public void Timer_Repeat()
    {
        int[] sessionIds = Utilities.GetPlayers().Where(player => _players[player.Slot].Session != null).Select(player => _players[player.Slot].Session!.Id).ToArray();
        _postgresService!.UpdatePlayedBulkAsync(sessionIds);
    }

    public async Task OnPlayerConnect(int playerSlot, ulong steamId)
    {
        _players[playerSlot] = await _postgresService!.GetPlayerBySteamIdAsync(steamId);
        _players[playerSlot].Session = await _postgresService.GetSessionAsync(_players[playerSlot].Id, _server.Id, _server.Map!.Id);
    }
}