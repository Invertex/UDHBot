using System;
using System.Collections.Generic;
using System.Net.Http;
using System.Threading.Tasks;
using Discord.WebSocket;
using DiscordBot.Data;
using DiscordBot.Extensions;
using Newtonsoft.Json;

namespace DiscordBot.Services
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
        private ILoggingService _loggingService;

        private readonly Settings.Deserialized.Settings _settings;

        public AnimeService(DiscordSocketClient client, ILoggingService loggingService, Settings.Deserialized.Settings settings)
        {
            _client = client;
            _loggingService = loggingService;
            _settings = settings;
        }

        public async void PublishDailyAnime()
        {
            Console.WriteLine("Publish daily anime");

            var channel = _client.GetChannel(_settings.AnimeChannel.Id) as ISocketMessageChannel;

            var airingAnimes = await GetAiringAnimes(1);

            string reply = "Yee-oh ! This is 2B with your Daily Anime Schedule !\n Here's what will be airing in the next 24h !\n";

            foreach (var anime in airingAnimes.data.Page.airingSchedules)
            {
                string malUrl = $"https://myanimelist.net/anime/{anime.media.idMal}";
                if (anime.timeUntilAiring != null) {
                    TimeSpan timeUntilAiring = TimeSpan.FromSeconds((double) anime.timeUntilAiring);
                    var secondTitle = anime.media.title.english ?? anime.media.title.native;
                    string daysString = timeUntilAiring.Days > 0 ? timeUntilAiring.Days + "d " : "";
                    daysString += timeUntilAiring.Hours > 0 ? timeUntilAiring.Hours + "h " : "";
                    daysString += timeUntilAiring.Minutes > 0 ? timeUntilAiring.Minutes + "min" : "";

                    string episodeCount = anime.episode + (anime.media.episodes != null ? "/" + anime.media.episodes : "");

                    reply += $"**{anime.media.title.romaji}** ({secondTitle}) - *Next airing episode* : **{episodeCount}** " +
                             $"in *{daysString}* " + // at *{DateTime.Now + timeUntilAiring}*(UTC+1) " +
                             $"<{malUrl}>\n";
                }
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

            var channel = _client.GetChannel(_settings.AnimeChannel.Id) as ISocketMessageChannel;

            string reply = "こんにちは! This is 2B with your Weekly Anime Schedule !\n Here's what will be airing in the next 7 days !\n";

            try
            {
                for (int i = 1; i < 10; i++)
                {
                    var airingAnimes = await GetAiringAnimes(7, i);

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
                                 $"in *{daysString}* " + // at *{DateTime.Now + timeUntilAiring}*(UTC+1) " +
                                 $"<{malUrl}>\n";
                    }
                }

                var str = reply.MessageSplit(1990);

                foreach (var message in str)
                {
                    await channel.SendMessageAsync(message);
                }
            }
            catch (Exception e)
            {
                Console.WriteLine(e);
                await _loggingService.LogAction(e.ToString());
                //throw;
            }
        }

        public async Task<AnilistResponse> SearchAnime(string title)
        {
            var json = @"{
              Page(page: 1, perPage: 50) {
                media(type:ANIME, search:" + "\"" + title + "\"" + @")
                       {
                          id,
                          idMal,
                          title{romaji},
                          description,
                          coverImage{
                            medium,        
                          },
                          startDate {
                            year
                            month
                            day
                          },
                          endDate {
                            year
                            month
                            day
                          },
                          genres,
                          title{
                            romaji,
                            native,
                            english
                          }
                        }
                      }
                    }";

            var response = await AnilistRequest(json, "");

            return response;
        }

        public async Task<AnilistResponse> GetAiringAnimes(int days, int page = 1)
        {
            int timeUntilAiring = 60 * 60 * 24 * days;

            var json = @"{
                Page(page: " + page + @", perPage: 50) {
                  airingSchedules(notYetAired: true, airingAt_greater: 604800, sort: TIME) {
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
                        startDate {
                                  year
                                  month
                                  day
                       }
                       endDate {
                                  year
                                  month
                                  day
                       }
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
                anilistResponse = JsonConvert.DeserializeObject<AnilistResponse>(response,
                    new JsonSerializerSettings
                    {
                        NullValueHandling = NullValueHandling.Ignore,
                        MissingMemberHandling = MissingMemberHandling.Ignore
                    });
            }

            return anilistResponse;
        }
    }
}