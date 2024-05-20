using Microsoft.Extensions.Logging;
using MySqlConnector;

namespace Sessions;

public class SqlService : IDatabase
{
    private readonly ILogger _logger;
    private readonly string _connectionString;
    private readonly MySqlConnection _connection;

    public SqlService(CoreConfig config, ILogger logger)
    {
        _logger = logger;
        _connectionString = BuildConnectionString(config);

        try
        {
            _connection = new MySqlConnection(_connectionString);
            _connection.Open();
        }
        catch (MySqlException ex)
        {
            _logger.LogError(ex, "Failed to connect to the database");
            throw;
        }
    }

    public string BuildConnectionString(CoreConfig config)
    {
        MySqlConnectionStringBuilder builder = new()
        {
            Server = config.DatabaseHost,
            Port = (uint)config.DatabasePort,
            UserID = config.DatabaseUser,
            Password = config.DatabasePassword,
            Database = config.DatabaseName,
            Keepalive = (uint)config.DatabaseKeepAlive,
            Pooling = true,
        };

        return builder.ConnectionString;
    }
}