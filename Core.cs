using CounterStrikeSharp.API;
using CounterStrikeSharp.API.Core;
using CounterStrikeSharp.API.Core.Attributes.Registration;

namespace Core;

// [MinimumApiVersion(160)]
public partial class Core : BasePlugin, IPluginConfig<CoreConfig>
{
    internal static PostgresService? _postgresService;
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
        _map = _postgresService.GetMapByMapNameAsync(Server.MapName).GetAwaiter().GetResult();

        Config = config;
    }

    public override void Load(bool hotReload)
    {
        if (!hotReload)
            return;

        foreach (CCSPlayerController player in Utilities.GetPlayers())
            OnPlayerConnect(player);
    }

    [GameEventHandler]
    public HookResult OnPlayerConnectFull(EventPlayerConnectFull @event, GameEventInfo info)
    {
        OnPlayerConnect(@event.Userid!);
        return HookResult.Continue;
    }

    public async void OnPlayerConnect(CCSPlayerController player)
    {
        if (player == null || player.IsBot)
            return;

        PlayerSQL playerSQL = await _postgresService!.GetPlayerBySteamIdAsync(player.SteamID);
        _players[player.Slot] = playerSQL;

        SessionSQL sessionSQL = await _postgresService.GetSessionAsync(playerSQL.Id, _map.Id);
        _players[player.Slot].Session = sessionSQL;
    }
}