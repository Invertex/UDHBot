using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Net;
using System.Net.Http;
using System.Text;
using System.Text.RegularExpressions;
using System.Threading.Tasks;
using Discord;
using Discord.Rest;
using Discord.WebSocket;
using DiscordBot.Extensions;
using ImageSharp;
using ImageSharp.Drawing;
using ImageSharp.Drawing.Brushes;
using ImageSharp.Formats;
using SixLabors.Fonts;
using SixLabors.Primitives;
using Image = ImageSharp.Image;

namespace DiscordBot
{
    public class UserService
    {
        private readonly DatabaseService _databaseService;
        private readonly LoggingService _loggingService;
        private readonly UpdateService _updateService;

        public Dictionary<ulong, DateTime> _mutedUsers;

        private readonly Dictionary<ulong, DateTime> _xpCooldown;
        private readonly HashSet<ulong> _canEditThanks; //Doesn't need to be saved
        private readonly Dictionary<ulong, DateTime> _thanksCooldown;
        private Dictionary<ulong, DateTime> _thanksReminderCooldown;

        public Dictionary<ulong, DateTime> ThanksReminderCooldown
        {
            get { return _thanksReminderCooldown; }
        }

        private Dictionary<ulong, DateTime> _codeReminderCooldown;

        public Dictionary<ulong, DateTime> CodeReminderCooldown
        {
            get { return _codeReminderCooldown; }
        }

        private readonly Random rand;

        private readonly FontCollection _fontCollection;
        private readonly Font _defaultFont;
        private readonly Font _nameFont;
        private readonly Font _levelFont;
        private readonly Font _levelFontSmall;
        private readonly Font _subtitlesBlackFont;
        private readonly Font _subtitlesWhiteFont;
        private readonly string _thanksRegex;

        private readonly int _thanksCooldownTime;
        private readonly int _thanksReminderCooldownTime;
        private readonly int _thanksMinJoinTime;

        private readonly int _xpMinPerMessage;
        private readonly int _xpMaxPerMessage;
        private readonly int _xpMinCooldown;
        private readonly int _xpMaxCooldown;

        private readonly int _codeReminderCooldownTime;
        public readonly string _codeReminderFormattingExample;

        private readonly List<ulong> _noXpChannels;

        //TODO: Add custom commands for user after (30karma ?/limited to 3 ?)

        public UserService(DatabaseService databaseService, LoggingService loggingService, UpdateService updateService)
        {
            rand = new Random();
            _databaseService = databaseService;
            _loggingService = loggingService;
            _updateService = updateService;
            _mutedUsers = new Dictionary<ulong, DateTime>();
            _xpCooldown = new Dictionary<ulong, DateTime>();
            _canEditThanks = new HashSet<ulong>(32);
            _thanksCooldown = new Dictionary<ulong, DateTime>();
            _thanksReminderCooldown = new Dictionary<ulong, DateTime>();
            _codeReminderCooldown = new Dictionary<ulong, DateTime>();

            _noXpChannels = new List<ulong>
            {
                Settings.GetBotCommandsChannel(),
                Settings.GetCasinoChannel(),
                Settings.GetMusicCommandsChannel()
            };

            /*
            Init font for the profile card
            */
            _fontCollection = new FontCollection();
            _defaultFont = _fontCollection
                .Install(SettingsHandler.LoadValueString("serverRootPath", JsonFile.Settings) +
                         "/fonts/OpenSans-Regular.ttf")
                .CreateFont(16);
            _nameFont = _fontCollection
                .Install(SettingsHandler.LoadValueString("serverRootPath", JsonFile.Settings) + "/fonts/Consolas.ttf")
                .CreateFont(22);
            _levelFont = _fontCollection
                .Install(SettingsHandler.LoadValueString("serverRootPath", JsonFile.Settings) + "/fonts/Consolas.ttf")
                .CreateFont(59);
            _levelFontSmall = _fontCollection
                .Install(SettingsHandler.LoadValueString("serverRootPath", JsonFile.Settings) + "/fonts/Consolas.ttf")
                .CreateFont(45);

            _subtitlesBlackFont = _fontCollection
                .Install(SettingsHandler.LoadValueString("serverRootPath", JsonFile.Settings) + "/fonts/OpenSansEmoji.ttf")
                .CreateFont(80);
            _subtitlesWhiteFont = _fontCollection
                .Install(SettingsHandler.LoadValueString("serverRootPath", JsonFile.Settings) + "/fonts/OpenSansEmoji.ttf")
                .CreateFont(75);

            /*
            Init XP
            */
            _xpMinPerMessage = SettingsHandler.LoadValueInt("xpMinPerMessage", JsonFile.UserSettings);
            _xpMaxPerMessage = SettingsHandler.LoadValueInt("xpMaxPerMessage", JsonFile.UserSettings);
            _xpMinCooldown = SettingsHandler.LoadValueInt("xpMinCooldown", JsonFile.UserSettings);
            _xpMaxCooldown = SettingsHandler.LoadValueInt("xpMaxCooldown", JsonFile.UserSettings);

            /*
            Init thanks
            */
            StringBuilder sbThanks = new StringBuilder();
            string[] thx = SettingsHandler.LoadValueStringArray("thanks", JsonFile.UserSettings);
            sbThanks.Append("(?i)\\b(");
            for (int i = 0; i < thx.Length; i++)
            {
                sbThanks.Append(thx[i]).Append("|");
            }

            sbThanks.Length--; //Efficiently remove the final pipe that gets added in final loop, simplifying loop
            sbThanks.Append(")\\b");
            _thanksRegex = sbThanks.ToString();
            _thanksCooldownTime = SettingsHandler.LoadValueInt("thanksCooldown", JsonFile.UserSettings);
            _thanksReminderCooldownTime = SettingsHandler.LoadValueInt("thanksReminderCooldown", JsonFile.UserSettings);
            _thanksMinJoinTime = SettingsHandler.LoadValueInt("thanksMinJoinTime", JsonFile.UserSettings);

            /*
             Init Code analysis
            */
            _codeReminderCooldownTime = SettingsHandler.LoadValueInt("codeReminderCooldown", JsonFile.UserSettings);
            _codeReminderFormattingExample = (
                @"\`\`\`cs" + Environment.NewLine +
                "Write your code on new line here." + Environment.NewLine +
                @"\`\`\`" + Environment.NewLine + Environment.NewLine +
                "Simple as that! If you'd like me to stop reminding you about this, simply type \"!disablecodetips\"");

            LoadData();
            UpdateLoop();
        }

        private async void UpdateLoop()
        {
            while (true)
            {
                await Task.Delay(10000);
                SaveData();
            }
        }

        private void LoadData()
        {
            var data = _updateService.GetUserData();
            _mutedUsers = data.MutedUsers ?? new Dictionary<ulong, DateTime>();
            _thanksReminderCooldown = data.ThanksReminderCooldown ?? new Dictionary<ulong, DateTime>();
            _codeReminderCooldown = data.CodeReminderCooldown ?? new Dictionary<ulong, DateTime>();
        }

        private void SaveData()
        {
            UserData data = new UserData
            {
                MutedUsers = _mutedUsers,
                ThanksReminderCooldown = _thanksReminderCooldown,
                CodeReminderCooldown = _codeReminderCooldown
            };
            _updateService.SetUserData(data);
        }

        public async Task UpdateXp(SocketMessage messageParam)
        {
            if (messageParam.Author.IsBot)
                return;

            if (_noXpChannels.Contains(messageParam.Channel.Id))
                return;

            ulong userId = messageParam.Author.Id;
            int waitTime = rand.Next(_xpMinCooldown, _xpMaxCooldown);
            float baseXp = rand.Next(_xpMinPerMessage, _xpMaxPerMessage);
            float bonusXp = 0;

            if (_xpCooldown.HasUser(userId))
                return;

            int karma = _databaseService.GetUserKarma(userId);
            if (messageParam.Author.Game != null)
            {
                if (Regex.Match(messageParam.Author.Game.Value.ToString(), "(Unity.+)").Length > 0)
                    bonusXp += baseXp / 4;
            }

            bonusXp += baseXp * (1f + (karma / 100f));

            //Reduce XP for members with no role
            if (((IGuildUser) messageParam.Author).RoleIds.Count < 2)
                baseXp *= .1f;
            //Console.WriteLine($"basexp {baseXp} karma {karma}  bonus {bonusXp}");
            _xpCooldown.AddCooldown(userId, waitTime);
            //Console.WriteLine($"{_xpCooldown[id].Minute}  {_xpCooldown[id].Second}");

            if (!await _databaseService.UserExists(userId))
                _databaseService.AddNewUser((SocketGuildUser)messageParam.Author);

            _databaseService.AddUserXp(userId, (int) Math.Round(baseXp + bonusXp));
            _databaseService.AddUserUdc(userId, (int) Math.Round((baseXp + bonusXp) * .15f));

            await LevelUp(messageParam, userId);

            //TODO: add xp gain on website
        }

        public async Task LevelUp(SocketMessage messageParam, ulong userId)
        {
            int level = (int) _databaseService.GetUserLevel(userId);
            uint xp = _databaseService.GetUserXp(userId);

            double xpLow = GetXpLow(level);
            double xpHigh = GetXpHigh(level);

            if (xp < xpHigh)
                return;

            _databaseService.AddUserLevel(userId, 1);
            _databaseService.AddUserUdc(userId, 1200);

            await messageParam.Channel.SendMessageAsync($"**{messageParam.Author}** has leveled up !").DeleteAfterTime(seconds: 60);
            //TODO: investigate why this is not running async
            //I believe it's because you didn't include async and await in the ContinueWith structure.
            // instead should be `ContinueWith(async _ => await message.DeleteAsync())`
            //await Task.Delay(TimeSpan.FromSeconds(60d)).ContinueWith(async _ => await message.DeleteAsync()).Unwrap();
            //TODO: Add level up card
        }

        private double GetXpLow(int level)
        {
            return 70d - (139.5d * (level + 1d)) + (69.5 * Math.Pow(level + 1d, 2d));
        }

        private double GetXpHigh(int level)
        {
            return 70d - (139.5d * (level + 2d)) + (69.5 * Math.Pow(level + 2d, 2d));
        }

        public async Task<string> GenerateProfileCard(IUser user)
        {
            var backgroundPath = SettingsHandler.LoadValueString("serverRootPath", JsonFile.Settings) +
                                 "/images/background.png";
            var foregroundPath = SettingsHandler.LoadValueString("serverRootPath", JsonFile.Settings) +
                                 "/images/foreground.png";
            Image<Rgba32> profileCard = ImageSharp.Image.Load(backgroundPath);
            Image<Rgba32> profileFg = ImageSharp.Image.Load(foregroundPath);
            Image<Rgba32> avatar;
            Image<Rgba32> triangle = ImageSharp.Image.Load(
                SettingsHandler.LoadValueString("serverRootPath", JsonFile.Settings) +
                "/images/triangle.png");
            Stream stream;
            string avatarUrl = user.GetAvatarUrl();
            ulong userId = user.Id;

            if (string.IsNullOrEmpty(avatarUrl))
            {
                avatar = ImageSharp.Image.Load(SettingsHandler.LoadValueString("serverRootPath", JsonFile.Settings) +
                                               "/images/default.png");
            }
            else
            {
                try
                {
                    using (var http = new HttpClient())
                    {
                        stream = await http.GetStreamAsync(new Uri(avatarUrl));
                    }

                    avatar = ImageSharp.Image.Load(stream);
                }
                catch (Exception e)
                {
                    Console.WriteLine(e);
                    avatar = ImageSharp.Image.Load(SettingsHandler.LoadValueString("serverRootPath", JsonFile.Settings) +
                                                   "/images/default.png");
                }
            }

            uint xp = _databaseService.GetUserXp(userId);
            uint rank = _databaseService.GetUserRank(userId);
            int karma = _databaseService.GetUserKarma(userId);
            uint level = _databaseService.GetUserLevel(userId);
            double xpLow = GetXpLow((int) level);
            double xpHigh = GetXpHigh((int) level);

            const float startX = 104;
            const float startY = 39;
            const float height = 16;
            float endX = (float) ((xp - xpLow) / (xpHigh - xpLow) * 232f);

            profileCard.DrawImage(profileFg, 100f, new Size(profileFg.Width, profileFg.Height), Point.Empty);

            var u = user as IGuildUser;
            IRole mainRole = null;
            foreach (ulong id in u.RoleIds)
            {
                IRole role = u.Guild.GetRole(id);
                if (mainRole == null)
                {
                    mainRole = u.Guild.GetRole(id);
                }
                else if (role.Position > mainRole.Position)
                {
                    mainRole = role;
                }
            }

            Color c = mainRole.Color;

            var brush = new RecolorBrush<Rgba32>(Rgba32.White,
                new Rgba32(c.R, c.G, c.B), .25f);

            triangle.Fill(brush);

            profileCard.DrawImage(triangle, 100f, new Size(triangle.Width, triangle.Height), new Point(346, 17));

            profileCard.Fill(Rgba32.FromHex("#3f3f3f"),
                new RectangleF(startX, startY, 232, height)); //XP bar background
            profileCard.Fill(Rgba32.FromHex("#00f0ff"),
                new RectangleF(startX + 1, startY + 1, endX, height - 2)); //XP bar
            profileCard.DrawImage(avatar, 100f, new Size(80, 80), new Point(16, 28));
            profileCard.DrawText(user.Username, _nameFont, Rgba32.FromHex("#3C3C3C"),
                new PointF(144, 8));
            profileCard.DrawText(level.ToString(), level < 100 ? _levelFont : _levelFontSmall, Rgba32.FromHex("#3C3C3C"),
                new PointF(98, 35));
            profileCard.DrawText("Server Rank        #" + rank, _defaultFont, Rgba32.FromHex("#3C3C3C"),
                new PointF(167, 60));
            profileCard.DrawText("Karma Points:    " + karma, _defaultFont, Rgba32.FromHex("#3C3C3C"),
                new PointF(167, 77));
            profileCard.DrawText("Total XP:              " + xp, _defaultFont, Rgba32.FromHex("#3C3C3C"),
                new PointF(167, 94));

            profileCard.Resize(400, 120);

            profileCard.Save(SettingsHandler.LoadValueString("serverRootPath", JsonFile.Settings) +
                             $"/images/profiles/{user.Username}-profile.png");
            return SettingsHandler.LoadValueString("serverRootPath", JsonFile.Settings) +
                   $"/images/profiles/{user.Username}-profile.png";
        }

        public Embed WelcomeMessage(string icon, string name, ushort discriminator)
        {
            icon = string.IsNullOrEmpty(icon) ? "https://cdn.discordapp.com/embed/avatars/0.png" : icon;
            EmbedBuilder builder = new EmbedBuilder()
                .WithDescription($"Welcome to Unity Developer Hub **{name}#{discriminator}** !")
                .WithColor(new Color(0x12D687))
                .WithAuthor(author =>
                {
                    author
                        .WithName(name)
                        .WithIconUrl(icon);
                });
            Embed embed = builder.Build();
            return embed;
        }

        // Message Edited Thanks
        public async Task ThanksEdited(Cacheable<IMessage, ulong> cachedMessage, SocketMessage messageParam, ISocketMessageChannel socketMessageChannel)
        {
            if(_canEditThanks.Contains(messageParam.Id))
            {
                await Thanks(messageParam);
            }
        }


        public async Task Thanks(SocketMessage messageParam)
        {
            if (messageParam.Author.IsBot)
                return;

            Match match = Regex.Match(messageParam.Content, _thanksRegex);
            if (!match.Success)
                return;

            IReadOnlyCollection<SocketUser> mentions = messageParam.MentionedUsers;
            mentions = mentions.Distinct().ToList();
            ulong userId = messageParam.Author.Id;
            const int defaultDelTime = 120;

            if (mentions.Count > 0)
            {
                if (_thanksCooldown.HasUser(userId))
                {
                    await messageParam.Channel.SendMessageAsync(
                        $"{messageParam.Author.Mention} you must wait " +
                        $"{DateTime.Now - _thanksCooldown[userId]:ss} " +
                        "seconds before giving another karma point").DeleteAfterTime(seconds: defaultDelTime);
                    return;
                }

                DateTime.TryParse(_databaseService.GetUserJoinDate(userId), out DateTime joinDate);
                var j = joinDate + TimeSpan.FromSeconds(_thanksMinJoinTime);

                if (j > DateTime.Now)
                {
                    await messageParam.Channel.SendMessageAsync(
                            $"{messageParam.Author.Mention} you must have been a member for at least 10 minutes to give karma points.")
                        .DeleteAfterTime(seconds: 140);
                    return;
                }

                bool mentionedSelf = false;
                bool mentionedBot = false;
                StringBuilder sb = new StringBuilder();

                sb.Append("**").Append(messageParam.Author.Username).Append("** gave karma to **");
                foreach (SocketUser user in mentions)
                {
                    if (user.IsBot)
                    {
                        mentionedBot = true;
                        continue;
                    }

                    if (user.Id == userId)
                    {
                        mentionedSelf = true;
                        continue;
                    }

                    _databaseService.AddUserKarma(user.Id, 1);
                    _databaseService.AddUserUdc(user.Id, 350);
                    sb.Append(user.Username).Append(" , ");
                }

                sb.Length -= 2; //Removes last instance of appended comma without convoluted tracking
                sb.Append("**");

                if (mentionedSelf)
                {
                    await messageParam.Channel.SendMessageAsync(
                        $"{messageParam.Author.Mention} you can't give karma to yourself.").DeleteAfterTime(seconds: defaultDelTime);
                }

                if (mentionedBot)
                {
                    await messageParam.Channel.SendMessageAsync(
                        $"Very cute of you {messageParam.Author.Mention} but I don't need karma :blush:{Environment.NewLine}" +
                        "If you'd like to know what Karma is about, type !karma").DeleteAfterTime(seconds: defaultDelTime);
                }

                _canEditThanks.Remove(messageParam.Id);

                //Don't give karma cooldown if user only mentioned himself or the bot or both
                if (((mentionedSelf || mentionedBot) && mentions.Count == 1) || (mentionedBot && mentionedSelf && mentions.Count == 2))
                    return;

                _thanksCooldown.AddCooldown(userId, _thanksCooldownTime);
                //Add thanks reminder cooldown after thanking to avoid casual thanks triggering remind afterwards
                ThanksReminderCooldown.AddCooldown(userId, _thanksReminderCooldownTime);
                await messageParam.Channel.SendMessageAsync(sb.ToString());
                await _loggingService.LogAction(sb + " in channel " + messageParam.Channel.Name);
            }
            else if (messageParam.Channel.Name != "general-chat" && !ThanksReminderCooldown.IsPermanent(userId) &&
                     !ThanksReminderCooldown.HasUser(userId) && !_thanksCooldown.HasUser(userId))
            {
                ThanksReminderCooldown.AddCooldown(userId, _thanksReminderCooldownTime);
                await messageParam.Channel.SendMessageAsync(
                        $"{messageParam.Author.Mention} , if you are thanking someone, please @mention them when you say \"thanks\" so they may receive karma for their help." +
                        Environment.NewLine +
                        "If you want me to stop reminding you about this, please type \"!disablethanksreminder\".")
                    .DeleteAfterTime(seconds: defaultDelTime);
            }
            if (mentions.Count == 0 && _canEditThanks.Add(messageParam.Id))
            {
                _canEditThanks.RemoveAfterSeconds(messageParam.Id, 240);
            }
        }

        public async Task CodeCheck(SocketMessage messageParam)
        {
            if (messageParam.Author.IsBot)
                return;

            ulong userId = messageParam.Author.Id;

            //Simple check to cover most large code posting cases without being an issue for most non-code messages
            // TODO: Perhaps work out a more advanced Regex based check at a later time
            if (!CodeReminderCooldown.HasUser(userId))
            {
                string content = messageParam.Content;
                //Changed to a regex check so that bot only alerts when there aren't surrounding backticks, instead of just looking if no triple backticks exist.
                bool foundCodeTags = Regex.Match(content, ".*?`[^`].*?`", RegexOptions.Singleline).Success;
                bool foundCurlyFries = (content.Contains("{") && content.Contains("}"));

                if (!foundCodeTags && foundCurlyFries)
                {
                    CodeReminderCooldown.AddCooldown(userId, _codeReminderCooldownTime);

                    StringBuilder sb = new StringBuilder();
                    sb.Append(messageParam.Author.Mention)
                        .AppendLine(
                            " are you trying to post code? If so, please place 3 backticks \\`\\`\\` at the beginning and end of your code, like so:");
                    sb.AppendLine(_codeReminderFormattingExample);

                    await messageParam.Channel.SendMessageAsync(sb.ToString()).DeleteAfterTime(minutes: 10);
                }
                else if (foundCodeTags && foundCurlyFries && content.Contains("```") && !content.Contains("```cs"))
                {
                    StringBuilder sb = new StringBuilder();
                    sb.Append(messageParam.Author.Mention)
                        .AppendLine(
                            " Don't forget to add \"cs\" after your first 3 backticks so that your code receives syntax highlighting:");
                    sb.AppendLine(_codeReminderFormattingExample);

                    await messageParam.Channel.SendMessageAsync(sb.ToString()).DeleteAfterTime(minutes: 8);

                    CodeReminderCooldown.AddCooldown(userId, _codeReminderCooldownTime);
                }
            }
        }


        public async Task ScoldForAtEveryoneUsage(SocketMessage messageParam)
        {
            if (messageParam.Author.IsBot || ((IGuildUser) messageParam.Author).GuildPermissions.MentionEveryone)
                return;

            ulong userId = messageParam.Author.Id;
            string content = messageParam.Content;

            if (content.Contains("@everyone") || content.Contains("@here"))
            {
                await messageParam.Channel.SendMessageAsync(
                        $"That is very rude of you to try and alert **everyone** on the server {messageParam.Author.Mention}!{Environment.NewLine}" +
                        "Thankfully, you do not have permission to do so. If you are asking a question, people will help you when they have time.")
                    .DeleteAfterTime(minutes: 5);
            }
        }

        // TODO: Response to people asking if anyone is around to help.
        /*
        public async Task UselessAskingCheck(SocketMessage messageParam)
        {
            if (messageParam.Author.IsBot)
                return;

            ulong userId = messageParam.Author.Id;
            string content = messageParam.Content;
        }*/

        //TODO: If Discord ever enables a hook that allows modifying a message during creation of it, then this could be put to use...
        // Disabled for now.
        /*
        public async Task EscapeMessage(SocketMessage messageParam)
        {
            if (messageParam.Author.IsBot)
                return;

            ulong userId = messageParam.Author.Id;
            string content = messageParam.Content;
            //Escape all \, ~, _, ` and * character's so they don't trigger any Discord formatting.
            content = content.EscapeDiscordMarkup();
        }*/

        public async Task<string> SubtitleImage(IMessage message, string text)
        {
            var attachments = message.Attachments;
            Attachment file = null;
            Image<Rgba32> image = null;

            foreach (var a in attachments)
            {
                if (Regex.Match(a.Filename, @"(.*?)\.(jpg|jpeg|png|gif)$").Success)
                    file = (Attachment) a;
            }

            if (file == null)
                return "";

            try
            {
                using (HttpClient client = new HttpClient())
                {
                    using (HttpResponseMessage response = await client.GetAsync(file.Url))
                    {
                        response.EnsureSuccessStatusCode();

                        byte[] reader = await response.Content.ReadAsByteArrayAsync();
                        image = ImageSharp.Image.Load(reader);
                    }
                }
            }
            catch (Exception e)
            {
                Console.WriteLine("Failed to load image : " + e);
                return "";
            }

            float beginHeight = image.Height - (image.Height * 0.3f);
            float beginWidth = (image.Width * .10f);
            float totalWidth = image.Width * .8f;

            //Shitty outline effect
            image.DrawText(text, _subtitlesWhiteFont, Rgba32.Black, new PointF(beginWidth - 4, beginHeight), new TextGraphicsOptions(true)
            {
                WrapTextWidth = totalWidth,
                HorizontalAlignment = HorizontalAlignment.Center,
            });
            image.DrawText(text, _subtitlesWhiteFont, Rgba32.Black, new PointF(beginWidth + 4, beginHeight), new TextGraphicsOptions(true)
            {
                WrapTextWidth = totalWidth,
                HorizontalAlignment = HorizontalAlignment.Center
            });
            image.DrawText(text, _subtitlesWhiteFont, Rgba32.Black, new PointF(beginWidth, beginHeight - 4), new TextGraphicsOptions(true)
            {
                WrapTextWidth = totalWidth,
                HorizontalAlignment = HorizontalAlignment.Center
            });
            image.DrawText(text, _subtitlesWhiteFont, Rgba32.Black, new PointF(beginWidth, beginHeight + 4), new TextGraphicsOptions(true)
            {
                WrapTextWidth = totalWidth,
                HorizontalAlignment = HorizontalAlignment.Center
            });

            image.DrawText(text, _subtitlesWhiteFont, Rgba32.White, new PointF(beginWidth, beginHeight), new TextGraphicsOptions(true)
            {
                WrapTextWidth = totalWidth,
                HorizontalAlignment = HorizontalAlignment.Center
            });

            string path = SettingsHandler.LoadValueString("serverRootPath", JsonFile.Settings) +
                          $"/images/subtitles/{message.Author}-{message.Id}.png";
            image.Save(path, new JpegEncoder {Quality = 95});

            return path;
        }
    }
}