using System;
using System.Collections.Generic;
using System.IO;
using System.Threading;
using System.Threading.Tasks;
using Newtonsoft.Json;
using System.Linq;
using Discord;
using Discord.WebSocket;
using DiscordBot.Extensions;

namespace DiscordBot
{
    public class BotData
    {
        public DateTime LastPublisherCheck { get; set; }
        public List<ulong> LastPublisherId { get; set; }
    }

    public class UserData
    {
        public Dictionary<ulong, DateTime> MutedUsers { get; set; }
        public Dictionary<ulong, DateTime> ThanksReminderCooldown { get; set; }
        public Dictionary<ulong, DateTime> CodeReminderCooldown { get; set; }

        public UserData()
        {
            MutedUsers = new Dictionary<ulong, DateTime>();
            ThanksReminderCooldown = new Dictionary<ulong, DateTime>();
            CodeReminderCooldown = new Dictionary<ulong, DateTime>();
        }
    }

    public class CasinoData
    {
        public int SlotMachineCashPool { get; set; }
        public int LotteryCashPool { get; set; }
    }

    //TODO: Download all avatars to cache them

    public class UpdateService
    {
        DiscordSocketClient _client;
        private readonly LoggingService _loggingService;
        private readonly PublisherService _publisherService;
        private readonly DatabaseService _databaseService;
        public readonly UserService _userService;
        private readonly AnimeService _animeService;
        private readonly CancellationToken _token;
        private BotData _botData;
        private Random _random;
        private AnimeData _animeData;
        private UserData _userData;
        private CasinoData _casinoData;

        public UpdateService(DiscordSocketClient client, LoggingService loggingService, PublisherService publisherService,
            DatabaseService databaseService, UserService userService, AnimeService animeService)
        {
            _client = client;
            _loggingService = loggingService;
            _publisherService = publisherService;
            _databaseService = databaseService;
            _userService = userService;
            _animeService = animeService;
            _token = new CancellationToken();
            _random = new Random();

            UpdateLoop();
        }

        private void UpdateLoop()
        {
            ReadDataFromFile();
            SaveDataToFile();
            //CheckDailyPublisher();
            UpdateUserRanks();
            UpdateAnime();
        }

        private void ReadDataFromFile()
        {
            if (File.Exists($"{Settings.GetServerRootPath()}/botdata.json"))
            {
                string json = File.ReadAllText($"{Settings.GetServerRootPath()}/botdata.json");
                _botData = JsonConvert.DeserializeObject<BotData>(json);
            }
            else
                _botData = new BotData();

            if (File.Exists($"{Settings.GetServerRootPath()}/animedata.json"))
            {
                string json = File.ReadAllText($"{Settings.GetServerRootPath()}/animedata.json");
                _animeData = JsonConvert.DeserializeObject<AnimeData>(json);
            }
            else
                _animeData = new AnimeData();

            if (File.Exists($"{Settings.GetServerRootPath()}/userdata.json"))
            {
                string json = File.ReadAllText($"{Settings.GetServerRootPath()}/userdata.json");
                _userData = JsonConvert.DeserializeObject<UserData>(json);

                Task.Run(
                    async () =>
                    {
                        
                        while (_client.ConnectionState != ConnectionState.Connected || _client.LoginState != LoginState.LoggedIn)
                            await Task.Delay(100);
                        await Task.Delay(1000);
                        //Check if there are users still muted
                        foreach (var userID in _userData.MutedUsers)
                        {
                            if (_userData.MutedUsers.HasUser(userID.Key, false))
                            {
                                SocketGuild guild = _client.Guilds.First();
                                IGuildUser user = guild.GetUser(userID.Key) as IGuildUser;

                                IRole mutedRole = Settings.GetMutedRole(user.Guild);
                                //Make sure they have the muted role
                                if (!user.RoleIds.Contains(mutedRole.Id))
                                {
                                    user.AddRoleAsync(mutedRole);
                                }

                                /*if (_userData.MutedUsers[userID.Key].Date < DateTime.Now)
                                {
                                    await user.RemoveRoleAsync(mutedRole);
                                    _userData.MutedUsers.Remove(userID.Key);
                                    return;
                                }*/
                                
                                //Setup delay to remove role when time is up.
                                Task.Run(async () =>
                                {
                                    await Task.Delay(_userData.MutedUsers.Seconds(userID.Key) * 1000);
                                    await user.RemoveRoleAsync(mutedRole);
                                    _userData.MutedUsers.Remove(userID.Key);
                                });
                            }
                        }
                    });
            }
            else
            {
                _userData = new UserData();
            }

            if (File.Exists($"{Settings.GetServerRootPath()}/casinodata.json"))
            {
                string json = File.ReadAllText($"{Settings.GetServerRootPath()}/casinodata.json");
                _casinoData = JsonConvert.DeserializeObject<CasinoData>(json);
            }
            else
                _casinoData = new CasinoData();
        }

        /*
        ** Save data to file every 20s
        */

        private async Task SaveDataToFile()
        {
            while (true)
            {
                var json = JsonConvert.SerializeObject(_botData);
                File.WriteAllText($"{Settings.GetServerRootPath()}/botdata.json", json);

                json = JsonConvert.SerializeObject(_animeData);
                File.WriteAllText($"{Settings.GetServerRootPath()}/animedata.json", json);

                json = JsonConvert.SerializeObject(_userData);
                File.WriteAllText($"{Settings.GetServerRootPath()}/userdata.json", json);

                json = JsonConvert.SerializeObject(_casinoData);
                File.WriteAllText($"{Settings.GetServerRootPath()}/casinodata.json", json);
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
                    uint count = _databaseService.GetPublisherAdCount();
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

        private async Task UpdateAnime()
        {
            await Task.Delay(TimeSpan.FromSeconds(30d), _token);
            while (true)
            {
                if (_animeData.LastDailyAnimeAiringList < DateTime.Now - TimeSpan.FromDays(1d))
                {
                    _animeService.PublishDailyAnime();
                    _animeData.LastDailyAnimeAiringList = DateTime.Now;
                }

                if (_animeData.LastWeeklyAnimeAiringList < DateTime.Now - TimeSpan.FromDays(7d))
                {
                    _animeService.PublishWeeklyAnime();
                    _animeData.LastWeeklyAnimeAiringList = DateTime.Now;
                }

                await Task.Delay(TimeSpan.FromMinutes(1d), _token);
            }
        }

        public UserData GetUserData()
        {
            return _userData;
        }

        public void SetUserData(UserData data)
        {
            _userData = data;
        }

        public CasinoData GetCasinoData()
        {
            return _casinoData;
        }

        public void SetCasinoData(CasinoData data)
        {
            _casinoData = data;
        }
    }
}