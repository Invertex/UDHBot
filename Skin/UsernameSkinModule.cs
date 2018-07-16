using DiscordBot.Domain;
using ImageMagick;

namespace DiscordBot.Skin
{
    public class UsernameSkinModule : BaseTextSkinModule
    {
        public override Drawables GetDrawables(ProfileData data)
        {
            Text = $"{data.Nickname ?? data.Username}";
            return base.GetDrawables(data);
        }

        public UsernameSkinModule()
        {
            FontPointSize = 34;
            Font = "Consolas";
            StrokeColor = MagickColors.BlueViolet.ToString();
            FillColor = MagickColors.DeepSkyBlue.ToString();
        }
    }
}