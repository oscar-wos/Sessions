using CounterStrikeSharp.API;
using CounterStrikeSharp.API.Core;
using CounterStrikeSharp.API.Modules.Cvars;
using CounterStrikeSharp.API.Modules.Timers;

namespace Sessions;

public partial class Sessions : BasePlugin, IPluginConfig<SessionsConfig>
{
    public SessionsConfig Config { get; set; } = new();

    public void OnConfigParsed(SessionsConfig config)
    {
        _database = new DatabaseFactory(config).Database;
    }

    public override void Load(bool hotReload)
    {
        string ip = _ip.GetPublicIp();
        ushort port = (ushort)ConVar.Find("hostport")!.GetPrimitiveValue<int>();

        _database.CreateTablesAsync().GetAwaiter().GetResult();
        _server = _database.GetServerAsync(ip, port).GetAwaiter().GetResult();
        _timer = AddTimer(1.0f, Timer_Repeat, TimerFlags.REPEAT);

        RegisterListener<Listeners.OnMapStart>(mapName =>
            _server.Map = _database.GetMapAsync(mapName).GetAwaiter().GetResult()
        );

        RegisterListener<Listeners.OnClientAuthorized>((playerSlot, steamId) =>
        {
            CCSPlayerController? player = Utilities.GetPlayerFromSlot(playerSlot);

            if (!IsValidPlayer(player))
                return;

            OnPlayerConnect(playerSlot, steamId.SteamId64, NativeAPI.GetPlayerIpAddress(playerSlot).Split(":")[0]).GetAwaiter().GetResult();
            CheckAlias(playerSlot, player!.PlayerName).GetAwaiter().GetResult();
        });

        RegisterListener<Listeners.OnClientDisconnect>(playerSlot =>
        {
            if (!_players.TryGetValue(playerSlot, out PlayerSQL? value))
                return;

            _database.UpdateSeen(value.Id);
            _players.Remove(playerSlot);
        });

        RegisterEventHandler<EventPlayerChat>((@event, info) =>
        {
            CCSPlayerController? player = Utilities.GetPlayerFromUserid(@event.Userid);

            if (!IsValidPlayer(player) || !_players.TryGetValue(player!.Slot, out PlayerSQL? value) || value.Session == null)
                return HookResult.Continue;

            MessageType messageType = @event.Teamonly ? MessageType.TeamChat : MessageType.Chat;
            _database.InsertMessage(value.Session.Id, value.Id, messageType, @event.Text);

            return HookResult.Continue;
        });

        if (!hotReload)
            return;

        _server.Map = _database.GetMapAsync(Server.MapName).GetAwaiter().GetResult();

        foreach (CCSPlayerController player in Utilities.GetPlayers())
        {
            OnPlayerConnect(player.Slot, player.SteamID, NativeAPI.GetPlayerIpAddress(player.Slot).Split(":")[0]).GetAwaiter().GetResult();
            CheckAlias(player.Slot, player.PlayerName).GetAwaiter().GetResult();
        }
    }

    public async Task OnPlayerConnect(int playerSlot, ulong steamId, string ip)
    {
        _players[playerSlot] = await _database.GetPlayerAsync(steamId);
        _players[playerSlot].Session = await _database.GetSessionAsync(_players[playerSlot].Id, _server!.Id, _server.Map!.Id, ip);
    }

    public async Task CheckAlias(int playerSlot, string alias)
    {
        if (!_players.TryGetValue(playerSlot, out PlayerSQL? value) || value.Session == null)
            return;

        AliasSQL? recentAlias = await _database.GetAliasAsync(value.Id);

        if (recentAlias == null || recentAlias.Alias != alias)
            _database.InsertAlias(value.Session.Id, value.Id, alias);
    }

    public void Timer_Repeat()
    {
        List<int> playerIds = [];
        List<long> sessionIds = [];

        foreach (CCSPlayerController player in Utilities.GetPlayers())
        {
            if (!_players.TryGetValue(player.Slot, out PlayerSQL? value))
                continue;

            playerIds.Add(value.Id);

            if (value.Session != null)
                sessionIds.Add(value.Session.Id);
        }

        _database.UpdateSessions(playerIds, sessionIds);
    }

    private static bool IsValidPlayer(CCSPlayerController? player)
    {
        return player != null && player.IsValid && !player.IsBot && !player.IsHLTV;
    }
}