using System;
using System.IO;
using System.Threading;
using System.Threading.Tasks;
using Newtonsoft.Json;

namespace DiscordBot
{
    public class BotData
    {
        public DateTime LastPublisherCheck;
        public uint LastPublisherId;
    }

    public class UpdateService
    {
        private readonly LoggingService _logging;
        private readonly PublisherService _publisher;
        private readonly DatabaseService _database;
        private readonly CancellationToken _token;
        private BotData _botData;
        private Random _random;

        public UpdateService(LoggingService logging, PublisherService publisher, DatabaseService database)
        {
            _logging = logging;
            _publisher = publisher;
            _database = database;
            _token = new CancellationToken();
            _random = new Random();

            UpdateLoop();
        }

        private void UpdateLoop()
        {
            ReadDataFromFile();
            SaveDataToFile();
            CheckDailyPublisher(_token);
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
                //await _logging.LogAction("Data successfully saved to file", true, false);
                await Task.Delay(TimeSpan.FromSeconds(20d), _token);
            }
        }

        private async Task CheckDailyPublisher(CancellationToken token)
        {
            await Task.Delay(TimeSpan.FromSeconds(10d));
            while (true)
            {
                if (_botData.LastPublisherCheck < DateTime.Now - TimeSpan.FromDays(1d))
                {
                    uint count = _database.GetPublisherAdCount();
                    int id;
                    do
                    {
                        id = _random.Next((int) count);
                        } while (id == _botData.LastPublisherId);
                    (uint, string) ad = _database.GetPublisherAd((uint) id);
                    await _publisher.PublisherAdvertising(ad.Item1, ad.Item2);
                    
                    await _logging.LogAction("Posted new daily publisher ad.", true, false);
                    _botData.LastPublisherCheck = DateTime.Now;
                }
                await Task.Delay(TimeSpan.FromMinutes(5d), token);
            }
        }
    }
}