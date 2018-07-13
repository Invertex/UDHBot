using DiscordBot.Domain;
using ImageMagick;

namespace DiscordBot.Skin
{
    public class UsernameSkinModule : BaseTextSkinModule
    {
        public override Drawables GetDrawables(ProfileData data)
        {
            PointD usernamePosition = new PointD(StartX, StartY);

            return new Drawables()
                .FontPointSize(FontPointSize)
                .Font(Font)
                .StrokeColor(new MagickColor(StrokeColor))
                .StrokeWidth(StrokeWidth)
                .StrokeAntialias(StrokeAntiAlias)
                .FillColor(new MagickColor(FillColor))
                .TextAlignment(TextAlignment.Left)
                .TextAntialias(TextAntiAlias)
                .Text(usernamePosition.X, usernamePosition.Y, $"{data.Nickname ?? data.Username}");
        }

        public UsernameSkinModule()
        {
            FontPointSize = 34;
            Font = "Consolas";
            StrokeColor = MagickColors.BlueViolet.ToString();
            FillColor = MagickColors.DeepSkyBlue.ToString();
            StrokeAntiAlias = true;
            StrokeWidth = 1;
        }
    }
}