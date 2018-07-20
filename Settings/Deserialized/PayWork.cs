namespace DiscordBot.Settings.Deserialized {
    public class PayWork {
        public int DaysToBeLockedOut { get; set; }
        public int DaysToRemoveMessage { get; set; }
        
        public LookingForWork LookingForWork { get; set; }
        public Hiring Hiring { get; set; }
        public Collaboration Collaboration { get; set; }
    }

    public class LookingForWork {
        public ulong ChannelId { get; set; }
        public ulong MutedId { get; set; }
    }

    public class Hiring {
        public ulong ChannelId { get; set; }
        public ulong MutedId { get; set; }
    }

    public class Collaboration {
        public ulong ChannelId { get; set; }
        public ulong MutedId { get; set; }
    }
}