using System;
using System.Threading.Tasks;
using Insight.Database;

namespace DiscordBot.Extensions
{
    public class ServerUser
    {
        public int ID { get; private set; }
        public string Username { get; set; }
        public string Discriminator { get; set; }
        public string UserID { get; set; }
        public string Avatar { get; set; }
        public string AvatarUrl { get; set; }
        // bot | muted | mutedTime
        public DateTime JoinDate { get; set; }
        public int Karma { get; set; }
        public int KarmaGiven { get; set; }
        // profanity | karmaLimit | active | lastSeen | lastMessage
        // status | roles | packagesAvailable | packagesLeft
        // keycode
        public long Exp { get; set; }
        public int Level { get; set; }
        public int Rank { get; set; } // Given scale of server, this could probably be removed and done via query.
        // game | live | liveMessage | udc
    }
    
    public interface IServerUserRepository
    {
        void InsertUser(ServerUser user);
        Task<ServerUser> GetUser(string userId);

        void UpdateUser(ServerUser user);
    }
}