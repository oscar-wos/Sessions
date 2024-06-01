using CounterStrikeSharp.API;
using CounterStrikeSharp.API.Core;
using CounterStrikeSharp.API.Core.Capabilities;
using Sessions.API;

namespace TestPlugin;

public class TestPlugin : BasePlugin
{
    public override string ModuleName => "TestPlugin";
    public override string ModuleVersion => "1.0.0";

    private static PlayerCapability<ISessionsPlayer> CapabilityPlayer { get; } = new("sessions:player");

    public override void Load(bool isReload)
    {
        foreach (var session in Utilities.GetPlayers().Select(player => CapabilityPlayer.Get(player)!.Session))
        {
            Console.WriteLine(session!.Id);
        }
    }
}