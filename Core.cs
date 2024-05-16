using System.Numerics;
using CounterStrikeSharp.API;
using CounterStrikeSharp.API.Core;
using CounterStrikeSharp.API.Core.Attributes;
using CounterStrikeSharp.API.Core.Attributes.Registration;
using CounterStrikeSharp.API.Core.Translations;
using CounterStrikeSharp.API.Modules.Commands;
using Microsoft.Extensions.Logging;

namespace Core;

// [MinimumApiVersion(160)]
public partial class Core : BasePlugin, IPluginConfig<CoreConfig>
{
    public CoreConfig Config { get; set; } = new();
    internal static PostgresService? _postgresService;

    public override string ModuleName => "Core";
    public override string ModuleDescription => "";
    public override string ModuleAuthor => "Oscar Wos-Szlaga";
    public override string ModuleVersion => "0.0.1";

    
    public async void OnConfigParsed(CoreConfig config)
	{
        _postgresService = new PostgresService(config);
        await _postgresService.InitConnectAsync();

        Config = config;
    }

    public override void Load(bool hotReload)
    {
    }

    [ConsoleCommand("css_hello", "Say hello in the player language")]
    public async void OnCommandHello(CCSPlayerController? player, CommandInfo command)
    {
        if (player != null)
        {
            ulong steamId = player.SteamID;

            var playerData = await _postgresService!.GetPlayerBySteamId(steamId);
            Console.WriteLine(playerData);
        }
        /*
        if (player != null)
        {
            string playerName = player.PlayerName;
            var playerLanguage = player.GetLanguage();
            ulong playerSteamId = player.SteamID;
            string commandString = command.GetArg(0).ToLower();

            _ = CheckPlayerCommand(commandString, playerName, playerSteamId);

            command.ReplyToCommand($"{Localizer["hello.player", playerName, playerLanguage]}");
        }
        */
    }
}