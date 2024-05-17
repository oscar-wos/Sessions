namespace Core
{
    public class Player()
    {
        public int Id { get; set; }
        public DateTime FirstSeen { get; set; }
        public DateTime LastSeen { get; set; }
    }

    public class Map()
    {
        public int Id { get; set; }
    }

    public class Session()
    {
        public int Id { get; set; }
    }

    public partial class Core
    {
        /*
        public async Task<Player> GetPlayerById(int id)
        {
            //1
            return;
        }
        */
    }
}