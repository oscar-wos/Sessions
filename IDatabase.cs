using Microsoft.Extensions.Logging;

namespace Sessions;

public interface IDatabase
{
    string BuildConnectionString(CoreConfig config);
}

public interface IDatabaseQueries
{
}

public interface IDatabaseFactory
{
    IDatabase Database { get; }
    ILogger Logger { get; }
}

public class DatabaseFactory : IDatabaseFactory
{
    private readonly IDatabase _database;
    private readonly ILogger _logger;

    public DatabaseFactory(CoreConfig config)
    {
        CheckConfig(config);

        var loggerFactory = LoggerFactory.Create(builder => builder.AddConsole());
        _logger = loggerFactory.CreateLogger<DatabaseFactory>();

        _database = config.DatabaseType switch
        {
            "postgres" => new PostgresService(config, _logger),
            "mysql" => new SqlService(config, _logger),
            _ => throw new InvalidOperationException("Database type is not supported"),
        };
    }

    public IDatabase Database => _database;
    public ILogger Logger => _logger;

    private static void CheckConfig(CoreConfig config)
    {
        if (string.IsNullOrWhiteSpace(config.DatabaseType) ||
            string.IsNullOrWhiteSpace(config.DatabaseHost) ||
            string.IsNullOrWhiteSpace(config.DatabaseUser) ||
            string.IsNullOrWhiteSpace(config.DatabaseName) ||
            config.DatabasePort == 0)
        {
            throw new InvalidOperationException("Database is not set in the configuration file");
        }
    }
}