using System;
using System.IO;
using System.Linq;
using System.Net;
using System.ServiceModel.Syndication;
using System.Text;
using System.Xml;
using Discord;
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
                HttpWebRequest webRequest = (HttpWebRequest) WebRequest.Create(BETA_URL);
                HttpWebResponse webResponse = (HttpWebResponse) webRequest.GetResponse();
                Stream dataStream = webResponse.GetResponseStream();
                StreamReader streamReader = new StreamReader(dataStream, Encoding.UTF8);
                string response = streamReader.ReadToEnd();
                streamReader.Close();
                response = Utils.SanitizeXml(response);
                XmlReader reader = new XmlTextReader(new StringReader(response));

                SyndicationFeed feed = SyndicationFeed.Load(reader);
                var channel = _client.GetChannel(_settings.UnityNewsChannel.Id) as ISocketMessageChannel;
                var role = _client.GetGuild(_settings.guildId).GetRole(_settings.SubsReleasesRoleId);
                foreach (var item in feed.Items.Take(MAXIMUM_CHECK))
                {
                    if (!feedData.PostedIds.Contains(item.Id))
                    {
                        feedData.PostedIds.Add(item.Id);

                        await role.ModifyAsync(properties => { properties.Mentionable = true; });
                        string message =
                            $"{role.Mention} New unity **beta **release !** {item.Title.Text}** \n <{item.Links[0].Uri.ToString().Replace("/fr/", "/")}>";

                        await channel.SendMessageAsync(message);
                        await role.ModifyAsync(properties => { properties.Mentionable = false; });
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
                HttpWebRequest webRequest = (HttpWebRequest) WebRequest.Create(RELEASE_URL);
                HttpWebResponse webResponse = (HttpWebResponse) webRequest.GetResponse();
                Stream dataStream = webResponse.GetResponseStream();
                StreamReader streamReader = new StreamReader(dataStream, Encoding.UTF8);
                string response = streamReader.ReadToEnd();
                streamReader.Close();
                response = Utils.SanitizeXml(response);
                XmlReader reader = new XmlTextReader(new StringReader(response));

                SyndicationFeed feed = SyndicationFeed.Load(reader);
                var channel = _client.GetChannel(_settings.UnityNewsChannel.Id) as ISocketMessageChannel;
                var role = _client.GetGuild(_settings.guildId).GetRole(_settings.SubsReleasesRoleId);

                foreach (var item in feed.Items.Take(MAXIMUM_CHECK))
                {
                    if (!feedData.PostedIds.Contains(item.Id))
                    {
                        feedData.PostedIds.Add(item.Id);

                        await role.ModifyAsync(properties => { properties.Mentionable = true; });
                        string message =
                            $"{role.Mention} New unity release ! **{item.Title.Text}** \n <{item.Links[0].Uri.ToString().Replace("/fr/", "/")}>";

                        await channel.SendMessageAsync(message);
                        await role.ModifyAsync(properties => { properties.Mentionable = false; });
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
                HttpWebRequest webRequest = (HttpWebRequest) WebRequest.Create(BLOG_URL);
                HttpWebResponse webResponse = (HttpWebResponse) webRequest.GetResponse();
                Stream dataStream = webResponse.GetResponseStream();
                StreamReader streamReader = new StreamReader(dataStream, Encoding.UTF8);
                string response = streamReader.ReadToEnd();
                streamReader.Close();
                response = Utils.SanitizeXml(response);
                XmlReader reader = new XmlTextReader(new StringReader(response));

                SyndicationFeed feed = SyndicationFeed.Load(reader);
                var channel = _client.GetChannel(_settings.UnityNewsChannel.Id) as ISocketMessageChannel;
                var role = _client.GetGuild(_settings.guildId).GetRole(_settings.SubsNewsRoleId);
                foreach (var item in feed.Items.Take(MAXIMUM_CHECK))
                {
                    if (!feedData.PostedIds.Contains(item.Id))
                    {
                        feedData.PostedIds.Add(item.Id);

                        await role.ModifyAsync(properties => { properties.Mentionable = true; });
                        string message =
                            $"{role.Mention} New unity blog post ! **{item.Title.Text}**\n{item.Links[0].Uri.ToString().Replace("/fr/", "/")}";

                        await channel.SendMessageAsync(message);
                        await role.ModifyAsync(properties => { properties.Mentionable = false; });
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