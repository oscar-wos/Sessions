using Microsoft.Extensions.Logging;
using Npgsql;

namespace Sessions;

public class PostgresService : IDatabase
{
    private readonly ILogger _logger;
    private readonly string _connectionString;
    private readonly NpgsqlConnection _connection;

    public PostgresService(CoreConfig config, ILogger logger)
    {
        _logger = logger;
        _connectionString = BuildConnectionString(config);

        try
        {
            _connection = new NpgsqlConnection(_connectionString);
            _connection.Open();
        }
        catch (NpgsqlException ex)
        {
            _logger.LogError(ex, "Failed to connect to the database");
            throw;
        }
    }

    public string BuildConnectionString(CoreConfig config)
    {
        NpgsqlConnectionStringBuilder builder = new()
        {
            Host = config.DatabaseHost,
            Port = config.DatabasePort,
            Username = config.DatabaseUser,
            Password = config.DatabasePassword,
            Database = config.DatabaseName,
            KeepAlive = config.DatabaseKeepAlive,
            Pooling = true,
        };

        return builder.ConnectionString;
    }
}