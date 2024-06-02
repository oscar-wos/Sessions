using CounterStrikeSharp.API;
using CounterStrikeSharp.API.Core;

namespace Sessions;

public partial class Sessions
{
    private HookResult OnPlayerChat(EventPlayerChat @event, GameEventInfo info)
    {
        var player = Utilities.GetPlayerFromUserid(@event.Userid);

        if (!IsValidPlayer(player) || !Players.TryGetValue(player!.Slot, out var value) || value.Session == null)
            return HookResult.Continue;

        var messageType = @event.Teamonly ? MessageType.TeamChat : MessageType.Chat;
        Database.InsertMessage(value.Session.Id, value.Id, messageType, @event.Text);

        return HookResult.Continue;
    }
}