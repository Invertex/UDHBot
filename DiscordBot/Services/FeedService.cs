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
        private const string BETA_URL = "https://unity3d.com/unity/beta/latest.xml";
        private const string RELEASE_URL = "https://unity3d.com/unity/releases.xml";
        private const string BLOG_URL = "https://blogs.unity3d.com/feed/";

        private const int MAXIMUM_CHECK = 3;

        private readonly Settings.Deserialized.Settings _settings;
        private readonly DiscordSocketClient _client;


        public FeedService(DiscordSocketClient client, Settings.Deserialized.Settings settings)
        {
            _settings = settings;
            _client = client;
        }

        public async void HandleFeed(FeedData feedData, string url, ulong channelID, ulong roleID, string message)
        {
            try
            {
                HttpWebRequest webRequest = (HttpWebRequest) WebRequest.Create(url);
                HttpWebResponse webResponse = (HttpWebResponse) webRequest.GetResponse();
                Stream dataStream = webResponse.GetResponseStream();
                StreamReader streamReader = new StreamReader(dataStream, Encoding.UTF8);
                string response = streamReader.ReadToEnd();
                streamReader.Close();
                response = Utils.SanitizeXml(response);
                XmlReader reader = new XmlTextReader(new StringReader(response));

                SyndicationFeed feed = SyndicationFeed.Load(reader);
                var channel = _client.GetChannel(channelID) as ISocketMessageChannel;
                var role = _client.GetGuild(_settings.guildId).GetRole(roleID);
                foreach (var item in feed.Items.Take(MAXIMUM_CHECK))
                {
                    if (!feedData.PostedIds.Contains(item.Id))
                    {
                        feedData.PostedIds.Add(item.Id);

                        await role.ModifyAsync(properties => { properties.Mentionable = true; });
                        string messageToSend = string.Format(message, role.Mention, item.Title.Text, item.Links[0].Uri.ToString());

                        await channel.SendMessageAsync(messageToSend);
                        await role.ModifyAsync(properties => { properties.Mentionable = false; });
                    }
                }
            }
            catch (Exception e)
            {
                Console.WriteLine(e);
            }
        }

        public void CheckUnityBetas(FeedData feedData)
        {
            this.HandleFeed(feedData, BETA_URL, _settings.UnityReleasesChannel.Id, _settings.SubsReleasesRoleId, "{0} New unity **beta **release !** {1}** \n <{2}>");
        }

        public void CheckUnityReleases(FeedData feedData)
        {
            this.HandleFeed(feedData, RELEASE_URL, _settings.UnityReleasesChannel.Id, _settings.SubsReleasesRoleId, "{0} New unity release ! **{1}** \n <{2}>");
        }

        public void CheckUnityBlog(FeedData feedData)
        {
            this.HandleFeed(feedData, BLOG_URL, _settings.UnityNewsChannel.Id, _settings.SubsNewsRoleId, "{0} New unity blog post ! **{1}**\n{2}");
        }
    }
}