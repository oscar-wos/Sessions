using Microsoft.Extensions.Logging;
using System.Text.RegularExpressions;

namespace Sessions;

public class DatabaseFactory
{
    public DatabaseFactory(SessionsConfig config, Sessions plugin)
    {
        if (!CheckConfig(config))
            throw new InvalidOperationException("Database is not set in the configuration file");

        //#if DEBUG
        plugin.Logger.LogInformation($"Checked: {config.DatabaseType} SSL: {config.DatabaseSsl.ToString()} " +
                                     $"{config.DatabaseUser}@{config.DatabaseHost}:{config.DatabasePort} " +
                                     $"{config.DatabaseName}:{Regex.Replace(config.DatabasePassword, ".", "*")}");
        //#endif

        var loggerFactory = LoggerFactory.Create(builder => builder.AddConsole());
        ILogger logger = loggerFactory.CreateLogger<DatabaseFactory>();

        Database = config.DatabaseType switch
        {
            "postgres" => new PostgresService(config, logger),
            "mysql" => new SqlService(config, logger),
            _ => throw new InvalidOperationException("Database type is not supported"),
        };
    }

    private static bool CheckConfig(SessionsConfig config)
    {
        return !string.IsNullOrWhiteSpace(config.DatabaseType)
               && !string.IsNullOrWhiteSpace(config.DatabaseHost)
               && !string.IsNullOrWhiteSpace(config.DatabaseUser)
               && !string.IsNullOrWhiteSpace(config.DatabaseName)
               && config.DatabasePort != 0;
    }

    public IDatabase Database { get; }
}