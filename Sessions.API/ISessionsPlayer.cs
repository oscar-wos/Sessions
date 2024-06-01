namespace Sessions.API;

public interface ISessionsPlayer
{
    Player? Player { get; }
    Session? Session { get; }
}