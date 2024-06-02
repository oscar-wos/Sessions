using CounterStrikeSharp.API.Core;
using CounterStrikeSharp.API;

namespace Sessions;

public partial class Sessions
{
    private void OnMapStart(string mapName)
    {
        Server!.Map = Database.GetMapAsync(mapName).GetAwaiter().GetResult();
    }

    private void OnClientAuthorized(int playerSlot, CounterStrikeSharp.API.Modules.Entities.SteamID steamId)
    {
        var player = Utilities.GetPlayerFromSlot(playerSlot);

        if (!IsValidPlayer(player))
            return;

        OnPlayerConnect(playerSlot, steamId.SteamId64, NativeAPI.GetPlayerIpAddress(playerSlot).Split(":")[0]).GetAwaiter().GetResult();
        CheckAlias(playerSlot, player!.PlayerName).GetAwaiter().GetResult();
    }

    private void OnClientDisconnect(int playerSlot)
    {
        if (!Players.TryGetValue(playerSlot, out var value))
            return;

        Database.UpdateSeen(value.Id);
        Players.Remove(playerSlot);
    }
}