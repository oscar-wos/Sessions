namespace Sessions.API;

public interface IEventSender
{
    public event EventHandler<Player> PlayerConnected;
}