using CounterStrikeSharp.API;
using CounterStrikeSharp.API.Core;
using CounterStrikeSharp.API.Modules.Cvars;
using CounterStrikeSharp.API.Modules.Timers;
using Microsoft.Extensions.Logging;

namespace Sessions;

public partial class Sessions : BasePlugin
{
    public void OnConfigParsed(SessionsConfig config)
    {
        Database = new DatabaseFactory(config, this).Database;
    }

    public override void Load(bool isReload)
    {
        RegisterCapabilities();

        var ip = _ip.GetPublicIp();
        var port = (ushort)ConVar.Find("hostport")!.GetPrimitiveValue<int>();

        //#if DEBUG
        Logger.LogInformation($"{nameof(Ip)}: ${ip}:${port}");
        //#endif

        Database.CreateTablesAsync().GetAwaiter().GetResult();
        _server = Database.GetServerAsync(ip, port).GetAwaiter().GetResult();
        AddTimer(1.0f, Timer_Repeat, TimerFlags.REPEAT);
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

    private static bool IsValidPlayer(CCSPlayerController? player)
    {
        return player != null && player is { IsValid: true, IsBot: false, IsHLTV: false };
    }
}