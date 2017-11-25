using System;
using System.Collections.Generic;
using System.Net.Http;
using System.Threading.Tasks;
using ConsoleApplication;
using Discord.WebSocket;
using DiscordBot.Extensions;
using Newtonsoft.Json;

namespace DiscordBot
{
    public class AnimeData
    {
        public DateTime LastDailyAnimeAiringList;
        public DateTime LastWeeklyAnimeAiringList;
        public Dictionary<int, List<ulong>> Subscribers;
    }
    
    public class AnimeService
    {
        private DiscordSocketClient _client;

        public AnimeService(DiscordSocketClient client)
        {
            _client = client;
        }
        
        public async void PublishDailyAnime()
        {
            Console.WriteLine("Publish daily anime");
           
            var channel = _client.GetChannel(Settings.GetAnimeChannel()) as ISocketMessageChannel;

            var airingAnimes = await GetAiringAnimes(1);

            string reply = "Yee-oh ! This is 2B with your Daily Anime Schedule !\n Here's what will be airing in the next 24h !\n";
            
            foreach (var anime in airingAnimes.data.Page.airingSchedules)
            {
                string malUrl = $"https://myanimelist.net/anime/{anime.media.idMal}";
                TimeSpan timeUntilAiring = TimeSpan.FromSeconds((double) anime.timeUntilAiring);
                var secondTitle = anime.media.title.english ?? anime.media.title.native;
                string daysString = timeUntilAiring.Days > 0 ? timeUntilAiring.Days + "d " : "";
                daysString += timeUntilAiring.Hours > 0 ? timeUntilAiring.Hours + "h " : "";
                daysString += timeUntilAiring.Minutes > 0 ? timeUntilAiring.Minutes + "min" : "";

                string episodeCount = anime.episode + (anime.media.episodes != null ? "/" + anime.media.episodes : "");
                
                reply += $"**{anime.media.title.romaji}** ({secondTitle}) - *Next airing episode* : **{episodeCount}** " +
                         $"in *{daysString}* "+ // at *{DateTime.Now + timeUntilAiring}*(UTC+1) " +
                         $"<{malUrl}>\n";
            }

            var str = reply.MessageSplit(1990);

            foreach (var message in str)
            {
                await channel.SendMessageAsync(message);
            }
        }

        public async void PublishWeeklyAnime()
        {
            Console.WriteLine("Publish weekly anime");
           
            var channel = _client.GetChannel(Settings.GetAnimeChannel()) as ISocketMessageChannel;

            var airingAnimes = await GetAiringAnimes(7);

            string reply = "こんにちは! This is 2B with your Weekly Anime Schedule !\n Here's what will be airing in the next 7 days !\n";
            
            foreach (var anime in airingAnimes.data.Page.airingSchedules)
            {
                string malUrl = $"https://myanimelist.net/anime/{anime.media.idMal}";
                TimeSpan timeUntilAiring = TimeSpan.FromSeconds((double) anime.timeUntilAiring);
                var secondTitle = anime.media.title.english ?? anime.media.title.native;
                string daysString = timeUntilAiring.Days > 0 ? timeUntilAiring.Days + "d " : "";
                daysString += timeUntilAiring.Hours > 0 ? timeUntilAiring.Hours + "h " : "";
                daysString += timeUntilAiring.Minutes > 0 ? timeUntilAiring.Minutes + "min" : "";

                string episodeCount = anime.episode + (anime.media.episodes != null ? "/" + anime.media.episodes : "");
                
                reply += $"**{anime.media.title.romaji}** ({secondTitle}) - *Next airing episode* : **{episodeCount}** " +
                         $"in *{daysString}* "+ // at *{DateTime.Now + timeUntilAiring}*(UTC+1) " +
                         $"<{malUrl}>\n";
            }

            var str = reply.MessageSplit(1990);

            foreach (var message in str)
            {
                await channel.SendMessageAsync(message);
            }
        }
        
        public async Task<AnilistResponse> GetAiringAnimes(int days)
        {
            int timeUntilAiring = 60 * 60 * 24 * days;
            
            var json = @"{
                Page(page: 1, perPage: 50) {
                  airingSchedules(notYetAired: true, airingAt_greater: 604800, sort: TIME_DESC) {
                    id
                    timeUntilAiring
                    episode
                    media {
                      title {
                        romaji
                        native
                        english
                      }
                      episodes
                      idMal
                      genres
                    }
                  }
                }
              }
              ";

            var response = await AnilistRequest(json, "");

            response.data.Page.airingSchedules.RemoveAll(x => x.timeUntilAiring > timeUntilAiring);

            return response;
        }

        public async Task<AnilistResponse> AnilistRequest(string payload, string variables)
        {
            string uri = "https://graphql.anilist.co";
            var parameters = new Dictionary<string, string>
            {
                {"query", payload},
                {"variables", variables}
            };
            var content = new FormUrlEncodedContent(parameters);
            string response;
            AnilistResponse anilistResponse;

            using (HttpClient client = new HttpClient())
            {
                var request = await client.PostAsync(uri, content);
                response = await request.Content.ReadAsStringAsync();
                anilistResponse = JsonConvert.DeserializeObject<AnilistResponse>(response);
            }

            return anilistResponse;
        }
    }
}