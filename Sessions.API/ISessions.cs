namespace Sessions.API;

public interface ISessionsPlayer
{
    Player? Player { get; }
    Session? Session { get; }
}

public class Server
{
    public short Id { get; set; }

    public Map? Map { get; set; }
}

public class Map
{
    public short Id { get; set; }
}

public class Player
{
    public int Id { get; set; }
    public DateTime FirstSeen { get; set; }
    public DateTime LastSeen { get; set; }

    public Session? Session { get; set; }
}

public class Session
{
    public long Id { get; set; }
}

public class Alias
{
    public int Id { get; set; }
    public required string Name { get; set; }
}