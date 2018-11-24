using System.Collections.Generic;

namespace DiscordBot.Skin
{
    public class SkinData
    {
        public string Name { get; set; }
        public string Codename { get; set; }
        public string Description { get; set; }
        public int AvatarSize { get; set; }
        public string Background { get; set; }
        public List<SkinLayer> Layers { get; set; }

        public SkinData()
        {
            Layers = new List<SkinLayer>();
        }
    }
}