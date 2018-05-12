using System;
using System.Collections.Generic;
using System.Linq;
using System.Net.Http;
using System.Text;
using System.Threading.Tasks;
using System.Web;
using Discord;
using Discord.Commands;
using DiscordBot.Extensions;
using HtmlAgilityPack;
using Newtonsoft.Json;

namespace DiscordBot
{
    public class UserModule : ModuleBase
    {
        private readonly LoggingService _loggingService;
        private readonly DatabaseService _databaseService;
        private readonly UserService _userService;
        private readonly PublisherService _publisherService;

        public UserModule(LoggingService loggingService, DatabaseService databaseService, UserService userService,
            PublisherService publisherService)
        {
            _loggingService = loggingService;
            _databaseService = databaseService;
            _userService = userService;
            _publisherService = publisherService;
        }

        [Command("help"), Summary("Display available commands (this). Syntax : !help")]
        [Alias("command", "commands")]
        private async Task DisplayHelp()
        {
            //TODO: Be possible in DM
            if (Context.Channel.Id != Settings.GetBotCommandsChannel())
            {
                await Task.Delay(1000);
                await Context.Message.DeleteAsync();
                return;
            }

            await ReplyAsync(Settings.GetCommandList());
        }

        [Command("rules"), Summary("Get the of the current channel by DM. Syntax : !rules")]
        private async Task Rules()
        {
            await Rules(Context.Channel);
            await Context.Message.DeleteAsync();
        }

        [Command("rules"), Summary("Get the rules of the mentionned channel by DM. !rules #channel")]
        [Alias("rule")]
        private async Task Rules(IMessageChannel channel)
        {
            Rule rule = Settings.GetRule(channel.Id);
            //IUserMessage m; //Unused, plan to be used in future?
            IDMChannel dm = await Context.User.GetOrCreateDMChannelAsync();
            if (rule == null)
            {
                await dm.SendMessageAsync(
                    "There is no special rule for this channel.\nPlease follow global rules (you can get them by typing `!globalrules`)");
            }
            else
            {
                await dm.SendMessageAsync(
                    $"{rule.header}{(rule.content.Length > 0 ? rule.content : "There is no special rule for this channel.\nPlease follow global rules (you can get them by typing `!globalrules`)")}");
            }

            Task deleteAsync = Context.Message?.DeleteAsync();
            if (deleteAsync != null) await deleteAsync;
        }

        [Command("globalrules"), Summary("Get the Global Rules by DM. Syntax : !globalrules")]
        private async Task GlobalRules(int seconds = 60)
        {
            string globalRules = Settings.GetRule(0).content;
            IDMChannel dm = await Context.User.GetOrCreateDMChannelAsync();
            await dm.SendMessageAsync(globalRules);
            await Context.Message.DeleteAsync();
        }

        [Command("channels"), Summary("Get description of the channels by DM. Syntax : !channels")]
        private async Task ChannelsDescription()
        {
            //Display rules of this channel for x seconds
            List<(ulong, string)> headers = Settings.GetChannelsHeader();
            StringBuilder sb = new StringBuilder();
            foreach (var h in headers)
                sb.Append((await Context.Guild.GetTextChannelAsync(h.Item1))?.Mention).Append(" - ").Append(h.Item2).Append("\n");
            string text = sb.ToString();

            IDMChannel dm = await Context.User.GetOrCreateDMChannelAsync();

            if (sb.ToString().Length > 2000)
            {
                await dm.SendMessageAsync(text.Substring(0, 2000));
                await dm.SendMessageAsync(text.Substring(2000, text.Length));
            }
            else
            {
                await dm.SendMessageAsync(text);
            }

            await Context.Message.DeleteAsync();
        }

        [Command("karma"), Summary("Display description of what Karma is for. Syntax : !karma")]
        private async Task KarmaDescription(int seconds = 60)
        {
            await ReplyAsync($"{Context.User.Username}, " +
                             $"Karma is tracked on your !profile, helping indicate how much you've helped others.{Environment.NewLine}" +
                             $"You also earn slightly more EXP from things the higher your Karma level is. Karma may be used for more features in the future.");

            await Task.Delay(TimeSpan.FromSeconds(seconds));
            await Context.Message.DeleteAsync();
        }

        [Command("top"), Summary("Display top 10 users by level. Syntax : !top")]
        [Alias("toplevel", "ranking")]
        private async Task TopLevel()
        {
            var users = _databaseService.GetTopLevel();

            StringBuilder sb = new StringBuilder();
            sb.Append("Here's the top 10 of users by level :");
            for (int i = 0; i < users.Count; i++)
                sb.Append(
                    $"\n#{i + 1} - **{(await Context.Guild.GetUserAsync(users[i].userId)).Username}** ~ *Level* **{users[i].level}**");

            await ReplyAsync(sb.ToString()).DeleteAfterTime(minutes: 3);
        }

        [Command("topkarma"), Summary("Display top 10 users by karma. Syntax : !topkarma")]
        [Alias("karmarank", "rankingkarma")]
        private async Task TopKarma()
        {
            var users = _databaseService.GetTopKarma();

            StringBuilder sb = new StringBuilder();
            sb.Append("Here's the top 10 of users by karma :");
            for (int i = 0; i < users.Count; i++)
                sb.Append(
                    $"\n#{i + 1} - **{(await Context.Guild.GetUserAsync(users[i].userId)).Username}** ~ **{users[i].karma}** *Karma*");

            await ReplyAsync(sb.ToString()).DeleteAfterTime(minutes: 3);
        }

        [Command("topudc"), Summary("Display top 10 users by UDC. Syntax : !topudc")]
        [Alias("udcrank")]
        private async Task TopUdc()
        {
            var users = _databaseService.GetTopUdc();

            StringBuilder sb = new StringBuilder();
            sb.Append("Here's the top 10 of users by UDC :");
            for (int i = 0; i < users.Count; i++)
                sb.Append($"\n#{i + 1} - **{(await Context.Guild.GetUserAsync(users[i].userId)).Username}** ~ **{users[i].udc}** *UDC*");

            await ReplyAsync(sb.ToString()).DeleteAfterTime(minutes: 3);
        }

        [Command("codetip"), Summary("Show code formatting example. Syntax : !codetip userToPing(optional)")]
        [Alias("codetips")]
        private async Task CodeTip(IUser user = null)
        {
            var message = (user != null) ? user.Mention + ", " : "";
            message += "When posting code, format it like this to display it properly:" + Environment.NewLine;
            message += _userService._codeFormattingExample;
            await Context.Message.DeleteAsync();
            ReplyAsync(message).DeleteAfterSeconds(240);
        }

        [Command("disablecodetips"), Summary("Prevents being reminded about using proper code formatting when code is detected. Syntax : !disablecodetips")]
        private async Task DisableCodeTips()
        {
            ulong userID = Context.User.Id;
            string replyMessage = "You've already told me to stop reminding you, don't worry, I won't forget!";

            if (!_userService.CodeReminderCooldown.IsPermanent(userID))
            {
                replyMessage = "I will no longer remind you about using proper code formatting.";
                _userService.CodeReminderCooldown.SetPermanent(Context.User.Id, true);
            }

            await ReplyAsync($"{Context.User.Username}, " + replyMessage).DeleteAfterTime(seconds: 20);
        }

        [Command("disablethanksreminder"),
         Summary("Prevents being reminded to mention the person you are thanking. Syntax : !disablethanksreminder")]
        private async Task DisableThanksReminder()
        {
            ulong userID = Context.User.Id;
            string replyMessage = "You've already told me to stop reminding you, don't worry, I won't forget!";

            if (!_userService.ThanksReminderCooldown.IsPermanent(userID))
            {
                replyMessage = "I will no longer remind you to mention the person you're thanking... (◕︿◕✿)";
                _userService.ThanksReminderCooldown.SetPermanent(Context.User.Id, true);
            }

            await ReplyAsync($"{Context.User.Username}, " + replyMessage).DeleteAfterTime(seconds: 20);
        }

        [Command("slap"), Summary("Slap the specified user(s). Syntax : !slap @user1 [@user2 @user3...]")]
        private async Task SlapUser(params IUser[] users)
        {
            StringBuilder sb = new StringBuilder();
            string[] slaps = { "trout", "duck", "truck" };
            var random = new Random();

            sb.Append("**").Append(Context.User.Username).Append("** Slaps ");
            foreach (var user in users)
            {
                sb.Append(user.Mention).Append(" ");
            }

            sb.Append("around a bit with a large ").Append(slaps[random.Next() % 3]);

            await Context.Channel.SendMessageAsync(sb.ToString());
            await Task.Delay(1000);
            await Context.Message.DeleteAsync();
        }

        [Command("profile"), Summary("Display current user profile card. Syntax : !profile")]
        private async Task DisplayProfile()
        {
            IUserMessage profile = await Context.Channel.SendFileAsync(await _userService.GenerateProfileCard(Context.Message.Author));

            await Task.Delay(10000);
            await Context.Message.DeleteAsync();
            await Task.Delay(TimeSpan.FromMinutes(1d));
            await profile.DeleteAsync();
        }

        [Command("profile"), Summary("Display profile card of mentionned user. Syntax : !profile @user")]
        private async Task DisplayProfile(IUser user)
        {
            IUserMessage profile = await Context.Channel.SendFileAsync(await _userService.GenerateProfileCard(user));

            await Task.Delay(1000);
            await Context.Message.DeleteAsync();
            await Task.Delay(TimeSpan.FromMinutes(1d));
            await profile.DeleteAsync();
        }

        [Command("quote"), Summary("Quote a message in current channel. Syntax : !quote messageid")]
        private async Task QuoteMessage(ulong id)
        {
            await Context.Message.DeleteAsync();
            IMessageChannel channel = Context.Channel;
            var message = await channel.GetMessageAsync(id);
            Console.WriteLine($"message {message.Author.Username}  {message.Channel.Name}");
            var builder = new EmbedBuilder()
                .WithColor(new Color(200, 128, 128))
                .WithTimestamp(message.Timestamp)
                .WithFooter(footer =>
                {
                    footer
                        .WithText($"In channel {message.Channel.Name}");
                })
                .WithAuthor(author =>
                {
                    author
                        .WithName(message.Author.Username)
                        .WithIconUrl(message.Author.GetAvatarUrl());
                })
                .AddField("Original message", message.Content.Truncate(1020));
            var embed = builder.Build();
            await ReplyAsync("", false, embed);
        }

        [Command("quote"), Summary("Quote a message. Syntax : !quote #channelname messageid")]
        private async Task QuoteMessage(IMessageChannel channel, ulong id)
        {
            var message = await channel.GetMessageAsync(id);
            var builder = new EmbedBuilder()
                .WithColor(new Color(200, 128, 128))
                .WithTimestamp(message.Timestamp)
                .WithFooter(footer =>
                {
                    footer
                        .WithText($"In channel {message.Channel.Name}");
                })
                .WithAuthor(author =>
                {
                    author
                        .WithName(message.Author.Username)
                        .WithIconUrl(message.Author.GetAvatarUrl());
                })
                .AddField("Original message", message.Content.Truncate(1020));
            var embed = builder.Build();
            await ReplyAsync("", false, embed);
            await Task.Delay(1000);
            await Context.Message.DeleteAsync();
        }

        [Command("compile"),
         Summary("Try to compile a snippet of C# code. Be sure to escape your strings. Syntax : !compile \"Your code\"")]
        [Alias("code", "compute", "assert")]
        private async Task CompileCode(params string[] code)
        {
            string codeComplete =
                $"using System;\nusing System.Collections.Generic;\n\n\tpublic class Hello\n\t{{\n\t\tpublic static void Main()\n\t\t{{\n\t\t\t{String.Join(" ", code)}\n\t\t}}\n\t}}\n";

            var parameters = new Dictionary<string, string>
            {
                {"source_code", codeComplete},
                {"language", "csharp"},
                {"api_key", "guest"}
            };
            var content = new FormUrlEncodedContent(parameters);

            var message = await ReplyAsync("Please wait a moment, trying to compile your code interpreted as\n" +
                                           $"```cs\n{codeComplete}```");

            using (HttpClient client = new HttpClient())
            {
                HttpResponseMessage httpResponse = await client.PostAsync("http://api.paiza.io/runners/create", content);
                var response = JsonConvert.DeserializeObject<Dictionary<string, string>>(await httpResponse.Content.ReadAsStringAsync());

                string id = response["id"];
                string status;
                DateTime startTime = DateTime.Now;
                const int maxTime = 30;

                do
                {
                    httpResponse = await client.GetAsync($"http://api.paiza.io/runners/get_details?id={id}&api_key=guest");
                    response = JsonConvert.DeserializeObject<Dictionary<string, string>>(await httpResponse.Content.ReadAsStringAsync());
                    status = response["status"];
                    await Task.Delay(300);
                } while (status != "completed" && (DateTime.Now - startTime).TotalSeconds < maxTime);

                string newMessage;

                if (status != "completed")
                {
                    newMessage = (message.Content + "The code didn't compile in time.").Truncate(1990);
                    await message.ModifyAsync(m => m.Content = newMessage);
                    return;
                }

                string build_stddout = response["build_stdout"];
                string stdout = response["stdout"];
                string stderr = response["stderr"];
                string build_stderr = response["build_stderr"];
                string result = response["build_result"];

                string fullMessage;

                if (result == "failure")
                {
                    fullMessage = message.Content + "The code resulted in a failure.\n"
                                                  + (build_stddout.Length > 0
                                                      ? $"```cs\n{build_stddout}```\n"
                                                      : "") +
                                                  (build_stderr.Length > 0
                                                      ? $"```cs\n{build_stderr}\n"
                                                      : "```");
                }
                else
                {
                    fullMessage = message.Content + "Result : "
                                                  + (stdout.Length > 0 ? $"```cs\n{stdout}```" : "") +
                                                  $"```cs\n{stderr}\n";
                }

                httpResponse = await client.PostAsync("https://hastebin.com/documents", new StringContent(fullMessage.Truncate(10000)));
                response = JsonConvert.DeserializeObject<Dictionary<string, string>>(await httpResponse.Content.ReadAsStringAsync());

                newMessage = ($"\nFull result : https://hastebin.com/{response["key"]}\n" + fullMessage).Truncate(1990) + "```";
                await message.ModifyAsync(m => m.Content = newMessage);
            }
        }

        [Command("coinflip"), Summary("Flip a coin and see the result. Syntax : !coinflip")]
        [Alias("flipcoin")]
        private async Task CoinFlip()
        {
            Random rand = new Random();
            var coin = new[] { "Heads", "Tails" };

            await ReplyAsync($"**{Context.User.Username}** flipped a coin and got **{coin[rand.Next() % 2]}** !");
            await Task.Delay(1000);
            await Context.Message.DeleteAsync();
        }

        [Command("subtitle"), Summary("Add a subtitle to an image attached. Syntax : !subtitle \"Text to write\"")]
        [Alias("subtitles", "sub", "subs")]
        private async Task Subtitles(string text)
        {
            var msg = await _userService.SubtitleImage(Context.Message, text);
            if (msg.Length < 6)
                await ReplyAsync("Sorry, there was an error processing your image.");
            else
                await Context.Channel.SendFileAsync(msg, $"From {Context.Message.Author.Mention}");
            await Context.Message.DeleteAsync();
        }

        [Command("pinfo"), Summary("Information on how to get the publisher role. Syntax : !pinfo")]
        [Alias("publisherinfo")]
        private async Task PublisherInfo()
        {
            if (Context.Channel.Id != Settings.GetBotCommandsChannel())
            {
                await Task.Delay(1000);
                await Context.Message.DeleteAsync();
                return;
            }

            Random rand = new Random();
            var coin = new[] { "Heads", "Tails" };

            await ReplyAsync($"\n" +
                             "**Publisher - BOT COMMANDS : ** ``these commands are not case-sensitive.``\n" +
                             "``!pkg ID`` - To add your package to Publisher everyday Advertising , ID means the digits on your package link.\n" +
                             "``!verify packageId verifCode`` - Verify your package with the code send to your email.");

            await Task.Delay(10000);
            await Context.Message.DeleteAsync();
        }

        [Command("pkg"), Summary("Add your published package to the daily advertising. Syntax : !pkg packageId")]
        [Alias("package")]
        private async Task Package(uint packageId)
        {
            if (Context.Channel.Id != Settings.GetBotCommandsChannel())
            {
                await Task.Delay(1000);
                await Context.Message.DeleteAsync();
                return;
            }

            (bool, string) verif = await _publisherService.VerifyPackage(packageId);
            await ReplyAsync(verif.Item2);
        }

        [Command("verify"), Summary("Verify a package with the code received by email. Syntax : !verify packageId code")]
        private async Task VerifyPackage(uint packageId, string code)
        {
            if (Context.Channel.Id != Settings.GetBotCommandsChannel())
            {
                await Task.Delay(1000);
                await Context.Message.DeleteAsync();
                return;
            }

            string verif = await _publisherService.ValidatePackageWithCode(Context.Message.Author, packageId, code);
            await ReplyAsync(verif);
        }

        [Command("search"), Summary("Searches on DuckDuckGo for web results. Syntax : !search \"query\" resNum site")]
        [Alias("s", "ddg")]
        private async Task SearchResults(string query, uint resNum = 3, string site = "")
        {
            // Cleaning inputs from user (maybe we can ban certain domains or keywords)
            resNum = resNum <= 5 ? resNum : 5;
            string searchQuery = "https://duckduckgo.com/html/?q=" + query.Replace(' ', '+');

            if (!site.Equals(""))
            {
                searchQuery += "+site:" + site;
            }

            HtmlDocument doc = new HtmlWeb().Load(searchQuery);
            int counter = 1;
            List<string> results = new List<string>();

            // XPath for DuckDuckGo as of 10/05/2018, if results stop showing up, check this first!
            foreach (HtmlNode row in doc.DocumentNode.SelectNodes("/html/body/div[1]/div[3]/div/div/div[*]/div/h2/a"))
            {
                // Check if we are within the allowed number of results and if the result is valid (i.e. no evil ads)
                if (counter <= resNum && IsValidResult(row))
                {
                    string title = HttpUtility.UrlDecode(row.InnerText);
                    string url = HttpUtility.UrlDecode(row.Attributes["href"].Value.Replace("/l/?kh=-1&amp;uddg=", ""));
                    string msg = "";

                    // Added line for pretty output
                    if (counter > 1)
                    {
                        msg += "──────────────────────────────────────────\n";
                    }

                    msg += counter + ". **" + title + "**\nRead More: " + url;
                    results.Add(msg);
                    counter++;
                }
            }

            // Send each result as separate message for embedding
            foreach (string msg in results)
            {
                await ReplyAsync(msg);
            }

        }

        // Utility function for avoiding evil ads from DuckDuckGo
        private bool IsValidResult(HtmlNode node)
        {
            return !node.Attributes["href"].Value.Contains("duckduckgo.com") &&
                   !node.Attributes["href"].Value.Contains("duck.co");
        }

        [Group("role")]
        public class RoleModule : ModuleBase
        {
            private readonly LoggingService _logging;

            public RoleModule(LoggingService logging)
            {
                _logging = logging;
            }

            [Command("add"), Summary("Add a role to yourself. Syntax : !role add role")]
            private async Task AddRoleUser(IRole role)
            {
                if (Context.Channel.Id != Settings.GetBotCommandsChannel())
                {
                    await Task.Delay(1000);
                    await Context.Message.DeleteAsync();
                    return;
                }

                if (!Settings.IsRoleAssignable(role))
                {
                    await ReplyAsync("This role is not assigneable");
                    return;
                }

                var u = Context.User as IGuildUser;

                await u.AddRoleAsync(role);
                await ReplyAsync($"{u.Username} you now have the role of `{role.Name}`");
                await _logging.LogAction($"{Context.User.Username} has added role {role} to himself in {Context.Channel.Name}");
            }

            [Command("remove"), Summary("Remove a role from yourself. Syntax : !role remove role")]
            [Alias("delete")]
            private async Task RemoveRoleUser(IRole role)
            {
                if (Context.Channel.Id != Settings.GetBotCommandsChannel())
                {
                    await Task.Delay(1000);
                    await Context.Message.DeleteAsync();
                    return;
                }

                if (!Settings.IsRoleAssignable(role))
                {
                    await ReplyAsync("Role is not assigneable");
                    return;
                }

                var u = Context.User as IGuildUser;

                await u.RemoveRoleAsync(role);
                await ReplyAsync($"{u.Username} your role of `{role.Name}` has been removed");
                await _logging.LogAction($"{Context.User.Username} has removed role {role} from himself in {Context.Channel.Name}");
            }

            [Command("list"), Summary("Display the list of roles. Syntax : !role list")]
            private async Task ListRole()
            {
                if (Context.Channel.Id != Settings.GetBotCommandsChannel())
                {
                    await Task.Delay(1000);
                    await Context.Message.DeleteAsync();
                    return;
                }

                await ReplyAsync("**The following roles are available on this server** :\n" +
                                 "\n" +
                                 "We offer multiple roles to show what you specialize in, so if you are particularly good at anything, assign your role! \n" +
                                 "You can have multiple specialties and your color is determined by the highest role you hold \n" +
                                 "\n" +
                                 "```To get the publisher role type **!pinfo** and follow the instructions." +
                                 "https://www.assetstore.unity3d.com/en/#!/search/page=1/sortby=popularity/query=publisher:7285 <= Example Digits```\n");
                await ReplyAsync("```!role add/remove Artists - The Graphic Designers, Artists and Modellers. \n" +
                                 "!role add/remove 3DModelers - People behind every vertex. \n" +
                                 "!role add/remove Coders - The valiant knights of programming who toil away, without rest. \n" +
                                 "!role add/remove C# - If you are using C# to program in Unity3D \n" +
                                 "!role add/remove Javascript - If you are using Javascript to program in Unity3D \n" +
                                 "!role add/remove Game-Designers - Those who specialise in writing, gameplay design and level design.\n" +
                                 "!role add/remove Audio-Artists - The unsung heroes of sound effects .\n" +
                                 "!role add/remove Generalists - Generalist may refer to a person with a wide array of knowledge.\n" +
                                 "!role add/remove Hobbyists - A person who is interested in Unity3D or Game Making as a hobby.\n" +
                                 "!role add/remove Vector-Artists - The people who love to have infinite resolution.\n" +
                                 "!role add/remove Voxel-Artist - People who love to voxelize the world.\n" +
                                 "!role add/remove Students - The eager learners among us, never stop learning. \n" +
                                 "!role add/remove VR-Developers - Passionate people who wants to bridge virtual world with real life. \n" +
                                 "--------------------------------------------------------------------------------------------\n" +
                                 "!role add/remove Streamer - If you stream on twitch/youtube or other discord integrated platforms content about tutorials and gaming. \n" +
                                 "```");
            }
        }

        [Group("anime")]
        public class AnimeModule : ModuleBase
        {
            private readonly AnimeService _animeService;

            public AnimeModule(AnimeService animeService)
            {
                _animeService = animeService;
            }

            [Command("search"), Summary("Returns an anime. Syntax : !anime search animeTitle")]
            private async Task SearchAnime(string title)
            {
                /*{
                  "content": "Here's your search result @blabla",
                  "embed": {
                    "title": "Anime search result",

                    "url": "https://discordapp.com",
                    "color": 14574459,
                    "thumbnail": {
                      "url": "https://cdn.anilist.co/img/dir/anime/reg/20800-Bdc1fJOBED6C.jpg"
                    },
                    "image": {
                      "url": "https://cdn.anilist.co/img/dir/anime/reg/20800-Bdc1fJOBED6C.jpg"
                    },

                    "fields": [
                      {
                        "name" : "Titles",
                        "value" : "Yuuki Yuuna wa Yuusha De Aru, 結城友奈は勇者である"
                      },
                      {
                        "name": "Description",
                        "value": "The story takes place in the era of the gods, year 300. Yuuna Yuuki lives an ordinary life as a second year middle school student, but she's also a member of the \"Hero Club,\" where club activities involve dealing with a mysterious being called \"Vertex.\""
                      },
                      {
                        "name": "Genres",
                        "value" : "Mahou Shoujo, Action, Drama"
                      },
                      {
                        "name": "MAL Link",
                        "value" : "https://myanimelist.net/anime/25519"
                      },
                      {
                        "name": "Start Date",
                        "value": "17/10/2014",
                        "inline": true
                      },
                      {
                        "name": "End Date",
                        "value": "26/12/2014",
                        "inline": true
                      }
                    ]
                  }
                }*/

                var animes = await _animeService.SearchAnime(title);
                var anime = animes.data.Page.media.FirstOrDefault();
                if (anime == null)
                {
                    await ReplyAsync("I'm sorry, I couldn't find an anime with this name.");
                    return;
                }

                var builder = new EmbedBuilder()
                    .WithTitle("Anime search result")
                    .WithUrl("https://myanimelist.net/anime/" + anime.idMal)
                    .WithColor(new Color(0xDE637B))
                    .WithThumbnailUrl(anime.coverImage.medium)
                    .WithImageUrl(anime.coverImage.medium)
                    .AddField("Titles", $"{anime.title.romaji}, {anime.title.native}")
                    .AddField("Description", anime.description.Truncate(1020))
                    .AddField("Genres", string.Join(",", anime.genres))
                    .AddField("MAL Link", "https://myanimelist.net/anime/" + anime.idMal)
                    .AddField("Start Date", $"{anime.startDate.day}/{anime.startDate.month}/{anime.startDate.year}")
                    .AddField("End Date", $"{anime.endDate.day}/{anime.endDate.month}/{anime.endDate.year}");
                var embed = builder.Build();
                await Context.Channel.SendMessageAsync($"Here's your search result {Context.Message.Author.Mention}", false, embed)
                    .ConfigureAwait(false);
            }
        }
    }
}