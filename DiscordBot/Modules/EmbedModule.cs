using System;
using System.Linq;
using System.Net;
using System.Text;
using System.Threading.Tasks;
using Discord;
using Discord.Commands;
using DiscordBot.Extensions;
using Newtonsoft.Json;

// ReSharper disable all UnusedMember.Local
namespace DiscordBot.Modules
{
    public class EmbedModule : ModuleBase
    {

#pragma warning disable 0649
        private class Embed
        {
            public class Footer
            {
                public string icon_url;
                public string text;
            }

            public class Thumbnail
            {
                public string url;
            }

            public class Image
            {
                public string url;
            }

            public class Author
            {
                public string name;
                public string url;
                public string icon_url;
            }

            public class Field
            {
                public string name;
                public string value;
                public bool? inline;
            }

            public string title;
            public string description;
            public string url;
            public uint? color;
            public DateTimeOffset? timestamp;
            public Footer footer;
            public Thumbnail thumbnail;
            public Image image;
            public Author author;
            public Field[] fields;
        }
#pragma warning restore 0649

        /// <summary>
        /// Generate an embed
        /// </summary>
        [RequireAdmin]
        [Command("embed"), Summary("Generate an embed.")]
        public async Task EmbedCommand()
        {
            await Context.Message.DeleteAsync();

            if (Context.Message.Attachments.Count < 1)
            {
                await ReplyAsync($"{Context.User.Mention}, you must provide a JSON file or a JSON url.").DeleteAfterSeconds(5);
                return;
            }

            var attachment = Context.Message.Attachments.ElementAt(0);

            WebClient webClient = new WebClient();
            byte[] buffer = webClient.DownloadData(attachment.Url);
            webClient.Dispose();
            string json = Encoding.UTF8.GetString(buffer);

            await ReplyAsync(embed: BuildEmbed(json));
        }

        [Command("embed"), Summary("Generate an embed from an URL (hastebin).")]
        public async Task EmbedCommand(string url)
        {
            await Context.Message.DeleteAsync();

            Uri uriResult;
            bool result = Uri.TryCreate(url, UriKind.Absolute, out uriResult)
                && (uriResult.Scheme == Uri.UriSchemeHttp || uriResult.Scheme == Uri.UriSchemeHttps);

            if (!result)
            {
                await ReplyAsync($"{Context.User.Mention}, the parameter is not a valid URL.").DeleteAfterSeconds(5);
                return;
            }

            string download_url;
            switch (uriResult.Host)
            {
                case "hastebin.com":
                case "gdl.space":
                    download_url = $"https://{uriResult.Host}/raw{uriResult.AbsolutePath}";
                    break;
                case "hastepaste.com":
                    download_url = $"https://hastepaste.com/raw{uriResult.AbsolutePath.Substring(5)}";
                    break;
                case "pastebin.com":
                    download_url = $"https://pastebin.com/raw{uriResult.AbsolutePath}";
                    break;
                case "pastie.org":
                    download_url = $"{url}/raw";
                    break;
                default:
                    await ReplyAsync($"{Context.User.Mention}, only those URLs are supported: [https://hastebin.com, https://pastebin.com, https://gdl.space, https://hastepaste.com, http://pastie.org].").DeleteAfterSeconds(5);
                    return;
            }

            Console.WriteLine($"Downloading JSON from {download_url}");
            WebClient webClient = new WebClient();
            byte[] buffer = webClient.DownloadData(download_url);
            webClient.Dispose();
            string json = Encoding.UTF8.GetString(buffer);

            await ReplyAsync(embed: BuildEmbed(json));
        }

        private Discord.Embed BuildEmbed(string json)
        {
            try
            {
                var embed_data = JsonConvert.DeserializeObject<Embed>(json);
                var embedBuilder = new EmbedBuilder();
                if (!String.IsNullOrEmpty(embed_data.title)) embedBuilder.Title = embed_data.title;
                if (!String.IsNullOrEmpty(embed_data.description)) embedBuilder.Description = embed_data.description;
                if (!String.IsNullOrEmpty(embed_data.url)) embedBuilder.Url = embed_data.url;
                if (embed_data.color.HasValue) embedBuilder.Color = new Color(embed_data.color.Value);
                if (embed_data.timestamp.HasValue) embedBuilder.Timestamp = embed_data.timestamp.Value;

                if (embed_data.footer != null)
                {
                    embedBuilder.Footer = new EmbedFooterBuilder();
                    if (!String.IsNullOrEmpty(embed_data.footer.icon_url)) embedBuilder.Footer.IconUrl = embed_data.footer.icon_url;
                    if (!String.IsNullOrEmpty(embed_data.footer.text)) embedBuilder.Footer.Text = embed_data.footer.text;
                }

                if (embed_data.thumbnail != null && !String.IsNullOrEmpty(embed_data.thumbnail.url)) embedBuilder.ThumbnailUrl = embed_data.thumbnail.url;
                if (embed_data.image != null && !String.IsNullOrEmpty(embed_data.image.url)) embedBuilder.ImageUrl = embed_data.image.url;

                if (embed_data.author != null)
                {
                    embedBuilder.Author = new EmbedAuthorBuilder();
                    if (!String.IsNullOrEmpty(embed_data.author.icon_url)) embedBuilder.Author.IconUrl = embed_data.author.icon_url;
                    if (!String.IsNullOrEmpty(embed_data.author.name)) embedBuilder.Author.Name = embed_data.author.name;
                    if (!String.IsNullOrEmpty(embed_data.author.url)) embedBuilder.Author.Url = embed_data.author.url;
                }

                if (embed_data.fields != null)
                {
                    foreach (var field in embed_data.fields)
                    {
                        var f = new EmbedFieldBuilder();
                        if (!String.IsNullOrEmpty(field.name)) f.Name = field.name;
                        if (!String.IsNullOrEmpty(field.value)) f.Value = field.value;
                        if (field.inline.HasValue) f.IsInline = field.inline.Value;
                        embedBuilder.AddField(f);
                    }
                }

                return embedBuilder.Build();
            }
            catch (Exception e)
            {
                Console.Error.WriteLine(e);
                ReplyAsync($"{Context.User.Mention}, the provided JSON is invalid.").DeleteAfterSeconds(5);
            }

            return null;
        }

    }
}