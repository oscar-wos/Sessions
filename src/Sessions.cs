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
        Server.MapName = CounterStrikeSharp.API.Server.MapName;

        RegisterCapabilities();
        RegisterListener<Listeners.OnMapStart>(OnMapStart);
        RegisterListener<Listeners.OnClientAuthorized>(OnClientAuthorized);
        RegisterListener<Listeners.OnClientDisconnect>(OnClientDisconnect);
        RegisterEventHandler<EventPlayerChat>(OnPlayerChat);
        AddTimer(1.0f, Timer_Repeat, TimerFlags.REPEAT);

        if (!isReload)
            return;

        Server.Map = Database.GetMapAsync(Server.MapName).GetAwaiter().GetResult();

        foreach (var controller in Utilities.GetPlayers().Where(IsValidPlayer))
        {
            PlayerConnect(controller.Slot, controller.AuthorizedSteamID!.SteamId64, controller.IpAddress!.Split(":")[0]).GetAwaiter().GetResult();
            CheckAlias(controller.Slot, controller.PlayerName).GetAwaiter().GetResult();
        }
    }

    private void Timer_Repeat()
    {
        List<int> playerIds = [];
        List<long> sessionIds = [];

        foreach (var controller in Utilities.GetPlayers().Where(IsValidPlayer))
        {
            if (!Players.TryGetValue(controller.Slot, out var value))
                continue;

            playerIds.Add(value.Id);

            if (value.Session != null)
                sessionIds.Add(value.Session.Id);
        }

        Database.UpdateSessions(playerIds, sessionIds);
    }

    private async Task PlayerConnect(int playerSlot, ulong steamId, string ip)
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