using CounterStrikeSharp.API;
using CounterStrikeSharp.API.Core;
using CounterStrikeSharp.API.Modules.Cvars;
using CounterStrikeSharp.API.Modules.Timers;

namespace Sessions;

public partial class Sessions : BasePlugin, IPluginConfig<CoreConfig>
{
    public CoreConfig Config { get; set; } = new();

    public override string ModuleName => "Sessions";
    public override string ModuleDescription => "Track player sessions";
    public override string ModuleAuthor => "github.com/oscar-wos/Sessions";
    public override string ModuleVersion => "1.2.1";

    public required IDatabase _database;
    public readonly Ip _ip = new();
    public ServerSQL? _server;
    public Dictionary<int, PlayerSQL> _players = [];
    public CounterStrikeSharp.API.Modules.Timers.Timer? _timer;
    
    public void OnConfigParsed(CoreConfig config)
    {
        Config = config;

        _database = new DatabaseFactory(config).Database;
        _database.CreateTablesAsync();
    }

    public override void Load(bool hotReload)
    {
        string ip = _ip.GetPublicIp();
        ushort port = (ushort)ConVar.Find("hostport")!.GetPrimitiveValue<int>();
        _server = _database!.GetServerAsync(ip, port).GetAwaiter().GetResult();

        RegisterListener<Listeners.OnMapStart>(mapName =>
            _server.Map = _database.GetMapAsync(mapName).GetAwaiter().GetResult()
        );

        RegisterListener<Listeners.OnClientAuthorized>(async (playerSlot, steamId) =>
            await OnPlayerConnect(playerSlot, steamId.SteamId64, NativeAPI.GetPlayerIpAddress(playerSlot).Split(":")[0])
        );

        RegisterListener<Listeners.OnClientDisconnect>(playerSlot =>
        {
            if (!_players.TryGetValue(playerSlot, out PlayerSQL? value))
                return;

            _database.UpdateSeenAsync(value.Id);
            _players.Remove(playerSlot);
        });

        _timer = AddTimer(1.0f, Timer_Repeat, TimerFlags.REPEAT);

        if (!hotReload)
            return;
            
        _server.Map = _database.GetMapAsync(Server.MapName).GetAwaiter().GetResult();
        Utilities.GetPlayers().Where(player => !player.IsBot).ToList().ForEach(player => OnPlayerConnect(player.Slot, player.SteamID, NativeAPI.GetPlayerIpAddress(player.Slot).Split(":")[0]).GetAwaiter().GetResult());
    }
    
    public void Timer_Repeat()
    {
        int[] sessionIds = Utilities.GetPlayers().Where(player => _players.TryGetValue(player.Slot, out PlayerSQL? p) && p.Session != null).Select(player => _players[player.Slot].Session!.Id).ToArray();
        _database.UpdateSessionsBulkAsync(sessionIds);
    }

    public async Task OnPlayerConnect(int playerSlot, ulong steamId, string ip)
    {
        _players[playerSlot] = await _database.GetPlayerAsync(steamId);
        _players[playerSlot].Session = await _database.GetSessionAsync(_players[playerSlot].Id, _server!.Id, _server.Map!.Id, ip);
    }
}