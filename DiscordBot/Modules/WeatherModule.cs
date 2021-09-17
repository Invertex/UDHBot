using System;
using System.Collections.Generic;
using System.Threading.Tasks;
using Discord;
using Discord.Commands;
using DiscordBot.Extensions;
using DiscordBot.Utils;
using Newtonsoft.Json;

namespace DiscordBot.Modules
{
    public class WeatherModule : ModuleBase
    {
        #region Weather Results
#pragma warning disable 0649
        // ReSharper disable InconsistentNaming
        public class Coord
        {
            public double Lon { get; set; }
            public double Lat { get; set; }
        }

        public class Weather
        {
            public int id { get; set; }
            [JsonProperty("main")] public string Name { get; set; }
            public string Description { get; set; }
            public string Icon { get; set; }
        }

        public class Main
        {
            public float Temp { get; set; }
            [JsonProperty("feels_like")] public double Feels { get; set; }
            [JsonProperty("temp_min")] public double Min { get; set; }
            [JsonProperty("temp_max")] public double Max { get; set; }
            public int Pressure { get; set; }
            public int Humidity { get; set; }
        }

        public class Wind
        {
            public double Speed { get; set; }
            public int Deg { get; set; }
        }

        public class Clouds
        {
            public int all { get; set; }
        }

        public class Sys
        {
            public int type { get; set; }
            public int id { get; set; }
            public double message { get; set; }
            public string country { get; set; }
            public int sunrise { get; set; }
            public int sunset { get; set; }
        }

        public class Result
        {
            public Coord coord { get; set; }
            public List<Weather> weather { get; set; }
            public string @base { get; set; }
            public Main main { get; set; }
            public int visibility { get; set; }
            public Wind wind { get; set; }
            public Clouds clouds { get; set; }
            public int dt { get; set; }
            public Sys sys { get; set; }
            public int timezone { get; set; }
            public int id { get; set; }
            public string name { get; set; }
            public int cod { get; set; }
        }
        // ReSharper restore InconsistentNaming
#pragma warning restore 0649

        #endregion
        
        [Command("Temperature")]
        [Summary("Attempts to provide the temperature of the city provided.")]
        [Alias("temp")]
        public async Task WeatherTemperature(params string[] city)
        {
            string cityName = string.Join(" ", city);
            Result res = await GetWeather(city: cityName);
            
            // We could show the age of the data, but prob isn't worth it.
            // var dataAge = DateTime.UtcNow - DateTimeOffset.FromUnixTimeSeconds(res.dt);

            if (res == null)
            {
                await ReplyAsync("API Returned no results.");
                return;
            }
            
            EmbedBuilder builder = new EmbedBuilder()
                .WithTitle($"{res.name} Temperature")
                .AddField($"Currently: **{Math.Round(res.main.Temp, 1)}°C** [Feels like {Math.Round(res.main.Feels, 1)}°C]",
                    $"Humidity: {res.main.Humidity}% | Weather: {res.weather[0].Name} (*{res.weather[0].Description}*)")
                .WithThumbnailUrl($"https://openweathermap.org/img/wn/{res.weather[0].Icon}@2x.png")
                .WithFooter($"Requested by {Context.User}");

            await ReplyAsync(embed: builder.Build());
            
            await Context.Message.DeleteAfterSeconds(seconds: 60f);
        }

        // https://openweathermap.org/current#call
        [Command("Weather")]
        [Summary("Attempts to provide the weather of the city provided.")]
        public async Task WeatherWeather(params string[] city)
        {
            string cityName = string.Join(" ", city);
            Result res = await GetWeather(city: cityName);
            
            // We could show the age of the data, but prob isn't worth it.
            // var dataAge = DateTime.UtcNow - DateTimeOffset.FromUnixTimeSeconds(res.dt);

            if (res == null)
            {
                await ReplyAsync("API Returned no results.");
                return;
            }
            
            EmbedBuilder builder = new EmbedBuilder()
                .WithTitle($"{res.name} Weather")
                .AddField($"Weather: **{Math.Round(res.main.Temp, 1)}°C** with {res.weather[0].Description}",
                    $"Min: **{Math.Round(res.main.Min, 1)}°C** | Max: **{Math.Round(res.main.Max, 1)}°C**\n" +
                    $"Wind Speed: {Math.Round((res.wind.Speed*60f*60f) / 1000f, 2)} km/h, Cloud cover: {res.clouds.all}%")
                .WithThumbnailUrl($"https://openweathermap.org/img/wn/{res.weather[0].Icon}@2x.png")
                .WithFooter($"Requested by {Context.User}");

            await ReplyAsync(embed: builder.Build());
            
            await Context.Message.DeleteAfterSeconds(seconds: 60f);
        }

        public async Task<Result> GetWeather(string city, string countryCode = "",
            string unit = "metric")
        {
            var api = "b091072fdf96759332a05b63d06d1d44";
            var query =
                $"https://api.openweathermap.org/data/2.5/weather?q={city}{countryCode}&appid={api}&units={unit}";

            var result = await InternetExtensions.GetHttpContents(query);

            return JsonConvert.DeserializeObject<Result>(result);
        }
    }
}