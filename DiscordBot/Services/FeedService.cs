using System;
using System.IO;
using System.Linq;
using System.Net;
using System.ServiceModel.Syndication;
using System.Text;
using System.Threading.Tasks;
using System.Xml;
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

        public async Task HandleFeed(FeedData feedData, string url, ulong channelID, ulong? roleID, string message)
        {
            try
            {
                HttpWebRequest webRequest = (HttpWebRequest)WebRequest.Create(url);
                HttpWebResponse webResponse = (HttpWebResponse)webRequest.GetResponse();
                Stream dataStream = webResponse.GetResponseStream();
                StreamReader streamReader = new StreamReader(dataStream, Encoding.UTF8);
                string response = streamReader.ReadToEnd();
                streamReader.Close();
                response = Utils.SanitizeXml(response);
                XmlReader reader = new XmlTextReader(new StringReader(response));

                SyndicationFeed feed = SyndicationFeed.Load(reader);
                var channel = _client.GetChannel(channelID) as ISocketMessageChannel;
                foreach (var item in feed.Items.Take(MAXIMUM_CHECK))
                {
                    if (!feedData.PostedIds.Contains(item.Id))
                    {
                        feedData.PostedIds.Add(item.Id);

                        string messageToSend = string.Format(message, item.Title.Text, item.Links[0].Uri.ToString());

                        var role = _client.GetGuild(_settings.guildId).GetRole(roleID ?? 0);
                        bool wasRoleMentionable = false;
                        if (role != null)
                        {
                            wasRoleMentionable = role.IsMentionable;
                            await role.ModifyAsync(properties => { properties.Mentionable = true; });
                            messageToSend = $"{role.Mention} {messageToSend}";
                        }

                        var postedMessage = await channel.SendMessageAsync(messageToSend);
                        if (channel is SocketNewsChannel) await postedMessage.CrosspostAsync();

                        if (role != null) await role.ModifyAsync(properties => { properties.Mentionable = wasRoleMentionable; });
                    }
                }
            }
            catch (Exception e)
            {
                Console.WriteLine(e);
            }
        }

        public async Task CheckUnityBetasAsync(FeedData feedData)
        {
            await this.HandleFeed(feedData, BETA_URL, _settings.UnityReleasesChannel.Id, _settings.SubsReleasesRoleId, "New unity **beta **release !** {0}** \n <{1}>");
        }

        public async Task CheckUnityReleasesAsync(FeedData feedData)
        {
            await this.HandleFeed(feedData, RELEASE_URL, _settings.UnityReleasesChannel.Id, _settings.SubsReleasesRoleId, "New unity release ! **{0}** \n <{1}>");
        }

        public async Task CheckUnityBlogAsync(FeedData feedData)
        {
            await this.HandleFeed(feedData, BLOG_URL, _settings.UnityNewsChannel.Id, _settings.SubsNewsRoleId, "New unity blog post ! **{0}**\n{1}");
        }
    }
}