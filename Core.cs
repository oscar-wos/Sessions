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
    internal static PostgresService? _postgresService;
    public CoreConfig Config { get; set; } = new();
    public Map _map = new();

    public override string ModuleName => "Core";
    public override string ModuleDescription => "";
    public override string ModuleAuthor => "Oscar Wos-Szlaga";
    public override string ModuleVersion => "0.0.1";
    
    public void OnConfigParsed(CoreConfig config)
	{
        _postgresService = new PostgresService(config);
        _postgresService.InitConnectAsync().GetAwaiter().GetResult();
        _map = _postgresService.GetMapByMapName(Server.MapName).GetAwaiter().GetResult();

        Config = config;
    }

    public override void Load(bool hotReload)
    {
    }
}