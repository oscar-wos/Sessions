using CounterStrikeSharp.API;
using CounterStrikeSharp.API.Core;
using CounterStrikeSharp.API.Modules.Entities;

namespace Sessions;

public partial class Sessions
{
    private void OnMapStart(string mapName)
    {
        Server!.Map = Database.GetMapAsync(mapName).GetAwaiter().GetResult();
    }

    private void OnClientAuthorized(int playerSlot, SteamID steamId)
    {
        var controller = Utilities.GetPlayerFromSlot(playerSlot);

        if (!IsValidPlayer(controller))
            return;

        PlayerConnect(playerSlot, steamId.SteamId64, controller!.IpAddress!.Split(":")[0]).GetAwaiter().GetResult();
        CheckAlias(playerSlot, controller.PlayerName).GetAwaiter().GetResult();
    }

    private void OnClientDisconnect(int playerSlot)
    {
        if (!Players.TryGetValue(playerSlot, out var value))
            return;

        Database.UpdateSeen(value.Id);
        Players.Remove(playerSlot);
    }
}