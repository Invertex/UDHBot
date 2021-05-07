using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Threading;
using System.Threading.Tasks;
using Discord;
using Discord.WebSocket;
using DiscordBot.Extensions;
using HtmlAgilityPack;
using Newtonsoft.Json;
using Newtonsoft.Json.Linq;

namespace DiscordBot.Services
{
    public class BotData
    {
        public DateTime LastPublisherCheck { get; set; }
        public List<ulong> LastPublisherId { get; set; }
        public DateTime LastUnityDocDatabaseUpdate { get; set; }
    }

    public class UserData
    {
        public UserData()
        {
            MutedUsers = new Dictionary<ulong, DateTime>();
            ThanksReminderCooldown = new Dictionary<ulong, DateTime>();
            CodeReminderCooldown = new Dictionary<ulong, DateTime>();
        }

        public Dictionary<ulong, DateTime> MutedUsers { get; set; }
        public Dictionary<ulong, DateTime> ThanksReminderCooldown { get; set; }
        public Dictionary<ulong, DateTime> CodeReminderCooldown { get; set; }
    }

    public class FaqData
    {
        public string Question { get; set; }
        public string Answer { get; set; }
        public string[] Keywords { get; set; }
    }

    public class FeedData
    {
        public FeedData()
        {
            PostedIds = new List<string>();
        }

        public DateTime LastUnityReleaseCheck { get; set; }
        public DateTime LastUnityBlogCheck { get; set; }
        public List<string> PostedIds { get; set; }
    }

    //TODO Download all avatars to cache them

    public class UpdateService
    {
        private readonly DatabaseService _databaseService;
        private readonly FeedService _feedService;
        private readonly ILoggingService _loggingService;
        private readonly PublisherService _publisherService;
        private readonly Settings.Deserialized.Settings _settings;
        private readonly CancellationToken _token;
        private string[][] _apiDatabase;

        private BotData _botData;
        private readonly DiscordSocketClient _client;
        private List<FaqData> _faqData;
        private FeedData _feedData;

        private string[][] _manualDatabase;
        private readonly Random _random;
        private UserData _userData;

        public UpdateService(DiscordSocketClient client, ILoggingService loggingService, PublisherService publisherService,
                             DatabaseService databaseService, Settings.Deserialized.Settings settings, FeedService feedService)
        {
            _client = client;
            _loggingService = loggingService;
            _publisherService = publisherService;
            _databaseService = databaseService;
            _feedService = feedService;

            _settings = settings;
            _token = new CancellationToken();
            _random = new Random();

            UpdateLoop();
        }

        private async void UpdateLoop()
        {
            ReadDataFromFile();
            await SaveDataToFile();
            //CheckDailyPublisher();
            await UpdateUserRanks();
            await UpdateDocDatabase();
            await UpdateRssFeeds();
        }

        private void ReadDataFromFile()
        {
            if (File.Exists($"{_settings.ServerRootPath}/botdata.json"))
            {
                var json = File.ReadAllText($"{_settings.ServerRootPath}/botdata.json");
                _botData = JsonConvert.DeserializeObject<BotData>(json);
            }
            else
                _botData = new BotData();

            if (File.Exists($"{_settings.ServerRootPath}/userdata.json"))
            {
                var json = File.ReadAllText($"{_settings.ServerRootPath}/userdata.json");
                _userData = JsonConvert.DeserializeObject<UserData>(json);

                Task.Run(
                    async () =>
                    {
                        while (_client.ConnectionState != ConnectionState.Connected || _client.LoginState != LoginState.LoggedIn)
                            await Task.Delay(100, _token);
                        await Task.Delay(10000, _token);
                        //Check if there are users still muted
                        foreach (var userId in _userData.MutedUsers)
                            if (_userData.MutedUsers.HasUser(userId.Key, true))
                            {
                                var guild = _client.Guilds.First(g => g.Id == _settings.GuildId);
                                var sgu = guild.GetUser(userId.Key);
                                if (sgu == null) continue;

                                IGuildUser user = sgu;

                                var mutedRole = user.Guild.GetRole(_settings.MutedRoleId);
                                //Make sure they have the muted role
                                if (!user.RoleIds.Contains(_settings.MutedRoleId)) await user.AddRoleAsync(mutedRole);

                                //Setup delay to remove role when time is up.
                                await Task.Run(async () =>
                                {
                                    await _userData.MutedUsers.AwaitCooldown(user.Id);
                                    await user.RemoveRoleAsync(mutedRole);
                                }, _token);
                            }
                    }, _token);
            }
            else
                _userData = new UserData();

            if (File.Exists($"{_settings.ServerRootPath}/FAQs.json"))
            {
                var json = File.ReadAllText($"{_settings.ServerRootPath}/FAQs.json");
                _faqData = JsonConvert.DeserializeObject<List<FaqData>>(json);
            }
            else
                _faqData = new List<FaqData>();

            if (File.Exists($"{_settings.ServerRootPath}/feeds.json"))
            {
                var json = File.ReadAllText($"{_settings.ServerRootPath}/feeds.json");
                _feedData = JsonConvert.DeserializeObject<FeedData>(json);
            }
            else
                _feedData = new FeedData();
        }

        /*
        ** Save data to file every 20s
        */

        private async Task SaveDataToFile()
        {
            while (true)
            {
                var json = JsonConvert.SerializeObject(_botData);
                await File.WriteAllTextAsync($"{_settings.ServerRootPath}/botdata.json", json, _token);

                json = JsonConvert.SerializeObject(_userData);
                await File.WriteAllTextAsync($"{_settings.ServerRootPath}/userdata.json", json, _token);

                json = JsonConvert.SerializeObject(_feedData);
                await File.WriteAllTextAsync($"{_settings.ServerRootPath}/feeds.json", json, _token);
                //await _logging.LogAction("Data successfully saved to file", true, false);
                await Task.Delay(TimeSpan.FromSeconds(20d), _token);
            }
        }

        public async Task CheckDailyPublisher(bool force = false)
        {
            await Task.Delay(TimeSpan.FromSeconds(10d), _token);
            while (true)
            {
                if (_botData.LastPublisherCheck < DateTime.Now - TimeSpan.FromDays(1d) || force)
                {
                    var count = _databaseService.GetPublisherAdCount();
                    ulong id;
                    uint rand;
                    do
                    {
                        rand = (uint) _random.Next((int) count);
                        id = _databaseService.GetPublisherAd(rand).userId;
                    } while (_botData.LastPublisherId.Contains(id));

                    await _publisherService.PostAd(rand);
                    await _loggingService.LogAction("Posted new daily publisher ad.", true, false);
                    _botData.LastPublisherCheck = DateTime.Now;
                    _botData.LastPublisherId.Add(id);
                }

                if (_botData.LastPublisherId.Count > 10)
                    _botData.LastPublisherId.RemoveAt(0);

                if (force)
                    return;
                await Task.Delay(TimeSpan.FromMinutes(5d), _token);
            }
        }

        private async Task UpdateUserRanks()
        {
            await Task.Delay(TimeSpan.FromSeconds(30d), _token);
            while (true)
            {
                _databaseService.UpdateUserRanks();
                await Task.Delay(TimeSpan.FromMinutes(1d), _token);
            }
        }

        public async Task<string[][]> GetManualDatabase()
        {
            if (_manualDatabase == null)
                await LoadDocDatabase();
            return _manualDatabase;
        }

        public async Task<string[][]> GetApiDatabase()
        {
            if (_apiDatabase == null)
                await LoadDocDatabase();
            return _apiDatabase;
        }

        public List<FaqData> GetFaqData() => _faqData;

        private async Task LoadDocDatabase()
        {
            if (File.Exists($"{_settings.ServerRootPath}/unitymanual.json") &&
                File.Exists($"{_settings.ServerRootPath}/unityapi.json"))
            {
                var json = await File.ReadAllTextAsync($"{_settings.ServerRootPath}/unitymanual.json", _token);
                _manualDatabase = JsonConvert.DeserializeObject<string[][]>(json);
                json = await File.ReadAllTextAsync($"{_settings.ServerRootPath}/unityapi.json", _token);
                _apiDatabase = JsonConvert.DeserializeObject<string[][]>(json);
            }
            else
                await DownloadDocDatabase();
        }

        private async Task DownloadDocDatabase()
        {
            try
            {
                var htmlWeb = new HtmlWeb();
                htmlWeb.CaptureRedirect = true;

                var manual = await htmlWeb.LoadFromWebAsync("https://docs.unity3d.com/Manual/docdata/index.js");
                var manualInput = manual.DocumentNode.OuterHtml;

                var api = await htmlWeb.LoadFromWebAsync("https://docs.unity3d.com/ScriptReference/docdata/index.js");
                var apiInput = api.DocumentNode.OuterHtml;

                _manualDatabase = ConvertJsToArray(manualInput, true);
                _apiDatabase = ConvertJsToArray(apiInput, false);

                File.WriteAllText($"{_settings.ServerRootPath}/unitymanual.json", JsonConvert.SerializeObject(_manualDatabase));
                File.WriteAllText($"{_settings.ServerRootPath}/unityapi.json", JsonConvert.SerializeObject(_apiDatabase));

                string[][] ConvertJsToArray(string data, bool isManual)
                {
                    var list = new List<string[]>();
                    string pagesInput;
                    if (isManual)
                    {
                        pagesInput = data.Split("info = [")[0].Split("pages=")[1];
                        pagesInput = pagesInput.Substring(2, pagesInput.Length - 4);
                    }
                    else
                    {
                        pagesInput = data.Split("info =")[0];
                        pagesInput = pagesInput.Substring(63, pagesInput.Length - 65);
                    }

                    foreach (var s in pagesInput.Split("],["))
                    {
                        var ps = s.Split(",");
                        list.Add(new[] {ps[0].Replace("\"", ""), ps[1].Replace("\"", "")});
                        //Console.WriteLine(ps[0].Replace("\"", "") + "," + ps[1].Replace("\"", ""));
                    }

                    return list.ToArray();
                }
            }
            catch (Exception e)
            {
                Console.WriteLine(e);
            }
        }

        private async Task UpdateDocDatabase()
        {
            while (true)
            {
                if (_botData.LastUnityDocDatabaseUpdate < DateTime.Now - TimeSpan.FromDays(1d))
                    await DownloadDocDatabase();

                await Task.Delay(TimeSpan.FromHours(1), _token);
            }
        }

        private async Task UpdateRssFeeds()
        {
            await Task.Delay(TimeSpan.FromSeconds(30d), _token);
            while (true)
            {
                if (_feedData != null)
                {
                    if (_feedData.LastUnityReleaseCheck < DateTime.Now - TimeSpan.FromMinutes(5))
                    {
                        _feedData.LastUnityReleaseCheck = DateTime.Now;

                        await _feedService.CheckUnityBetasAsync(_feedData);
                        await _feedService.CheckUnityReleasesAsync(_feedData);
                    }

                    if (_feedData.LastUnityBlogCheck < DateTime.Now - TimeSpan.FromMinutes(10))
                    {
                        _feedData.LastUnityBlogCheck = DateTime.Now;

                        await _feedService.CheckUnityBlogAsync(_feedData);
                    }
                }

                await Task.Delay(TimeSpan.FromSeconds(30d), _token);
            }
        }

        public async Task<(string name, string extract, string url)> DownloadWikipediaArticle(string searchQuery)
        {
            var wikiSearchUri = Uri.EscapeUriString(_settings.WikipediaSearchPage + searchQuery);
            var htmlWeb = new HtmlWeb {CaptureRedirect = true};
            HtmlDocument wikiSearchResponse;

            try
            {
                wikiSearchResponse = await htmlWeb.LoadFromWebAsync(wikiSearchUri, _token);
            }
            catch
            {
                Console.WriteLine("Wikipedia method failed loading URL: " + wikiSearchUri);
                return (null, null, null);
            }

            try
            {
                var job = JObject.Parse(wikiSearchResponse.Text);

                if (job.TryGetValue("query", out var query))
                {
                    var pages = JsonConvert.DeserializeObject<List<WikiPage>>(job[query.Path]["pages"].ToString(), new JsonSerializerSettings {NullValueHandling = NullValueHandling.Ignore});

                    if (pages != null && pages.Count > 0)
                    {
                        pages.Sort((x, y) => x.Index.CompareTo(y.Index)); //Sort from smallest index to biggest, smallest index is indicitive of best matching result
                        var page = pages[0];

                        const string referToString = "may refer to:...";
                        var referToIndex = page.Extract.IndexOf(referToString);
                        //If a multi-refer result was given, reformat title to indicate this and strip the "may refer to" portion from the body
                        if (referToIndex > 0)
                        {
                            var splitIndex = referToIndex + referToString.Length;
                            page.Title = page.Extract.Substring(0, splitIndex - 4); //-4 to strip the useless characters since this will be a title
                            page.Extract = page.Extract.Substring(splitIndex);
                            page.Extract.Replace("\n", Environment.NewLine + "-");
                        }
                        else
                            page.Extract = page.Extract.Replace("\n", Environment.NewLine);

                        return (page.Title + ":", page.Extract, page.FullUrl.ToString());
                    }
                }
                else
                    return (null, null, null);
            }
            catch (Exception e)
            {
                Console.WriteLine(e);
                Console.WriteLine("Wikipedia method likely failed to parse JSON response from: " + wikiSearchUri);
            }

            return (null, null, null);
        }

        public UserData GetUserData() => _userData;

        public void SetUserData(UserData data)
        {
            _userData = data;
        }

        /// <summary>
        ///     JSON object for the Wikipedia command to convert results to.
        /// </summary>
        private class WikiPage
        {
            [JsonProperty("index")]
            public long Index { get; set; }

            [JsonProperty("title")]
            public string Title { get; set; }

            [JsonProperty("extract")]
            public string Extract { get; set; }

            [JsonProperty("fullurl")]
            public Uri FullUrl { get; set; }
        }
    }
}