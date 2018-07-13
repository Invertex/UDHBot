using DiscordBot.Domain;
using ImageMagick;

namespace DiscordBot.Skin
{
    public class KarmaRankSkinModule : BaseTextSkinModule
    {
        public override Drawables GetDrawables(ProfileData data)
        {
            PointD xpPosition = new PointD(StartX, StartY);

            return new Drawables()
                .FontPointSize(FontPointSize)
                .Font(Font)
                .StrokeColor(new MagickColor(StrokeColor))
                .StrokeWidth(StrokeWidth)
                .StrokeAntialias(StrokeAntiAlias)
                .FillColor(new MagickColor(FillColor))
                .TextAntialias(TextAntiAlias)
                .TextAlignment(TextAlignment.Right)
                .Text(xpPosition.X, xpPosition.Y, $"#{data.KarmaRank}");
        }

        public KarmaRankSkinModule()
        {
            StartX = 535;
            StartY = 153;
            StrokeColor = MagickColors.Transparent.ToString();
            FillColor = MagickColors.Black.ToString();
            FontPointSize = 17;
            StrokeWidth = 1;
            Font = "Consolas";
            TextAntiAlias = true;
            StrokeAntiAlias = true;
        }
    }
}