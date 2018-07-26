using System;
using System.Collections.Generic;
using System.Globalization;
using System.Linq;
using System.Net;
using System.Net.Http;
using System.Text;
using System.Threading.Tasks;
using Discord;
using Discord.Commands;
using DiscordBot.Extensions;
using DiscordBot.Properties;
using DiscordBot.Services;
using DiscordBot.Settings.Deserialized;
using HtmlAgilityPack;
using Newtonsoft.Json;

namespace DiscordBot.Modules
{
    public class UserModule : ModuleBase
    {
        private readonly LoggingService _loggingService;
        private readonly DatabaseService _databaseService;
        private readonly UserService _userService;
        private readonly PublisherService _publisherService;
        private readonly UpdateService _updateService;

        private readonly Rules _rules;
        private static Settings.Deserialized.Settings _settings;

        public UserModule(LoggingService loggingService, DatabaseService databaseService, UserService userService,
            PublisherService publisherService, UpdateService updateService, Rules rules
            , Settings.Deserialized.Settings settings)
        {
            _loggingService = loggingService;
            _databaseService = databaseService;
            _userService = userService;
            _publisherService = publisherService;
            _updateService = updateService;
            _rules = rules;
            _settings = settings;
        }

        [Command("help"), Summary("Display available commands (this). Syntax : !help")]
        [Alias("command", "commands")]
        private async Task DisplayHelp()
        {
            //TODO: Be possible in DM
            if (Context.Channel.Id != _settings.BotCommandsChannel.Id)
            {
                await Task.Delay(1000);
                await Context.Message.DeleteAsync();
                return;
            }

            var commands = Program.CommandList;

            foreach (var message in commands.MessageSplit())
                await ReplyAsync(message);
        }

        #region Rules

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
            var rule = _rules.Channel.First(x => x.Id == channel.Id);
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
                    $"{rule.Header}{(rule.Content.Length > 0 ? rule.Content : "There is no special rule for this channel.\nPlease follow global rules (you can get them by typing `!globalrules`)")}");
            }

            Task deleteAsync = Context.Message?.DeleteAsync();
            if (deleteAsync != null) await deleteAsync;
        }

        [Command("globalrules"), Summary("Get the Global Rules by DM. Syntax : !globalrules")]
        private async Task GlobalRules(int seconds = 60)
        {
            string globalRules = _rules.Channel.First(x => x.Id == 0).Content;
            IDMChannel dm = await Context.User.GetOrCreateDMChannelAsync();
            await dm.SendMessageAsync(globalRules);
            await Context.Message.DeleteAsync();
        }

        [Command("channels"), Summary("Get description of the channels by DM. Syntax : !channels")]
        private async Task ChannelsDescription()
        {
            //Display rules of this channel for x seconds
            var channelData = _rules.Channel;
            StringBuilder sb = new StringBuilder();
            foreach (var c in channelData)
                sb.Append((await Context.Guild.GetTextChannelAsync(c.Id))?.Mention).Append(" - ").Append(c.Header).Append("\n");
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

        #endregion

        #region XP & Karma

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

        [Command("profile"), Summary("Display current user profile card. Syntax : !profile")]
        private async Task DisplayProfile()
        {
            IUserMessage profile =
                await Context.Channel.SendFileAsync(await _userService.GenerateProfileCard(Context.Message.Author));

            await Task.Delay(10000);
            await Context.Message.DeleteAsync();
            await Task.Delay(TimeSpan.FromMinutes(3d));
            await profile.DeleteAsync();
        }

        [Command("profile"), Summary("Display profile card of mentionned user. Syntax : !profile @user")]
        private async Task DisplayProfile(IUser user)
        {
            IUserMessage profile = await Context.Channel.SendFileAsync(await _userService.GenerateProfileCard(user));

            await Task.Delay(1000);
            await Context.Message.DeleteAsync();
            await Task.Delay(TimeSpan.FromMinutes(3d));
            await profile.DeleteAsync();
        }

        #endregion

        #region Codetips

        [Command("codetip"), Summary("Show code formatting example. Syntax : !codetip userToPing(optional)")]
        [Alias("codetips")]
        private async Task CodeTip(IUser user = null)
        {
            var message = (user != null) ? user.Mention + ", " : "";
            message += "When posting code, format it like this to display it properly:" + Environment.NewLine;
            message += _userService._codeFormattingExample;
            await Context.Message.DeleteAsync();
            await ReplyAsync(message).DeleteAfterSeconds(240);
        }

        [Command("disablecodetips"),
         Summary("Prevents being reminded about using proper code formatting when code is detected. Syntax : !disablecodetips")]
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

        #endregion


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


        [Command("quote"), Summary("Quote a message. Syntax : !quote messageid (#channelname)")]
        private async Task QuoteMessage(ulong id, IMessageChannel channel = null)
        {
            // If channel is null use Context.Channel, else use the provided channel
            channel = channel ?? Context.Channel;

            var message = await channel.GetMessageAsync(id);
            string messageLink = "https://discordapp.com/channels/"+ Context.Guild.Id + "/" + Context.Channel.Id + "/" + Context.Message.Id;

            var builder = new EmbedBuilder()
                .WithColor(new Color(200, 128, 128))
                .WithTimestamp(message.Timestamp)
                .WithFooter(footer =>
                {
                    footer
                        .WithText($"In channel {message.Channel.Name}")                        ;
                                    })
                .WithTitle("Linkback")
                .WithUrl(messageLink)
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
            var codeComplete = Resources.PaizaCodeTemplate.Replace("{code}", string.Join(" ", code));

            var parameters = new Dictionary<string, string> {{"source_code", codeComplete}, {"language", "csharp"}, {"api_key", "guest"}};

            var content = new FormUrlEncodedContent(parameters);

            var message = await ReplyAsync(
                $"Please wait a moment, trying to compile your code interpreted as\n {codeComplete.AsCodeBlock()}");

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
                    fullMessage = message.Content + "The code resulted in a failure.\n";
                    fullMessage += build_stddout.Length > 0 ? build_stddout.AsCodeBlock() : string.Empty;
                    fullMessage += build_stderr.Length > 0 ? build_stderr.AsCodeBlock() : string.Empty;
                }
                else
                {
                    fullMessage = message.Content + "Result : ";
                    fullMessage += stdout.Length > 0 ? stdout.AsCodeBlock() : string.Empty;
                    fullMessage += stderr.Length > 0 ? stderr.AsCodeBlock() : string.Empty;
                }

                httpResponse = await client.PostAsync("https://hastebin.com/documents", new StringContent(fullMessage.Truncate(10000)));
                response = JsonConvert.DeserializeObject<Dictionary<string, string>>(await httpResponse.Content.ReadAsStringAsync());

                newMessage = ($"\nFull result : https://hastebin.com/{response["key"]}\n" + fullMessage).Truncate(1990) + "```";
                await message.ModifyAsync(m => m.Content = newMessage);
            }
        }

        #region Fun

        [Command("slap"), Summary("Slap the specified user(s). Syntax : !slap @user1 [@user2 @user3...]")]
        private async Task SlapUser(params IUser[] users)
        {
            StringBuilder sb = new StringBuilder();
            string[] slaps = {"trout", "duck", "truck"};
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


        [Command("coinflip"), Summary("Flip a coin and see the result. Syntax : !coinflip")]
        [Alias("flipcoin")]
        private async Task CoinFlip()
        {
            Random rand = new Random();
            var coin = new[] {"Heads", "Tails"};

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

        #endregion

        #region Publisher

        [Command("pinfo"), Summary("Information on how to get the publisher role. Syntax : !pinfo")]
        [Alias("publisherinfo")]
        private async Task PublisherInfo()
        {
            if (Context.Channel.Id != _settings.BotCommandsChannel.Id)
            {
                await Task.Delay(1000);
                await Context.Message.DeleteAsync();
                return;
            }

            Random rand = new Random();
            var coin = new[] {"Heads", "Tails"};

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
            if (Context.Channel.Id != _settings.BotCommandsChannel.Id)
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
            if (Context.Channel.Id != _settings.BotCommandsChannel.Id)
            {
                await Task.Delay(1000);
                await Context.Message.DeleteAsync();
                return;
            }

            string verif = await _publisherService.ValidatePackageWithCode(Context.Message.Author, packageId, code);
            await ReplyAsync(verif);
        }

        #endregion

        #region Search

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
                    string title = WebUtility.UrlDecode(row.InnerText);
                    string url = WebUtility.UrlDecode(row.Attributes["href"].Value.Replace("/l/?kh=-1&amp;uddg=", ""));
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

            // Utility function for avoiding evil ads from DuckDuckGo
            bool IsValidResult(HtmlNode node)
            {
                return !node.Attributes["href"].Value.Contains("duckduckgo.com") &&
                       !node.Attributes["href"].Value.Contains("duck.co");
            }
        }

        [Command("manual"), Summary("Searches on Unity3D manual results. Syntax : !manual \"query\"")]
        private async Task SearchManual(params string[] queries)
        {
            // Download Unity3D Documentation Database (lol)

            // Calculate the closest match to the input query
            double minimumScore = double.MaxValue;
            string[] mostSimilarPage = null;
            string[][] pages = await _updateService.GetManualDatabase();
            string query = String.Join(" ", queries);
            foreach (string[] p in pages)
            {
                double curScore = CalculateScore(p[1], query);
                if (curScore < minimumScore)
                {
                    minimumScore = curScore;
                    mostSimilarPage = p;
                }
            }

            // If a page has been found (should be), return the message, else return information
            if (mostSimilarPage != null)
                await ReplyAsync($"** {mostSimilarPage[1]} **\nRead More: https://docs.unity3d.com/Manual/{mostSimilarPage[0]}.html");
            else
                await ReplyAsync("No Results Found.");
        }

        [Command("doc"), Summary("Searches on Unity3D API results. Syntax : !api \"query\"")]
        [Alias("ref", "reference", "api", "docs")]
        private async Task SearchApi(params string[] queries)
        {
            // Download Unity3D Documentation Database (lol)

            // Calculate the closest match to the input query
            double minimumScore = double.MaxValue;
            string[] mostSimilarPage = null;
            string[][] pages = await _updateService.GetApiDatabase();
            string query = String.Join(" ", queries);
            foreach (string[] p in pages)
            {
                double curScore = CalculateScore(p[1], query);
                if (curScore < minimumScore)
                {
                    minimumScore = curScore;
                    mostSimilarPage = p;
                }
            }

            // If a page has been found (should be), return the message, else return information
            if (mostSimilarPage != null)
                await ReplyAsync(
                    $"** {mostSimilarPage[1]} **\nRead More: https://docs.unity3d.com/ScriptReference/{mostSimilarPage[0]}.html");
            else
                await ReplyAsync("No Results Found.");
        }

        private double CalculateScore(string s1, string s2)
        {
            double curScore = 0;
            int i = 0;

            foreach (string q in s1.Split(' '))
            {
                foreach (string x in s2.Split(' '))
                {
                    i++;
                    if (x.Equals(q))
                        curScore -= 50;
                    else
                        curScore += x.CalculateLevenshteinDistance(q);
                }
            }

            curScore /= i;
            return curScore;
        }

        [Command("faq"), Summary("Searches UDH FAQs. Syntax : !faq \"query\"")]
        private async Task SearchFaqs(params string[] queries)
        {
            List<FaqData> faqDataList = _updateService.GetFaqData();

            // Check if query is faq ID (e.g. "!faq 1")
            if (queries.Length == 1 && ParseNumber(queries[0]) > 0)
            {
                int id = ParseNumber(queries[0]) - 1;
                if (id < faqDataList.Count)
                {
                    await ReplyAsync(embed: GetFaqEmbed(id + 1, faqDataList[id]));
                }
                else
                {
                    await ReplyAsync("Invalid FAQ ID selected.");
                }
            }
            // Check if query contains "list" command (i.e. "!faq list")
            else if (queries.Length > 0 && !(queries.Length == 1 && queries[0].Equals("list")))
            {
                // Calculate the closest match to the input query
                double minimumScore = double.MaxValue;
                FaqData mostSimilarFaq = null;
                string query = String.Join(" ", queries);
                int index = 1;
                int mostSimilarIndex = 0;

                // Go through each FAQ in the list and check the most similar
                foreach (FaqData faq in faqDataList)
                {
                    foreach (string keyword in faq.Keywords)
                    {
                        double curScore = CalculateScore(keyword, query);
                        if (curScore < minimumScore)
                        {
                            minimumScore = curScore;
                            mostSimilarFaq = faq;
                            mostSimilarIndex = index;
                        }
                    }

                    index++;
                }

                // If an FAQ has been found (should be), return the FAQ, else return information msg
                if (mostSimilarFaq != null)
                    await ReplyAsync(embed: GetFaqEmbed(mostSimilarIndex, mostSimilarFaq));
                else
                    await ReplyAsync("No FAQs Found.");
            }
            else
            {
                // List all the FAQs available
                ListFaqs(faqDataList);
            }
        }

        private async void ListFaqs(List<FaqData> faqs)
        {
            StringBuilder sb = new StringBuilder(faqs.Count);
            int index = 1;
            foreach (FaqData faq in faqs)
            {
                sb.Append(FormatFaq(index, faq) + "\n");
                string keywords = "[";
                for (int i = 0; i < faq.Keywords.Length; i++)
                {
                    keywords += faq.Keywords[i] + (i < faq.Keywords.Length - 1 ? ", " : "]\n\n");
                }

                index++;
                sb.Append(keywords);
            }

            await ReplyAsync(sb.ToString()).DeleteAfterTime(minutes: 3);
        }

        private Embed GetFaqEmbed(int id, FaqData faq)
        {
            var builder = new EmbedBuilder()
                .WithTitle($"{faq.Question}")
                .WithDescription($"{faq.Answer}")
                .WithColor(new Color(0x33CC00));
            return builder.Build();
        }

        private string FormatFaq(int id, FaqData faq)
        {
            return $"{id}. **{faq.Question}** - {faq.Answer}";
        }

        private int ParseNumber(string s)
        {
            int id;
            if (int.TryParse(s, out id))
            {
                return id;
            }
            else
            {
                return -1;
            }
        }

        #endregion

        #region Birthday

        [Command("birthday"), Summary("Display next member birthday. Syntax : !birthday")]
        [Alias("bday")]
        private async Task Birthday()
        {
            // URL to cell C15/"Next birthday" cell from Corn's google sheet
            string nextBirthday =
                "https://docs.google.com/spreadsheets/d/10iGiKcrBl1fjoBNTzdtjEVYEgOfTveRXdI5cybRTnj4/gviz/tq?tqx=out:html&range=C15:C15";
            HtmlDocument doc = new HtmlWeb().Load(nextBirthday);

            // XPath to the table row
            HtmlNode row = doc.DocumentNode.SelectSingleNode("/html/body/table/tr[2]/td");
            string tableText = row.InnerText;
            string message = $"**{tableText}**";

            await ReplyAsync(message).DeleteAfterTime(minutes: 3);
            await Context.Message.DeleteAfterTime(minutes: 3);
        }

        [Command("birthday"), Summary("Display birthday of mentioned user. Syntax : !birthday @user")]
        [Alias("bday")]
        private async Task Birthday(IUser user)
        {
            string searchName = user.Username;
            // URL to columns B to D of Corn's google sheet
            string birthdayTable =
                "https://docs.google.com/spreadsheets/d/10iGiKcrBl1fjoBNTzdtjEVYEgOfTveRXdI5cybRTnj4/gviz/tq?tqx=out:html&gid=318080247&range=B:D";
            HtmlDocument doc = new HtmlWeb().Load(birthdayTable);
            DateTime birthdate = default(DateTime);

            // XPath to each table row
            foreach (HtmlNode row in doc.DocumentNode.SelectNodes("/html/body/table/tr"))
            {
                // XPath to the name column (C)
                HtmlNode nameNode = row.SelectSingleNode("td[2]");
                string name = nameNode.InnerText;
                if (name.ToLower().Contains(searchName.ToLower()))
                {
                    // XPath to the date column (B)
                    HtmlNode dateNode = row.SelectSingleNode("td[1]");
                    // XPath to the year column (D)
                    HtmlNode yearNode = row.SelectSingleNode("td[3]");

                    CultureInfo provider = CultureInfo.InvariantCulture;
                    string wrongFormat = "M/d/yyyy";
                    //string rightFormat = "dd-MMMM-yyyy";

                    string dateString = dateNode.InnerText;
                    if (!yearNode.InnerText.Contains("&nbsp;"))
                    {
                        dateString = dateString + "/" + yearNode.InnerText;
                    }

                    dateString = dateString.Trim();

                    try
                    {
                        // Converting the birthdate from the wrong format to the right format WITH year
                        birthdate = DateTime.ParseExact(dateString, wrongFormat, provider);
                    }
                    catch (FormatException)
                    {
                        // Converting the birthdate from the wrong format to the right format WITHOUT year
                        birthdate = DateTime.ParseExact(dateString, "M/d", provider);
                    }

                    break;
                }
            }

            if (birthdate == default(DateTime))
            {
                await ReplyAsync(
                        $"Sorry, I couldn't find **{searchName}**'s birthday date. He can add it at https://docs.google.com/forms/d/e/1FAIpQLSfUglZtJ3pyMwhRk5jApYpvqT3EtKmLBXijCXYNwHY-v-lKxQ/viewform ! :stuck_out_tongue_winking_eye: ")
                    .DeleteAfterSeconds(30);
            }
            else
            {
                string message =
                    $"**{searchName}**'s birthdate: __**{birthdate.ToString("dd MMMM yyyy", CultureInfo.InvariantCulture)}**__ " +
                    $"({(int) ((DateTime.Now - birthdate).TotalDays / 365)}yo)";

                await ReplyAsync(message).DeleteAfterTime(minutes: 3);
            }

            await Context.Message.DeleteAfterTime(minutes: 3);
        }

        #endregion

        #region temperatures

        [Command("ftoc"), Summary("Converts a temperature in fahrenheit to celsius. Syntax : !ftoc temperature")]
        private async Task FahrenheitToCelsius(float f)
        {
            await ReplyAsync($"{Context.User.Mention} {f}°F is {Math.Round((f - 32) * 0.555555f, 2)}°C.");
        }

        [Command("ctof"), Summary("Converts a temperature in celsius to fahrenheit. Syntax : !ftoc temperature")]
        private async Task CelsiusToFahrenheit(float c)
        {
            await ReplyAsync($"{Context.User.Mention}  {c}°C is {Math.Round(c * 1.8f + 32, 2)}°F");
        }

        #endregion


        [Command("ping"), Summary("Display bot ping. Syntax : !ping")]
        [Alias("pong")]
        private async Task Ping()
        {
            var message = await ReplyAsync($"Pong :blush:");
            var time = message.Timestamp.Subtract(Context.Message.Timestamp);
            await message.ModifyAsync(m => m.Content = $"Pong :blush: (**{time.TotalMilliseconds}** *ms*)");
            await message.DeleteAfterTime(minutes: 3);
            await Context.Message.DeleteAfterTime(minutes: 3);
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
                if (Context.Channel.Id != _settings.BotCommandsChannel.Id)
                {
                    await Task.Delay(1000);
                    await Context.Message.DeleteAsync();
                    return;
                }

                if (!_settings.AllRoles.Roles.Contains(role.Name))
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
                if (Context.Channel.Id != _settings.BotCommandsChannel.Id)
                {
                    await Task.Delay(1000);
                    await Context.Message.DeleteAsync();
                    return;
                }

                if (!_settings.AllRoles.Roles.Contains(role.Name))
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
                if (Context.Channel.Id != _settings.BotCommandsChannel.Id)
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