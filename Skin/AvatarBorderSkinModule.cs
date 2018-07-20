using DiscordBot.Domain;
using ImageMagick;

namespace DiscordBot.Skin
{
    public class AvatarBorderSkinModule : ISkinModule
    {
        public double StartX { get; set; }
        public double StartY { get; set; }
        public double Size { get; set; }

        public string Type { get; set; }

        public Drawables GetDrawables(ProfileData data)
        {
            double avatarContourStartX = StartX;
            double avatarContourStartY = StartY;
            RectangleD avatarContour = new RectangleD(avatarContourStartX - 2, avatarContourStartY - 2,
                avatarContourStartX + Size + 1, avatarContourStartY + Size + 1);
            MagickColor avatarContourColor = MagickColors.IndianRed;

            return new Drawables()
                .StrokeColor(new MagickColor(data.MainRoleColor.R, data.MainRoleColor.G, data.MainRoleColor.B))
                .FillColor(new MagickColor(data.MainRoleColor.R, data.MainRoleColor.G, data.MainRoleColor.B))
                .Rectangle(avatarContour.UpperLeftX, avatarContour.UpperLeftY, avatarContour.LowerRightX, avatarContour.LowerRightY);
        }

        public AvatarBorderSkinModule()
        {
            Size = 128;
        }
    }
}