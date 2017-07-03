using System;
using System.Collections.Generic;
using System.IO;
using System.Net;
using System.Net.Http;
using System.Security.Cryptography.X509Certificates;
using System.Text.RegularExpressions;
using System.Threading.Tasks;
using Discord;
using Discord.WebSocket;
using ImageSharp;
using ImageSharp.PixelFormats;
using SixLabors.Fonts;
using SixLabors.Primitives;
using Image = ImageSharp.Image;

namespace DiscordBot
{
    public class ProfileService
    {
        private readonly DatabaseService _database;

        private Dictionary<ulong, DateTime> _xpCooldown;
        private Random rand;

        private FontCollection _fontCollection;
        private Font _defaultFont;
        private Font _nameFont;

        //TODO : NOT HARDCODE
        private const int _xpMinPerMessage = 5;

        private const int _xpMaxPerMessage = 30;
        private const int _minCooldown = 3;
        private const int _maxCooldown = 5;

        public ProfileService(DatabaseService database)
        {
            rand = new Random();
            _database = database;
            _xpCooldown = new Dictionary<ulong, DateTime>();

            _fontCollection = new FontCollection();
            _defaultFont = _fontCollection.Install(Settings.GetServerRootPath() + @"\fonts\georgia.ttf").CreateFont(12);
            _nameFont = _fontCollection.Install(Settings.GetServerRootPath() + @"\fonts\georgia.ttf").CreateFont(18);
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
            int level = (int)_database.GetUserLevel(userId);
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
            var backgroundPath = Settings.GetServerRootPath() + @"\images\background.png";
            Image<Rgba32> profileCard = Image.Load(backgroundPath);
            Image<Rgba32> avatar;
            Stream stream;
            string avatarUrl = user.GetAvatarUrl();
            ulong userId = user.Id;

            if (string.IsNullOrEmpty(avatarUrl))
            {
                avatar = Image.Load(Settings.GetServerRootPath() + @"\images\default.png");
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

            const float startX = 80;
            const float startY = 60;
            const float height = 10;
            float endX = (float) ((xp - xpLow) / (xpHigh - xpLow) * 250f);
            
            profileCard.Fill(new Rgba32(.5f, .5f, .5f, .5f), new RectangleF(20, 20, 376, 88)); //Background

            profileCard.Fill(new Rgba32(1f, 1f, 1f, .75f),
                new RectangleF(startX, startY, 250, height)); //XP bar background
            profileCard.Fill(new Rgba32(.5f, .5f, .5f, 1f),
                new RectangleF(startX + 1, startY + 1, endX, height - 2)); //XP bar
            profileCard.DrawImage(avatar, 100f, new Size(32, 32), new Point(40, 50));
            profileCard.DrawText(user.Username + "#" + user.Discriminator, _nameFont, Rgba32.AntiqueWhite,
                new PointF(192, 20));
            profileCard.DrawText("Level " + level, _defaultFont, Rgba32.AntiqueWhite, new PointF(83, 70));
            profileCard.DrawText("Total xp " + xp, _defaultFont, Rgba32.AntiqueWhite, new PointF(256, 70));
            profileCard.DrawText("#" + rank, _defaultFont, Rgba32.AntiqueWhite, new PointF(256, 80));
            profileCard.DrawText("kp:" + karma, _defaultFont, Rgba32.AntiqueWhite, new PointF(256, 90));

            profileCard.Save(Settings.GetServerRootPath() + $@"\images\profiles\{user.Username}-profile.png");
            return Settings.GetServerRootPath() + $@"\images\profiles\{user.Username}-profile.png";
        }
    }
}