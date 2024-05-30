using CounterStrikeSharp.API;
using CounterStrikeSharp.API.Core;
using CounterStrikeSharp.API.Core.Capabilities;
using Sessions.API;

namespace TestPlugin;

public class TestPlugin : BasePlugin
{
    public override string ModuleName => "TestPlugin";
    public override string ModuleVersion => "1.0.0";

    public static PlayerCapability<ISessionsPlayer> Capability_Player { get; } = new("sessions:player");

    public override void Load(bool isReload)
    {
        foreach (CCSPlayerController player in Utilities.GetPlayers())
        {
            var temp = Capability_Player.Get(player);
            var temp2 = temp.SessionSQL;

            Console.WriteLine(temp2.Id);
        }
    }
}