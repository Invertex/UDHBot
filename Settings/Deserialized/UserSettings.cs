using System.Collections.Generic;

namespace DiscordBot.Settings.Deserialized {
    public class UserSettings {
        public List<string> Thanks { get; set; }
        public int ThanksCooldown { get; set; }
        public int ThanksReminderCooldown { get; set; }
        public int ThanksMinJoinTime { get; set; }
        
        public int XpMinPerMessage { get; set; }
        public int XpMaxPerMessage { get; set; }
        public int XpMinCooldown { get; set; }
        public int XpMaxCooldown { get; set; }
        
        public int CodeReminderCooldown { get; set; }
        
        public List<string> IsSomeoneThere { get; set; }
    }
}