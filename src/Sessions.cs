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
        AddTimer(1.0f, Timer_Repeat, TimerFlags.REPEAT);
        RegisterCapabilities();

        RegisterListener<Listeners.OnMapStart>(OnMapStart);
        RegisterListener<Listeners.OnClientAuthorized>(OnClientAuthorized);
        RegisterListener<Listeners.OnClientDisconnect>(OnClientDisconnect);

        NativeAPI.HookUsermessage(
            118,
            (InputArgument)FunctionReference.Create(OnSay),
            HookMode.Pre
        );
    }

    public override void OnAllPluginsLoaded(bool isReload)
    {
        var ip = _ip.GetPublicIp();
        var port = (ushort)ConVar.Find("hostport")!.GetPrimitiveValue<int>();

        Database.StartAsync().GetAwaiter().GetResult();
        Server = Database.GetServerAsync(ip, port).GetAwaiter().GetResult();

        Server.Ip = ip;
        Server.Port = port;

        Server.Map = Database
            ?.GetMapAsync(CounterStrikeSharp.API.Server.MapName)
            .GetAwaiter()
            .GetResult();

        foreach (var player in Utilities.GetPlayers().Where(IsValidPlayer))
        {
            _ = Task.Run(async () =>
            {
                await OnPlayerConnect(
                    player.Slot,
                    player.AuthorizedSteamID!.SteamId64,
                    NativeAPI.GetPlayerIpAddress(player.Slot).Split(":")[0]
                );

                await CheckAlias(player.Slot, player.PlayerName);
            });
        }
    }

    private void Timer_Repeat()
    {
        List<int> playerIds = [];
        List<long> sessionIds = [];

        foreach (var player in Utilities.GetPlayers())
        {
            if (!IsValidPlayer(player) || !_players.TryGetValue(player.Slot, out var value))
                continue;

            playerIds.Add(value.Id);

            if (value.Session != null)
                sessionIds.Add(value.Session.Id);
        }

        _ = Task.Run(() => Database.UpdateSessionsAsync(playerIds, sessionIds));
    }

    private async Task OnPlayerConnect(int playerSlot, ulong steamId, string ip)
    {
        _players[playerSlot] = await Database.GetPlayerAsync(steamId);

        _players[playerSlot].Session = await Database.GetSessionAsync(
            _players[playerSlot].Id,
            Server!.Id,
            Server!.Map!.Id,
            ip
        );
    }

    private async Task CheckAlias(int playerSlot, string name)
    {
        if (!_players.TryGetValue(playerSlot, out var value) || value.Session == null)
            return;

        var recentAlias = await Database.GetAliasAsync(value.Id);

        if (recentAlias == null || recentAlias.Name != name)
            Database.InsertAliasAsync(value.Session.Id, value.Id, name);
    }

    private static bool IsValidPlayer(CCSPlayerController? player)
    {
        return player != null && player is { IsValid: true, IsBot: false, IsHLTV: false };
    }
}
