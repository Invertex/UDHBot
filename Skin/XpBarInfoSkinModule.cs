using DiscordBot.Domain;
using ImageMagick;

namespace DiscordBot.Skin
{
    public class XpBarInfoSkinModule : ISkinModule
    {
        public double StartX { get; set; }
        public double StartY { get; set; }
        public double StrokeWidth { get; set; }

        public string FillColor { get; set; }
        public string StrokeColor { get; set; }
        public string Font { get; set; }
        public double FontPointSize { get; set; }

        public string Type { get; set; }

        public Drawables GetDrawables(ProfileData data)
        {
            PointD xpBarPosition = new PointD(StartX, StartY);

            return new Drawables()
                //XP Bar Info
                .StrokeColor(new MagickColor(StrokeColor))
                .FillColor(new MagickColor(FillColor))
                .StrokeWidth(StrokeWidth)
                .Font(Font)
                .FontPointSize(FontPointSize)
                .TextAlignment(TextAlignment.Center)
                .Text(xpBarPosition.X, xpBarPosition.Y, $"{data.XpShown:#,##0} / {data.MaxXpShown:N0} ({(data.XpPercentage * 100):0}%)");
        }

        public XpBarInfoSkinModule()
        {
            StrokeWidth = 1;
            FillColor = MagickColors.Black.ToString();
            StrokeColor = MagickColors.Transparent.ToString();
            Font = "Consolas";
            FontPointSize = 17;
            
        }
    }
}