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
    [RequireAdmin]
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
            var embed = BuildEmbedFromUrl(attachment.Url);
            
            await ReplyAsync(embed: embed);
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

            if (!IsValidHost(uriResult.Host))
            {
                await ReplyAsync($"{Context.User.Mention}, supported URLs: [https://hastebin.com, https://pastebin.com, https://gdl.space, https://hastepaste.com, http://pastie.org].").DeleteAfterSeconds(5);
                return;
            }
            string download_url = GetDownUrlFromUri(uriResult);
            
            var embed = BuildEmbedFromUrl(download_url);
            await ReplyAsync(embed: embed);
        }
        
        private readonly IEmote _thumbUpEmote = new Emoji("👍");
        [Command("send embed"), Summary("Generate an embed from an URL and posts it to a channel, or edits a message if ID passed in and replaces with new embed.")]
        public async Task EmbedToChannel(string url, IMessageChannel channel, ulong messageId = 0)
        {
            Uri uriResult;
            bool result = Uri.TryCreate(url, UriKind.Absolute, out uriResult)
                          && (uriResult.Scheme == Uri.UriSchemeHttp || uriResult.Scheme == Uri.UriSchemeHttps);
            if (!result)
            {
                await ReplyAsync($"{Context.User.Mention}, the parameter is not a valid URL.").DeleteAfterSeconds(5);
                return;
            }
            
            if (!IsValidHost(uriResult.Host))
            {
                await ReplyAsync($"{Context.User.Mention}, supported URLs: [https://hastebin.com, https://pastebin.com, https://gdl.space, https://hastepaste.com, http://pastie.org].").DeleteAfterSeconds(5);
                return;
            }
            string download_url = GetDownUrlFromUri(uriResult);
            
            var embed = BuildEmbedFromUrl(download_url);
            await ReplyAsync(embed: embed);
            if (embed.Length <= 0)
            {
                await ReplyAsync("Embed is improperly formatted or corrupt.");
                return;
            }
            
            // Confirm with user it is correct
            var tempEmbed = await ReplyAsync(embed: embed);
            var message = await ReplyAsync("If correct, react to this message within 20 seconds to continue.");
            await message.AddReactionAsync(_thumbUpEmote);
            // 20 seconds wait?
            bool confirmedEmbed = false;
            for (int i = 0; i < 10; i++)
            {
                await Task.Delay(2000);
                var reactions = await message.GetReactionUsersAsync(_thumbUpEmote, 10).FlattenAsync();
                if (reactions.Count() > 1)
                {
                    foreach (var reaction in reactions)
                    {
                        if (reaction.Id == Context.User.Id)
                        {
                            confirmedEmbed = true;
                            break;
                        }
                    }
                }
                i++;
            }
            await tempEmbed.DeleteAsync();
            await message.DeleteAsync();
            // If no reaction, we assume it was bad and abort
            if (!confirmedEmbed)
            {
                await ReplyAsync("Reaction not detected, embed aborted.").DeleteAfterSeconds(seconds: 5);
                return;
            }
            
            if (messageId != 0)
            {
                var messageToEdit = await channel.GetMessageAsync(messageId) as IUserMessage;
                if (messageToEdit == null)
                {
                    await ReplyAsync("Bot doesn't own the message passed in").DeleteAfterSeconds(5);
                    return;
                }
                // Modify the old message, we clear any text it might have had.
                await messageToEdit.ModifyAsync(x =>
                {
                    x.Content = "";
                    x.Embed = embed;
                });
                await ReplyAsync("Message replaced!").DeleteAfterSeconds(5);
            }
            else
            {
                await channel.SendMessageAsync(embed: embed);
                await ReplyAsync("Embed Posted!").DeleteAfterSeconds(5);
            }
        }
        
        [Command("send embed"), Summary("Generate an embed from an URL and posts it to a channel, or edits a message if ID passed in and replaces with new embed.")]
        public async Task EmbedToChannel(IMessageChannel channel, ulong messageId = 0)
        {
            if (Context.Message.Attachments.Count < 1)
            {
                await ReplyAsync($"{Context.User.Mention}, you must provide a JSON file or a JSON url.").DeleteAfterSeconds(5);
                return;
            }
            var attachment = Context.Message.Attachments.ElementAt(0);
            var embed = BuildEmbedFromUrl(attachment.Url);
            await ReplyAsync(embed: embed);
            if (embed.Length <= 0)
            {
                await ReplyAsync("Embed is improperly formatted or corrupt.");
                return;
            }
            
            // Confirm with user it is correct
            var tempEmbed = await ReplyAsync(embed: embed);
            var message = await ReplyAsync("If correct, react to this message within 20 seconds to continue.");
            await message.AddReactionAsync(_thumbUpEmote);
            // 20 seconds wait?
            bool confirmedEmbed = false;
            for (int i = 0; i < 10; i++)
            {
                await Task.Delay(2000);
                var reactions = await message.GetReactionUsersAsync(_thumbUpEmote, 10).FlattenAsync();
                if (reactions.Count() > 1)
                {
                    foreach (var reaction in reactions)
                    {
                        if (reaction.Id == Context.User.Id)
                        {
                            confirmedEmbed = true;
                            break;
                        }
                    }
                }
                i++;
            }
            await tempEmbed.DeleteAsync();
            await message.DeleteAsync();
            // If no reaction, we assume it was bad and abort
            if (!confirmedEmbed)
            {
                await ReplyAsync("Reaction not detected, embed aborted.").DeleteAfterSeconds(seconds: 5);
                return;
            }
            
            if (messageId != 0)
            {
                var messageToEdit = await channel.GetMessageAsync(messageId) as IUserMessage;
                if (messageToEdit == null)
                {
                    await ReplyAsync("Bot doesn't own the message passed in").DeleteAfterSeconds(5);
                    return;
                }
                // Modify the old message, we clear any text it might have had.
                await messageToEdit.ModifyAsync(x =>
                {
                    x.Content = "";
                    x.Embed = embed;
                });
                await ReplyAsync("Message replaced!").DeleteAfterSeconds(5);
            }
            else
            {
                await channel.SendMessageAsync(embed: embed);
                await ReplyAsync("Embed Posted!").DeleteAfterSeconds(5);
            }
        }

        private Discord.Embed BuildEmbedFromUrl(string url)
        {
            WebClient webClient = new WebClient();
            byte[] buffer = webClient.DownloadData(url);
            webClient.Dispose();
            string json = Encoding.UTF8.GetString(buffer);

            return BuildEmbed(json);
        }

        private bool IsValidHost(string url)
        {
            switch (url)
            {
                case "hastebin.com":
                case "gdl.space":
                case "hastepaste.com":
                case "pastebin.com":
                case "pastie.org":
                    return true;
                default:
                    return false;
            }
        }

        private string GetDownUrlFromUri(Uri uri)
        {
            switch (uri.Host)
            {
                case "hastebin.com":
                case "gdl.space":
                    return $"https://{uri.Host}/raw{uri.AbsolutePath}";
                case "hastepaste.com":
                    return $"https://hastepaste.com/raw{uri.AbsolutePath.Substring(5)}";
                case "pastebin.com":
                    return $"https://pastebin.com/raw{uri.AbsolutePath}";
                case "pastie.org":
                    return $"{uri.OriginalString}/raw";
            }
            return string.Empty;
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
