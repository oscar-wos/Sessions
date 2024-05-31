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
        Database = new DatabaseFactory(config).Database;
    }

    public override void Load(bool hotReload)
    {
        RegisterCapabilities();

        var ip = Ip.GetPublicIp();
        var port = (ushort)ConVar.Find("hostport")!.GetPrimitiveValue<int>();

        Database.CreateTablesAsync().GetAwaiter().GetResult();
        Server = Database.GetServerAsync(ip, port).GetAwaiter().GetResult();
        Timer = AddTimer(1.0f, Timer_Repeat, TimerFlags.REPEAT);

        RegisterListener<Listeners.OnMapStart>(mapName =>
            Server.Map = Database.GetMapAsync(mapName).GetAwaiter().GetResult()
        );

        RegisterListener<Listeners.OnClientAuthorized>((playerSlot, steamId) =>
        {
            var player = Utilities.GetPlayerFromSlot(playerSlot);

            if (!IsValidPlayer(player))
                return;

            OnPlayerConnect(playerSlot, steamId.SteamId64, NativeAPI.GetPlayerIpAddress(playerSlot).Split(":")[0]).GetAwaiter().GetResult();
            CheckAlias(playerSlot, player!.PlayerName).GetAwaiter().GetResult();
        });

        RegisterListener<Listeners.OnClientDisconnect>(playerSlot =>
        {
            if (!Players.TryGetValue(playerSlot, out var value))
                return;

            Database.UpdateSeen(value.Id);
            Players.Remove(playerSlot);
        });

        RegisterEventHandler<EventPlayerChat>((@event, _) =>
        {
            var player = Utilities.GetPlayerFromUserid(@event.Userid);

            if (!IsValidPlayer(player) || !Players.TryGetValue(player!.Slot, out var value) || value.Session == null)
                return HookResult.Continue;

            var messageType = @event.Teamonly ? MessageType.TeamChat : MessageType.Chat;
            Database.InsertMessage(value.Session.Id, value.Id, messageType, @event.Text);

            return HookResult.Continue;
        });

        if (!hotReload)
            return;

        Server.Map = Database.GetMapAsync(CounterStrikeSharp.API.Server.MapName).GetAwaiter().GetResult();

        foreach (var player in Utilities.GetPlayers().Where(IsValidPlayer))
        {
            OnPlayerConnect(player.Slot, player.SteamID, NativeAPI.GetPlayerIpAddress(player.Slot).Split(":")[0]).GetAwaiter().GetResult();
            CheckAlias(player.Slot, player.PlayerName).GetAwaiter().GetResult();
        }
    }

    public async Task OnPlayerConnect(int playerSlot, ulong steamId, string ip)
    {
        Players[playerSlot] = await Database.GetPlayerAsync(steamId);
        Players[playerSlot].Session = await Database.GetSessionAsync(Players[playerSlot].Id, Server!.Id, Server.Map!.Id, ip);
    }

    public async Task CheckAlias(int playerSlot, string name)
    {
        if (!Players.TryGetValue(playerSlot, out var value) || value.Session == null)
            return;

        var recentAlias = await Database.GetAliasAsync(value.Id);

        if (recentAlias == null || recentAlias.Name != name)
            Database.InsertAlias(value.Session.Id, value.Id, name);
    }

    public void Timer_Repeat()
    {
        List<int> playerIds = [];
        List<long> sessionIds = [];

        foreach (var player in Utilities.GetPlayers())
        {
            if (!IsValidPlayer(player) || !Players.TryGetValue(player.Slot, out var value))
                continue;

            playerIds.Add(value.Id);

            if (value.Session != null)
                sessionIds.Add(value.Session.Id);
        }

        Database.UpdateSessions(playerIds, sessionIds);
    }

    private static bool IsValidPlayer(CCSPlayerController? player)
    {
        return player != null && player is { IsValid: true, IsBot: false, IsHLTV: false };
    }
}