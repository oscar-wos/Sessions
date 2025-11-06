using CounterStrikeSharp.API;
using CounterStrikeSharp.API.Core;
using CounterStrikeSharp.API.Modules.Entities;
using CounterStrikeSharp.API.Modules.Menu;
using CounterStrikeSharp.API.Modules.UserMessages;
using Microsoft.Extensions.Logging;

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

        _ = Task.Run(async () =>
        {
            await OnPlayerConnect(
                playerSlot,
                steamId.SteamId64,
                NativeAPI.GetPlayerIpAddress(playerSlot).Split(":")[0]
            );

            await CheckAlias(playerSlot, controller!.PlayerName);
        });
    }

    private void OnClientDisconnect(int playerSlot)
    {
        if (!_players.TryGetValue(playerSlot, out var value))
            return;

        _ = Task.Run(async () =>
        {
            Database.UpdateSeenAsync(value.Id);
            _players.Remove(playerSlot);
        });
    }

    private HookResult OnSay(UserMessage um)
    {
        int index = um.ReadInt("entityindex");
        bool chat = um.ReadBool("chat");
        string message = um.ReadString("param2");

        if (Utilities.GetPlayerFromIndex(index) is not { IsValid: true } controller)
            return HookResult.Continue;

        if (!Players.TryGetValue(controller.Slot, out var value) || value.Session == null)
            return HookResult.Continue;

        _ = Task.Run(async () =>
            Database.InsertMessageAsync(
                value.Session.Id,
                value.Id,
                chat ? MessageType.TeamChat : MessageType.Chat,
                message
            )
        );

        return HookResult.Continue;
    }
}
