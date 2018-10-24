using System.Collections.Generic;

namespace DiscordBot.Settings.Deserialized {
    public class Rules {
        public List<ChannelData> Channel { get; set; }
    }

    public class ChannelData {
        public ulong Id { get; set; }
        public string Header { get; set; }
        public string Content { get; set; }
    }
}