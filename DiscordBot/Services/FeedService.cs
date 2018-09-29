using System;
using System.Linq;
using System.ServiceModel.Syndication;
using System.Xml;
using Discord.WebSocket;

namespace DiscordBot.Services
{
    public class FeedService
    {
        private const string BETA_URL = "https://unity3d.com/fr/unity/beta/latest.xml";
        private const string RELEASE_URL = "https://unity3d.com/fr/unity/releases.xml";
        private const string BLOG_URL = "https://blogs.unity3d.com/feed/";

        private const int MAXIMUM_CHECK = 3;

        private readonly Settings.Deserialized.Settings _settings;
        private readonly DiscordSocketClient _client;


        public FeedService(DiscordSocketClient client, Settings.Deserialized.Settings settings)
        {
            _settings = settings;
            _client = client;
        }

        public async void CheckUnityBetas(FeedData feedData)
        {
            try
            {
                SyndicationFeed feed = SyndicationFeed.Load(XmlReader.Create(BETA_URL));
                var channel = _client.GetChannel(_settings.UnityNewsChannel.Id) as ISocketMessageChannel;

                foreach (var item in feed.Items.Take(MAXIMUM_CHECK))
                {
                    if (!feedData.PostedIds.Contains(item.Id))
                    {
                        feedData.PostedIds.Add(item.Id);

                        string message = $"New unity **beta **release !** {item.Title.Text}** \n <{item.Links[0].Uri.ToString().Replace("/fr/", "/")}>";
                        await channel.SendMessageAsync(message);
                    }
                }
            }
            catch (Exception e)
            {
                Console.WriteLine(e);
            }
        }

        public async void CheckUnityReleases(FeedData feedData)
        {
            try
            {
                SyndicationFeed feed = SyndicationFeed.Load(XmlReader.Create(RELEASE_URL));
                var channel = _client.GetChannel(_settings.UnityNewsChannel.Id) as ISocketMessageChannel;

                foreach (var item in feed.Items.Take(MAXIMUM_CHECK))
                {
                    if (!feedData.PostedIds.Contains(item.Id))
                    {
                        feedData.PostedIds.Add(item.Id);

                        string message = $"New unity release ! **{item.Title.Text}** \n <{item.Links[0].Uri.ToString().Replace("/fr/", "/")}>";
                        await channel.SendMessageAsync(message);
                    }
                }
            }
            catch (Exception e)
            {
                Console.WriteLine(e);
            }
        }

        public async void CheckUnityBlog(FeedData feedData)
        {
            try
            {
                SyndicationFeed feed = SyndicationFeed.Load(XmlReader.Create(BLOG_URL));
                var channel = _client.GetChannel(_settings.UnityNewsChannel.Id) as ISocketMessageChannel;

                foreach (var item in feed.Items.Take(MAXIMUM_CHECK))
                {
                    if (!feedData.PostedIds.Contains(item.Id))
                    {
                        feedData.PostedIds.Add(item.Id);

                        string message = $"New unity blog post ! **{item.Title.Text}**\n{item.Links[0].Uri.ToString().Replace("/fr/", "/")}";
                        await channel.SendMessageAsync(message);
                    }
                }
            }
            catch (Exception e)
            {
                Console.WriteLine(e);
            }
        }
    }
}