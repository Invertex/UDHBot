using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Net;
using System.Net.Http;
using System.Security.Cryptography.X509Certificates;
using System.Text.RegularExpressions;
using System.Threading.Tasks;
using Discord;
using Discord.WebSocket;
using ImageSharp;
using ImageSharp.Drawing.Brushes;
using ImageSharp.PixelFormats;
using SixLabors.Fonts;
using SixLabors.Primitives;
using Image = ImageSharp.Image;

namespace DiscordBot
{
    public class UserService
    {
        private readonly DatabaseService _database;

        private Dictionary<ulong, DateTime> _xpCooldown;
        private Random rand;

        private FontCollection _fontCollection;
        private Font _defaultFont;
        private Font _nameFont;
        private Font _levelFont;

        //TODO : NOT HARDCODE
        private const int _xpMinPerMessage = 5;

        private const int _xpMaxPerMessage = 30;
        private const int _minCooldown = 3;
        private const int _maxCooldown = 5;

        public UserService(DatabaseService database)
        {
            rand = new Random();
            _database = database;
            _xpCooldown = new Dictionary<ulong, DateTime>();

            _fontCollection = new FontCollection();
            _defaultFont = _fontCollection
                .Install(SettingsHandler.LoadValueString("serverRootPath", JsonFile.Settings) +
                         @"\fonts\OpenSans-Regular.ttf")
                .CreateFont(16);
            _nameFont = _fontCollection
                .Install(SettingsHandler.LoadValueString("serverRootPath", JsonFile.Settings) + @"\fonts\Consolas.ttf")
                .CreateFont(24);
            _levelFont = _fontCollection
                .Install(SettingsHandler.LoadValueString("serverRootPath", JsonFile.Settings) + @"\fonts\Consolas.ttf")
                .CreateFont(59);
        }

        public async Task UpdateXp(SocketMessage messageParam)
        {
            ulong id = messageParam.Author.Id;
            int waitTime = rand.Next(_minCooldown, _maxCooldown);
            float baseXp = rand.Next(_xpMinPerMessage, _xpMaxPerMessage);
            float bonusXp = 0;

            if (_xpCooldown.ContainsKey(id))
            {
                if (DateTime.Now > _xpCooldown[id])
                    _xpCooldown.Remove(id);
                else
                    return;
            }

            uint karma = _database.GetUserKarma(id);
            if (messageParam.Author.Game != null)
                if (Regex.Match(messageParam.Author.Game.Value.ToString(), "(Unity.+)").Length > 0)
                    bonusXp += baseXp / 4;

            bonusXp += baseXp * (1f + karma / 100f);
            //Console.WriteLine($"basexp {baseXp} karma {karma}  bonus {bonusXp}");

            _xpCooldown.Add(id, DateTime.Now.Add(new TimeSpan(0, 0, 0, waitTime)));
            //Console.WriteLine($"{_xpCooldown[id].Minute}  {_xpCooldown[id].Second}");

            _database.AddUserXp(id, (uint) Math.Round(baseXp + bonusXp));

            await LevelUp(id);

            //TODO: add xp gain on website
        }

        public async Task LevelUp(ulong userId)
        {
            int level = (int) _database.GetUserLevel(userId);
            uint xp = _database.GetUserXp(userId);

            double xpLow = GetXpLow(level);
            double xpHigh = GetXpHigh(level);

            if (xp > xpHigh)
            {
                _database.AddUserLevel(userId, 1);
            }

            //TODO: Add level up message
        }

        private double GetXpLow(int level)
        {
            return 70d - 139.5d * (level + 1d) + 69.5 * Math.Pow(level + 1d, 2d);
            ;
        }

        private double GetXpHigh(int level)
        {
            return 70d - 139.5d * (level + 2d) + 69.5 * Math.Pow(level + 2d, 2d);
        }

        public async Task<string> GenerateProfileCard(IUser user)
        {
            var backgroundPath = SettingsHandler.LoadValueString("serverRootPath", JsonFile.Settings) +
                                 @"\images\background.png";
            var foregroundPath = SettingsHandler.LoadValueString("serverRootPath", JsonFile.Settings) +
                                 @"\images\foreground.png";
            Image<Rgba32> profileCard = Image.Load(backgroundPath);
            Image<Rgba32> profileFg = Image.Load(foregroundPath);
            Image<Rgba32> avatar;
            Image<Rgba32> triangle = Image.Load(
                SettingsHandler.LoadValueString("serverRootPath", JsonFile.Settings) +
                @"\images\triangle.png");
            Stream stream;
            string avatarUrl = user.GetAvatarUrl();
            ulong userId = user.Id;

            if (string.IsNullOrEmpty(avatarUrl))
            {
                avatar = Image.Load(SettingsHandler.LoadValueString("serverRootPath", JsonFile.Settings) +
                                    @"\images\default.png");
            }
            else
            {
                using (var http = new HttpClient())
                {
                    stream = await http.GetStreamAsync(new Uri(avatarUrl));
                }
                avatar = Image.Load(stream);
            }
            uint xp = _database.GetUserXp(userId);
            uint rank = _database.GetUserRank(userId);
            uint karma = _database.GetUserKarma(userId);
            uint level = _database.GetUserLevel(userId);
            double xpLow = GetXpLow((int) level);
            double xpHigh = GetXpHigh((int) level);

            const float startX = 104;
            const float startY = 39;
            const float height = 16;
            float endX = (float) ((xp - xpLow) / (xpHigh - xpLow) * 250f);

            profileCard.DrawImage(profileFg, 100f, new Size(profileFg.Width, profileFg.Height), Point.Empty);
            
            var u = user as IGuildUser;
            IRole mainRole = null;
            foreach (var id in u.RoleIds)
            {
                var role = u.Guild.GetRole(id);
                if (mainRole == null)
                    mainRole = u.Guild.GetRole(id);
                else if (role.Position > mainRole.Position)
                {
                    mainRole = role;
                }
            }
            var c = mainRole.Color;
            //Console.WriteLine($"{u.Guild.GetRole(u.RoleIds.Last())}  {c.R} {c.G} {c.B} {c.RawValue}");
         
            RecolorBrush<Rgba32> brush = new RecolorBrush<Rgba32>(Rgba32.White,
                new Rgba32(c.R, c.G, c.B), .25f);

            triangle.Fill(brush);

            profileCard.DrawImage(triangle, 100f, new Size(triangle.Width, triangle.Height), new Point(346, 14));
            //profileCard.Fill(new Rgba32(.5f, .5f, .5f, .5f), new RectangleF(20, 20, 376, 88)); //Background

            profileCard.Fill(Rgba32.FromHex("#3f3f3f"),
                new RectangleF(startX, startY, 232, height)); //XP bar background
            profileCard.Fill(Rgba32.FromHex("#00f0ff"),
                new RectangleF(startX + 1, startY + 1, endX, height - 2)); //XP bar
            profileCard.DrawImage(avatar, 100f, new Size(80, 80), new Point(16, 28));
            profileCard.DrawText(user.Username + "#" + user.Discriminator, _nameFont, Rgba32.FromHex("#3C3C3C"),
                new PointF(144, 8));
            profileCard.DrawText(level.ToString(), _levelFont, Rgba32.FromHex("#3C3C3C"), new PointF(98, 35));
            profileCard.DrawText("Server Rank        #" + rank, _defaultFont, Rgba32.FromHex("#3C3C3C"),
                new PointF(167, 60));
            profileCard.DrawText("Karma Points:    " + karma, _defaultFont, Rgba32.FromHex("#3C3C3C"),
                new PointF(167, 77));
            profileCard.DrawText("Total XP:              " + xp, _defaultFont, Rgba32.FromHex("#3C3C3C"),
                new PointF(167, 94));

            profileCard.Save(SettingsHandler.LoadValueString("serverRootPath", JsonFile.Settings) +
                             $@"\images\profiles\{user.Username}-profile.png");
            return SettingsHandler.LoadValueString("serverRootPath", JsonFile.Settings) +
                   $@"\images\profiles\{user.Username}-profile.png";
        }

        public Embed WelcomeMessage(string icon, string name, ushort discriminator)
        {
            icon = string.IsNullOrEmpty(icon) ? "https://cdn.discordapp.com/embed/avatars/0.png" : icon;
            var builder = new EmbedBuilder()
                .WithDescription($"Welcome **{(name) + "#" + discriminator}** to the server!")
                .WithColor(new Color(0x12D687))
                .WithAuthor(author =>
                {
                    author
                        .WithName(name)
                        .WithIconUrl(icon);
                });
            var embed = builder.Build();
            return embed;
        }
    }
}