namespace Sessions;

public interface IDatabaseQueries
{
    string CreateServers { get; }
    string CreateMaps { get; }
    string CreatePlayers { get; }
    string CreateSessions { get; }
    string CreateAliases { get; }
    string CreateMessages { get; }

    string SelectServer { get; }
    string InsertServer { get; }

    string SelectMap { get; }
    string InsertMap { get; }

    string SelectPlayer { get; }
    string InsertPlayer { get; }

    string InsertSession { get; }
    string UpdateSession { get; }
    string UpdateSeen { get; }

    string SelectAlias { get; }
    string InsertAlias { get; }
    string InsertMessage { get; }
}

public abstract class Queries
{
    public abstract string CreateServers { get; }
    public abstract string CreateMaps { get; }
    public abstract string CreatePlayers { get; }
    public abstract string CreateSessions { get; }
    public abstract string CreateAliases { get; }
    public abstract string CreateMessages { get; }

    public IEnumerable<string> GetCreateQueries()
    {
        yield return CreateServers;
        yield return CreateMaps;
        yield return CreatePlayers;
        yield return CreateSessions;
        yield return CreateAliases;
        yield return CreateMessages;
    }
}