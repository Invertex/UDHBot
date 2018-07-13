using DiscordBot.Domain;
using ImageMagick;

namespace DiscordBot.Skin
{
    public class LevelSkinModule : BaseTextSkinModule
    {
        public override Drawables GetDrawables(ProfileData data)
        {
            PointD levelPosition = new PointD( StartX, StartY);

            return new Drawables()
                .FontPointSize(FontPointSize)
                .Font(Font)
                .StrokeColor(new MagickColor(StrokeColor))
                .StrokeWidth(StrokeWidth)
                .StrokeAntialias(StrokeAntiAlias)
                .FillColor(new MagickColor(FillColor))
                .TextAntialias(TextAntiAlias)
                .TextAlignment(TextAlignment.Center)
                .Text(levelPosition.X, levelPosition.Y, data.Level.ToString());
        }

        public LevelSkinModule()
        {
            StartX = 220;
            StartY = 140;
            StrokeColor = MagickColors.IndianRed.ToString();
            FillColor = MagickColors.IndianRed.ToString();
            FontPointSize = 50;
            TextAntiAlias = true;
            StrokeAntiAlias = true;
        }
    }
}