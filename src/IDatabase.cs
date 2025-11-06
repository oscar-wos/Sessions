using Sessions.API;

namespace Sessions;

public interface IDatabase
{
    Task StartAsync();
    Task<Server> GetServerAsync(string serverIp, ushort serverPort);
    Task<Map> GetMapAsync(string mapName);
    Task<Player> GetPlayerAsync(ulong steamId);
    Task<Session> GetSessionAsync(int playerId, int serverId, int mapId, string ip);
    Task<Alias?> GetAliasAsync(int playerId);
    Task InsertAliasAsync(long sessionId, int playerId, string name);
    Task InsertMessageAsync(long sessionId, int playerId, MessageType messageType, string message);
    Task UpdateSessionsAsync(List<int> playerIds, List<long> sessionIds);
    Task UpdateSeenAsync(int playerId);
}
