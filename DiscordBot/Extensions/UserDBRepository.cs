using System;
using System.Collections.Generic;
using System.Threading.Tasks;
using Insight.Database;

namespace DiscordBot.Extensions
{
    public class ServerUser
    {
        /// <summary> This is internal Database ID, remember to use UserID</summary>
        // ReSharper disable once InconsistentNaming
        public int ID { get; private set; }
        public string Username { get; set; }
        public string Discriminator { get; set; }
        // ReSharper disable once InconsistentNaming
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
        // public int Rank { get; set; } // Given scale of server, this could probably be removed and done via query.
        // game | live | liveMessage | udc
    }
    
    public interface IServerUserRepo
    {
        [Sql("INSERT INTO users (Username, Discriminator, UserID, Avatar, AvatarUrl) VALUES (@Username, @Discriminator, @UserID, @Avatar, @AvatarUrl)")]
        Task InsertUser(ServerUser user);
        [Sql("DELETE FROM users WHERE UserID = @userId")]
        Task RemoveUser(string userId);
        
        [Sql("SELECT * FROM users WHERE UserID = @userId")]
        Task<ServerUser> GetUser(string userId);
        
        // Rank Stuff
        [Sql("SELECT Username, UserID, Karma, Level, Exp FROM users ORDER BY Level DESC LIMIT @n")] 
        Task<IList<ServerUser>> GetTopLevel(int n);
        [Sql("SELECT Username, UserID, Karma, KarmaGiven FROM users ORDER BY Karma DESC LIMIT @n")] 
        Task<IList<ServerUser>> GetTopKarma(int n);
        [Sql("SELECT COUNT(UserID)+1 FROM users WHERE Level > @level")] 
        Task<long> GetLevelRank(string userId, int level);
        [Sql("SELECT COUNT(UserID)+1 FROM users WHERE Karma > @karma")] 
        Task<long> GetKarmaRank(string userId, int karma);
        
        // Update Values
        [Sql("UPDATE users SET Username = @username WHERE UserID = @userId")] 
        Task UpdateUserName(string userId, string username);
        [Sql("UPDATE users SET Discriminator = @discriminator WHERE UserID = @userId")] 
        Task UpdateDiscriminator(string userId, string discriminator);
        [Sql("UPDATE users SET Avatar = @avatar, AvatarUrl = @avatarUrl WHERE UserID = @userId")] 
        Task UpdateAvatar(string userId, string avatar, string avatarUrl);
        
        [Sql("UPDATE users SET Karma = @karma WHERE UserID = @userId")] 
        Task UpdateKarma(string userId, int karma);
        [Sql("UPDATE users SET KarmaGiven = @karmaGiven WHERE UserID = @userId")] 
        Task UpdateKarmaGiven(string userId, int karmaGiven);
        [Sql("UPDATE users SET Exp = @xp WHERE UserID = @userId")] 
        Task UpdateXp(string userId, long xp);
        [Sql("UPDATE users SET Level = @level WHERE UserID = @userId")] 
        Task UpdateLevel(string userId, int level);
        
        // Get Single Values
        [Sql("SELECT Username FROM users WHERE UserID = @userId")] 
        Task<string> GetUsername(string userId);
        [Sql("SELECT Discriminator FROM users WHERE UserID = @userId")] 
        Task<string> GetDiscriminator(string userId);
        [Sql("SELECT Avatar FROM users WHERE UserID = @userId")] 
        Task<string> GetAvatar(string userId);
        [Sql("SELECT AvatarUrl FROM users WHERE UserID = @userId")] 
        Task<string> GetAvatarUrl(string userId);
        [Sql("SELECT JoinDate FROM users WHERE UserID = @userId")] 
        Task<DateTime> GetJoinDate(string userId);
        [Sql("SELECT Karma FROM users WHERE UserID = @userId")] 
        Task<int> GetKarma(string userId);
        [Sql("SELECT KarmaGiven FROM users WHERE UserID = @userId")] 
        Task<int> GetKarmaGiven(string userId);
        [Sql("SELECT Exp FROM users WHERE UserID = @userId")] 
        Task<long> GetXp(string userId);
        [Sql("SELECT Level FROM users WHERE UserID = @userId")] 
        Task<int> GetLevel(string userId);

        /// <summary>Returns a count of users in the Table, otherwise it fails. </summary>
        [Sql("SELECT COUNT(*) FROM users")]
        long TestConnection();
    }
}