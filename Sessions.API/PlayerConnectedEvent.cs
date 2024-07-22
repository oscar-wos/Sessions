using CounterStrikeSharp.API.Core;
using Sessions.API;

public record PlayerConnectedEvent(CCSPlayerController Controller, Player Player);