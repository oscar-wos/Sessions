using Dapper;
using Npgsql;
using Microsoft.Extensions.Logging;

namespace Core;

public class PostgresService
{
    private readonly ILogger<PostgresService> _logger;
    private readonly CoreConfig _config;
    private readonly string _connectionString;
    private string[] _tables = ["players", "maps", "sessions", "servers"];

    private readonly NpgsqlConnection _connection;

    private readonly string players = @"CREATE TABLE IF NOT EXISTS players (
        id SERIAL,
        steam_id BIGINT NOT NULL PRIMARY KEY,
        first_seen TIMESTAMP WITH TIME ZONE NOT NULL DEFAULT CURRENT_TIMESTAMP,
        last_seen TIMESTAMP WITH TIME ZONE NOT NULL DEFAULT CURRENT_TIMESTAMP
    )";

    private readonly string servers = @"CREATE TABLE IF NOT EXISTS servers (
        id SMALLSERIAL PRIMARY KEY,
        server_ip INET NOT NULL,
        server_port SMALLINT NOT NULL
    )";

    private readonly string maps = @"CREATE TABLE IF NOT EXISTS maps (
        id SMALLSERIAL,
        map_name VARCHAR(255) NOT NULL PRIMARY KEY
    )";

    private readonly string sessions = @"CREATE TABLE IF NOT EXISTS sessions (
        id BIGSERIAL PRIMARY KEY,
        player_id INT NOT NULL,
        server_id SMALLINT NOT NULL,
        map_id SMALLINT NOT NULL,
        start_time TIMESTAMP WITH TIME ZONE NOT NULL DEFAULT CURRENT_TIMESTAMP,
        end_time TIMESTAMP WITH TIME ZONE NOT NULL DEFAULT CURRENT_TIMESTAMP
    )";

    /*
    private readonly string aliases = @"CREATE TABLE IF NOT EXISTS aliases (
        id BIGSERIAL PRIMARY KEY,
        player_id INT NOT NULL,
        alias VARCHAR(255) NOT NULL,
        timestamp TIMESTAMP WITH TIME ZONE NOT NULL DEFAULT CURRENT_TIMESTAMP
    )";
    */

    public PostgresService(CoreConfig config)
    {   
        var loggerFactory = LoggerFactory.Create(builder => builder.AddConsole());
        _logger = loggerFactory.CreateLogger<PostgresService>();

        _config = config;
        _connectionString = BuildPostgresConnectionString();

        try
        {
            _connection = new NpgsqlConnection(_connectionString);
            _connection.Open();
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error while opening database connection");
            throw;
        }
    }
    
    private string BuildPostgresConnectionString()
    {
        if (string.IsNullOrWhiteSpace(_config.DatabaseHost) ||
            string.IsNullOrWhiteSpace(_config.DatabaseUser) ||
            string.IsNullOrWhiteSpace(_config.DatabaseName) ||
            _config.DatabasePort == 0)
        {
            throw new InvalidOperationException("Database is not set in the configuration file");
        }

        NpgsqlConnectionStringBuilder builder = new()
        {
            Host = _config.DatabaseHost,
            Port = _config.DatabasePort,
            Username = _config.DatabaseUser,
            Password = _config.DatabasePassword,
            Database = _config.DatabaseName,
            KeepAlive = _config.DatabaseKeepAlive,
            Pooling = true,
        };

        return builder.ConnectionString;
    }

    public async Task InitConnectAsync()
    {
        try
        {
            await using NpgsqlTransaction tx = await _connection.BeginTransactionAsync();

            try
            {
                foreach (var table in _tables)
                {
                    string createTableQuery = table switch
                    {
                        "players" => players,
                        "maps" => maps,
                        "sessions" => sessions,
                        "servers" => servers,
                        //"aliases" => aliases,
                        _ => throw new InvalidOperationException($"Unknown table: {table}")
                    };

                    await _connection.ExecuteAsync(createTableQuery, transaction: tx);
                }

                await tx.CommitAsync();
            }
            catch (Exception ex)
            {  
                _logger.LogError(ex, "Error while checking database table");
                await tx.RollbackAsync();
                throw;
            }
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error while opening database connection");
            throw;
        }
    }

    public async Task<ServerSQL> GetServerAsync(string serverIp, ushort serverPort)
    {
        try
        {
            short serverPortSigned = (short)(serverPort - 0x8000);
            ServerSQL? result = await _connection.QueryFirstOrDefaultAsync<ServerSQL>("SELECT id FROM servers WHERE server_ip = CAST(@ServerIp as INET) AND server_port = @ServerPort", new { ServerIp = serverIp, ServerPort = serverPortSigned });

            if (result != null)
                return result;

            return await _connection.QuerySingleAsync<ServerSQL>("INSERT INTO servers (server_ip, server_port) VALUES (CAST(@ServerIp as INET), @ServerPort) RETURNING id", new { ServerIp = serverIp, ServerPort = serverPortSigned });
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error while getting server");
            throw;
        }
    }

    public async Task<PlayerSQL> GetPlayerBySteamIdAsync(ulong steamId)
    {
        try
        {
            PlayerSQL? result = await _connection.QueryFirstOrDefaultAsync<PlayerSQL>("SELECT id, first_seen, last_seen FROM players WHERE steam_id = @SteamId", new { SteamId = (long)steamId });

            if (result != null)
                return result;
            
            return await _connection.QuerySingleAsync<PlayerSQL>("INSERT INTO players (steam_id) VALUES (@SteamId) RETURNING id, first_seen, last_seen", new { SteamId = (long)steamId });
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error while getting player");
            throw;
        } 
    }

    public async Task<MapSQL> GetMapByMapNameAsync(string mapName)
    {
        try
        {
            MapSQL? result = await _connection.QueryFirstOrDefaultAsync<MapSQL>("SELECT id FROM maps WHERE map_name = @MapName", new { MapName = mapName });
        
            if (result != null)
                return result;

            return await _connection.QuerySingleAsync<MapSQL>("INSERT INTO maps (map_name) VALUES (@MapName) RETURNING id", new { MapName = mapName });                
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error while getting map");
            throw;
        } 
    }

    public async Task<SessionSQL> GetSessionAsync(int playerId, int serverId, int mapId)
    {
        try
        {
            return await _connection.QuerySingleAsync<SessionSQL>("INSERT INTO sessions (player_id, server_id, map_id) VALUES (@PlayerId, @ServerId, @MapId) RETURNING id", new { PlayerId = playerId, ServerId = serverId, MapId = mapId });
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error while getting session");
            throw;
        }
    }

    public async void UpdateSeenAsync(int playerId)
    {
        try
        {
            await _connection.ExecuteAsync("UPDATE players SET last_seen = NOW() WHERE id = @PlayerId", new { PlayerId = playerId });
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error while updating seen");
            throw;
        }
    }

    public async void UpdatePlayedBulkAsync(int[] sessionIds)
    {
        await using NpgsqlTransaction tx = await _connection.BeginTransactionAsync();

        try
        {
            foreach (var sessionId in sessionIds)
                await _connection.ExecuteAsync("UPDATE sessions SET end_time = NOW() WHERE id = @SessionId", new { SessionId = sessionId }, transaction: tx);
            
            await tx.CommitAsync();
        }
        catch (Exception ex)
        {
            await tx.RollbackAsync();
            _logger.LogError(ex, "Error while updating played bulk");
            throw;
        }
    }
}