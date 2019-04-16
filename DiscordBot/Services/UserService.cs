using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Net.Http;
using System.Text;
using System.Text.RegularExpressions;
using System.Threading.Tasks;
using Discord;
using Discord.WebSocket;
using DiscordBot.Domain;
using DiscordBot.Extensions;
using DiscordBot.Settings.Deserialized;
using DiscordBot.Skin;
using ImageMagick;
using Newtonsoft.Json;

namespace DiscordBot.Services
{
    public class UserService
    {
        private readonly DiscordSocketClient _client;
        private readonly DatabaseService _databaseService;
        private readonly ILoggingService _loggingService;
        private readonly UpdateService _updateService;

        private readonly Settings.Deserialized.Settings _settings;
        private readonly UserSettings _userSettings;

        public Dictionary<ulong, DateTime> _mutedUsers;

        private readonly Dictionary<ulong, DateTime> _xpCooldown;
        private readonly HashSet<ulong> _canEditThanks; //Doesn't need to be saved
        private readonly Dictionary<ulong, DateTime> _thanksCooldown;
        private Dictionary<ulong, DateTime> _thanksReminderCooldown;

        public Dictionary<ulong, DateTime> ThanksReminderCooldown => _thanksReminderCooldown;

        private Dictionary<ulong, DateTime> _codeReminderCooldown;

        public Dictionary<ulong, DateTime> CodeReminderCooldown => _codeReminderCooldown;

        private readonly Random rand;

        private readonly string _thanksRegex;

        private readonly int _thanksCooldownTime;
        private readonly int _thanksReminderCooldownTime;
        private readonly int _thanksMinJoinTime;

        private readonly int _xpMinPerMessage;
        private readonly int _xpMaxPerMessage;
        private readonly int _xpMinCooldown;
        private readonly int _xpMaxCooldown;

        private readonly int _codeReminderCooldownTime;
        public readonly string _codeFormattingExample;
        public readonly string _codeReminderFormattingExample;

        private readonly List<ulong> _noXpChannels;

        //TODO: Add custom commands for user after (30karma ?/limited to 3 ?)

        public UserService(DiscordSocketClient client,DatabaseService databaseService, ILoggingService loggingService, UpdateService updateService,
            Settings.Deserialized.Settings settings, UserSettings userSettings)
        {
            _client = client;
            rand = new Random();
            _databaseService = databaseService;
            _loggingService = loggingService;
            _updateService = updateService;
            _settings = settings;
            _userSettings = userSettings;
            _mutedUsers = new Dictionary<ulong, DateTime>();
            _xpCooldown = new Dictionary<ulong, DateTime>();
            _canEditThanks = new HashSet<ulong>(32);
            _thanksCooldown = new Dictionary<ulong, DateTime>();
            _thanksReminderCooldown = new Dictionary<ulong, DateTime>();
            _codeReminderCooldown = new Dictionary<ulong, DateTime>();

            _noXpChannels = new List<ulong>
            {
                _settings.BotCommandsChannel.Id, _settings.CasinoChannel.Id, _settings.MusicCommandsChannel.Id
            }; 

            /*
            Init XP
            */
            _xpMinPerMessage = _userSettings.XpMinPerMessage;
            _xpMaxPerMessage = _userSettings.XpMaxPerMessage;
            _xpMinCooldown = _userSettings.XpMinCooldown;
            _xpMaxCooldown = _userSettings.XpMaxCooldown;

            /*
            Init thanks
            */
            StringBuilder sbThanks = new StringBuilder();
            var thx = _userSettings.Thanks;
            sbThanks.Append("(?i)\\b(");
            foreach (var t in thx)
            {
                sbThanks.Append(t).Append("|");
            }

            sbThanks.Length--; //Efficiently remove the final pipe that gets added in final loop, simplifying loop
            sbThanks.Append(")\\b");
            _thanksRegex = sbThanks.ToString();
            _thanksCooldownTime = _userSettings.ThanksCooldown;
            _thanksReminderCooldownTime = _userSettings.ThanksReminderCooldown;
            _thanksMinJoinTime = _userSettings.ThanksMinJoinTime;

            /*
             Init Code analysis
            */
            _codeReminderCooldownTime = _userSettings.CodeReminderCooldown;
            _codeFormattingExample = (
                @"\`\`\`cs" + Environment.NewLine +
                "Write your code on new line here." + Environment.NewLine +
                @"\`\`\`" + Environment.NewLine);
            _codeReminderFormattingExample = (
                _codeFormattingExample + Environment.NewLine +
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
                MutedUsers = _mutedUsers, ThanksReminderCooldown = _thanksReminderCooldown, CodeReminderCooldown = _codeReminderCooldown
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
            if (messageParam.Author.Activity != null)
            {
                if (Regex.Match(messageParam.Author.Activity.Name, "(Unity.+)").Length > 0)
                    bonusXp += baseXp / 4;
            }

            bonusXp += baseXp * (1f + (karma / 100f));

            //Reduce XP for members with no role
            if (((IGuildUser) messageParam.Author).RoleIds.Count < 2)
                baseXp *= .9f;

            //Lower xp for difference between level and karma
            uint level = _databaseService.GetUserLevel(userId);
            float reduceXp = 1f;
            if (karma < level)
            {
                reduceXp = 1 - Math.Min(.9f, (level - karma) * .05f);
            }

            int xpGain = (int) Math.Round((baseXp + bonusXp) * reduceXp);
            //Console.WriteLine($"basexp {baseXp} karma {karma}  bonus {bonusXp}");
            _xpCooldown.AddCooldown(userId, waitTime);
            //Console.WriteLine($"{_xpCooldown[id].Minute}  {_xpCooldown[id].Second}");

            if (!await _databaseService.UserExists(userId))
                _databaseService.AddNewUser((SocketGuildUser) messageParam.Author);

            _databaseService.AddUserXp(userId, xpGain);
            _databaseService.AddUserUdc(userId, (int) Math.Round(xpGain * .15f));

            _loggingService.LogXp(messageParam.Channel.Name, messageParam.Author.Username, baseXp, bonusXp, reduceXp, xpGain);

            await LevelUp(messageParam, userId);

            //TODO: add xp gain on website
        }

        /// <summary>
        /// Show level up emssage
        /// </summary>
        /// <param name="messageParam"></param>
        /// <param name="userId"></param>
        /// <returns></returns>
        public async Task LevelUp(SocketMessage messageParam, ulong userId)
        {
            int level = (int) (_databaseService.GetUserLevel(userId));
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

        private SkinData GetSkinData()
        {
            return JsonConvert.DeserializeObject<SkinData>(File.ReadAllText($"{_settings.ServerRootPath}/skins/skin.json"),
                new SkinModuleJsonConverter());
        }

        /// <summary>
        /// Generate the profile card for a given user and returns the generated image path
        /// </summary>
        /// <param name="user"></param>
        /// <returns></returns>
        public async Task<string> GenerateProfileCard(IUser user)
        {
            ulong userId = user.Id;
            uint xpTotal = _databaseService.GetUserXp(userId);
            uint xpRank = _databaseService.GetUserRank(userId);
            int karma = _databaseService.GetUserKarma(userId);
            uint level = _databaseService.GetUserLevel(userId);
            uint karmaRank = _databaseService.GetUserKarmaRank(userId);
            double xpLow = GetXpLow((int) level);
            double xpHigh = GetXpHigh((int) level);

            uint xpShown = (uint) (xpTotal - xpLow);
            uint maxXpShown = (uint) (xpHigh - xpLow);

            float percentage = (float) xpShown / maxXpShown;

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

            using (MagickImageCollection profileCard = new MagickImageCollection())
            {
                SkinData skin = GetSkinData();
                ProfileData profile = new ProfileData
                {
                    Karma = karma,
                    KarmaRank = karmaRank,
                    Level = level,
                    MainRoleColor = mainRole.Color,
                    MaxXpShown = maxXpShown,
                    Nickname = (user as IGuildUser).Nickname,
                    UserId = userId,
                    Username = user.Username,
                    XpHigh = xpHigh,
                    XpLow = xpLow,
                    XpPercentage = percentage,
                    XpRank = xpRank,
                    XpShown = xpShown,
                    XpTotal = xpTotal
                };

                MagickImage background = new MagickImage($"{_settings.ServerRootPath}/skins/{skin.Background}");

                string avatarUrl = user.GetAvatarUrl(ImageFormat.Auto, 256);
                if (string.IsNullOrEmpty(avatarUrl))
                {
                    profile.Picture = new MagickImage($"{_settings.ServerRootPath}/images/default.png");
                }
                else
                {
                    try
                    {
                        Stream stream;

                        using (var http = new HttpClient())
                        {
                            stream = await http.GetStreamAsync(new Uri(avatarUrl));
                        }

                        profile.Picture = new MagickImage(stream);
                    }
                    catch (Exception e)
                    {
                        Console.WriteLine(e);
                        profile.Picture = new MagickImage($"{_settings.ServerRootPath}/images/default.png");
                    }
                }

                profile.Picture.Resize((int) skin.AvatarSize, skin.AvatarSize);
                profileCard.Add(background);

                foreach (var layer in skin.Layers)
                {
                    if (layer.Image != null)
                    {
                        MagickImage image = layer.Image.ToLower() == "avatar"
                            ? profile.Picture
                            : new MagickImage($"{_settings.ServerRootPath}/skins/{layer.Image}");

                        background.Composite(image, (int) layer.StartX, (int) layer.StartY, CompositeOperator.Over);
                    }

                    MagickImage l = new MagickImage(MagickColors.Transparent, (int) layer.Width, (int) layer.Height);
                    foreach (var module in layer.Modules)
                    {
                        module.GetDrawables(profile).Draw(l);
                    }

                    background.Composite(l, (int) layer.StartX, (int) layer.StartY, CompositeOperator.Over);
                }

                using (IMagickImage result = profileCard.Mosaic())
                {
                    result.Write($"{_settings.ServerRootPath}/images/profiles/{user.Username}-profile.png");
                }
            }

            return $"{_settings.ServerRootPath}/images/profiles/{user.Username}-profile.png";
        }


        public Embed WelcomeMessage(string icon, string name, ushort discriminator)
        {
            icon = string.IsNullOrEmpty(icon) ? "https://cdn.discordapp.com/embed/avatars/0.png" : icon;

            EmbedBuilder builder = new EmbedBuilder()
                .WithDescription($"Welcome to Unity Developer Community **{name}#{discriminator}** !")
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
        public async Task ThanksEdited(Cacheable<IMessage, ulong> cachedMessage, SocketMessage messageParam,
            ISocketMessageChannel socketMessageChannel)
        {
            if (_canEditThanks.Contains(messageParam.Id))
            {
                await Thanks(messageParam);
            }
        }


        public async Task Thanks(SocketMessage messageParam)
        {
            //Get guild id
            SocketGuildChannel channel = messageParam.Channel as SocketGuildChannel;
            ulong guildId = channel.Guild.Id;

            //Make sure its in the UDH server
            if (guildId != _settings.guildId) {
                return;
            }

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
                else if (foundCodeTags && foundCurlyFries && content.Contains("```") && !content.ToLower().Contains("```cs"))
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

        public int GetGatewayPing()
        {
            return _client.Latency;
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
       /* public async Task<string> SubtitleImage(IMessage message, string text)
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

            image.DrawText(text, _subtitlesWhiteFont, Rgba32.Black, new PointF(beginWidth - 4, beginHeight),
                new TextGraphicsOptions(true) {WrapTextWidth = totalWidth, HorizontalAlignment = HorizontalAlignment.Center,});
            image.DrawText(text, _subtitlesWhiteFont, Rgba32.Black, new PointF(beginWidth + 4, beginHeight),
                new TextGraphicsOptions(true) {WrapTextWidth = totalWidth, HorizontalAlignment = HorizontalAlignment.Center});
            image.DrawText(text, _subtitlesWhiteFont, Rgba32.Black, new PointF(beginWidth, beginHeight - 4),
                new TextGraphicsOptions(true) {WrapTextWidth = totalWidth, HorizontalAlignment = HorizontalAlignment.Center});
            image.DrawText(text, _subtitlesWhiteFont, Rgba32.Black, new PointF(beginWidth, beginHeight + 4),
                new TextGraphicsOptions(true) {WrapTextWidth = totalWidth, HorizontalAlignment = HorizontalAlignment.Center});
            image.DrawText(text, _subtitlesWhiteFont, Rgba32.White, new PointF(beginWidth, beginHeight),
                new TextGraphicsOptions(true) {WrapTextWidth = totalWidth, HorizontalAlignment = HorizontalAlignment.Center});
            string path = $"{_settings.ServerRootPath}/images/subtitles/{message.Author}-{message.Id}.png";

            image.Save(path, new JpegEncoder {Quality = 95});
            return path;
        }*/
    }
}