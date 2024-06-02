using CounterStrikeSharp.API;
using CounterStrikeSharp.API.Core;
using CounterStrikeSharp.API.Modules.Cvars;
using CounterStrikeSharp.API.Modules.Timers;

namespace Sessions;

public partial class Sessions : BasePlugin, IPluginConfig<SessionsConfig>
{
    public void OnConfigParsed(SessionsConfig config)
    {
        Database = new DatabaseFactory(config, this).Database;
    }

    public override void Load(bool isReload)
    {
        var ip = _ip.GetPublicIp()!;
        var port = (ushort)ConVar.Find("hostport")!.GetPrimitiveValue<int>();

        Database.StartAsync().GetAwaiter().GetResult();
        Server = Database.GetServerAsync(ip, port).GetAwaiter().GetResult();
        Server.Ip = ip;
        Server.Port = port;

        RegisterCapabilities();
        RegisterListener<Listeners.OnMapStart>(OnMapStart);
        RegisterListener<Listeners.OnClientAuthorized>(OnClientAuthorized);
        RegisterListener<Listeners.OnClientDisconnect>(OnClientDisconnect);
        RegisterEventHandler<EventPlayerChat>(OnPlayerChat);
        AddTimer(1.0f, Timer_Repeat, TimerFlags.REPEAT);

        if (!isReload)
            return;

        Server.Map = Database.GetMapAsync(CounterStrikeSharp.API.Server.MapName).GetAwaiter().GetResult();

        foreach (var player in Utilities.GetPlayers().Where(IsValidPlayer))
        {
            OnPlayerConnect(player.Slot, player.AuthorizedSteamID!.SteamId64, NativeAPI.GetPlayerIpAddress(player.Slot).Split(":")[0]).GetAwaiter().GetResult();
            CheckAlias(player.Slot, player.PlayerName).GetAwaiter().GetResult();
        }
    }

    private void Timer_Repeat()
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

    private async Task OnPlayerConnect(int playerSlot, ulong steamId, string ip)
    {
        Players[playerSlot] = await Database.GetPlayerAsync(steamId);
        Players[playerSlot].Session = await Database.GetSessionAsync(Players[playerSlot].Id, Server!.Id, Server!.Map!.Id, ip);
    }

    private async Task CheckAlias(int playerSlot, string name)
    {
        if (!Players.TryGetValue(playerSlot, out var value) || value.Session == null)
            return;

        var recentAlias = await Database.GetAliasAsync(value.Id);

        if (recentAlias == null || recentAlias.Name != name)
            Database.InsertAlias(value.Session.Id, value.Id, name);
    }

    private static bool IsValidPlayer(CCSPlayerController? player)
    {
        return player != null && player is { IsValid: true, IsBot: false, IsHLTV: false };
    }
}