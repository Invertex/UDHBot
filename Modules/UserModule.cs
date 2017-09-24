using System;
using System.Collections.Generic;
using System.Text;
using System.Threading.Tasks;
using Discord;
using Discord.Commands;

namespace DiscordBot
{
    public class UserModule : ModuleBase
    {
        private readonly LoggingService _logging;
        private readonly DatabaseService _database;
        private readonly UserService _user;
        private readonly PublisherService _publisher;

        public UserModule(LoggingService logging, DatabaseService database, UserService user, PublisherService publisher)
        {
            _logging = logging;
            _database = database;
            _user = user;
            _publisher = publisher;
        }

        [Command("help"), Summary("Display available commands (this). Syntax : !help")]
        async Task DisplayHelp()
        {
            //TODO: Be possible in DM
            if (Context.Channel.Id != Settings.GetBotCommandsChannel())
            {
                await Task.Delay(1000);
                await Context.Message.DeleteAsync();
                return;
            }

            await ReplyAsync(Settings.GetCommandList());
        }

        [Command("rules"), Summary("Get the of the current channel by DM. Syntax : !rules")]
        async Task Rules()
        {
            Rules(Context.Channel);
            await Context.Message.DeleteAsync();
        }

        [Command("rules"), Summary("Get the rules of the mentionned channel by DM. !rules #channel")]
        [Alias("rule")]
        async Task Rules(IMessageChannel channel)
        {
            Rule rule = Settings.GetRule(channel.Id);
            IUserMessage m;
            IDMChannel dm = await Context.User.GetOrCreateDMChannelAsync();
            if (rule == null)
                await dm.SendMessageAsync(
                    "There is no special rule for this channel.\nPlease follow global rules (you can get them by typing `!globalrules`)");
            else
                await dm.SendMessageAsync(
                    $"{rule.header}{(rule.content.Length > 0 ? rule.content : "There is no special rule for this channel.\nPlease follow global rules (you can get them by typing `!globalrules`)")}");

            Task deleteAsync = Context.Message?.DeleteAsync();
            if (deleteAsync != null) await deleteAsync;
        }

        [Command("globalrules"), Summary("Get the Global Rules by DM. Syntax : !globalrules")]
        async Task GlobalRules(int seconds = 60)
        {
            string globalRules = Settings.GetRule(0).content;
            IDMChannel dm = await Context.User.GetOrCreateDMChannelAsync();
            await dm.SendMessageAsync(globalRules);
            await Context.Message.DeleteAsync();
        }

        [Command("channels"), Summary("Get description of the channels by DM. Syntax : !channels")]
        async Task ChannelsDescription()
        {
            //Display rules of this channel for x seconds
            List<(ulong, string)> headers = Settings.GetChannelsHeader();
            StringBuilder sb = new StringBuilder();
            foreach (var h in headers)
                sb.Append($"{(await Context.Guild.GetTextChannelAsync(h.Item1))?.Mention} - {h.Item2}\n");
            string text = sb.ToString();

            IDMChannel dm = await Context.User.GetOrCreateDMChannelAsync();

            if (sb.ToString().Length > 2000)
            {
                await dm.SendMessageAsync(text.Substring(0, 2000));
                await dm.SendMessageAsync(text.Substring(2000, text.Length));
            }
            else
            {
                await dm.SendMessageAsync(text);
            }

            await Context.Message.DeleteAsync();
        }

        [Command("slap"), Summary("Slap the specified user(s). Syntax : !slap @user1 [@user2 @user3...]")]
        async Task SlapUser(params IUser[] users)
        {
            StringBuilder sb = new StringBuilder();
            string[] slaps = {"trout", "duck", "truck"};
            var random = new Random();

            sb.Append($"**{Context.User.Username}** Slaps ");
            foreach (var user in users)
            {
                sb.Append($"{user.Mention} ");
            }
            sb.Append($"around a bit with a large {slaps[random.Next() % 3]}");

            await Context.Channel.SendMessageAsync(sb.ToString());
            await Task.Delay(1000);
            await Context.Message.DeleteAsync();
        }

        [Command("profile"), Summary("Display current user profile card. Syntax : !profile")]
        async Task DisplayProfile()
        {
            IUserMessage profile = await Context.Channel.SendFileAsync(await _user.GenerateProfileCard(Context.Message.Author));

            await Task.Delay(10000);
            await Context.Message.DeleteAsync();
            await Task.Delay(TimeSpan.FromMinutes(1d));
            await profile.DeleteAsync();
        }

        [Command("profile"), Summary("Display profile card of mentionned user. Syntax : !profile @user")]
        async Task DisplayProfile(IUser user)
        {
            IUserMessage profile = await Context.Channel.SendFileAsync(await _user.GenerateProfileCard(user));

            await Task.Delay(1000);
            await Context.Message.DeleteAsync();
            await Task.Delay(TimeSpan.FromMinutes(1d));
            await profile.DeleteAsync();
        }

        [Command("quote"), Summary("Quote a message in current channel. Syntax : !quote messageid")]
        async Task QuoteMessage(ulong id)
        {
        await Context.Message.DeleteAsync();
            IMessageChannel channel = Context.Channel;
            var message = await channel.GetMessageAsync(id);
            Console.WriteLine($"message {message.Author.Username}  {message.Channel.Name}");
            var builder = new EmbedBuilder()
                .WithColor(new Color(200, 128, 128))
                .WithTimestamp(message.Timestamp)
                .WithFooter(footer =>
                {
                    footer
                        .WithText($"In channel {message.Channel.Name}");
                })
                .WithAuthor(author =>
                {
                    author
                        .WithName($"{message.Author.Username}")
                        .WithIconUrl(message.Author.GetAvatarUrl());
                })
                .AddField("Original message", message.Content);
            var embed = builder.Build();
            await ReplyAsync("", false, embed);            
        }

        [Command("quote"), Summary("Quote a message. Syntax : !quote #channelname messageid")]
        async Task QuoteMessage(IMessageChannel channel, ulong id)
        {
            var message = await channel.GetMessageAsync(id);
            var builder = new EmbedBuilder()
                .WithColor(new Color(200, 128, 128))
                .WithTimestamp(message.Timestamp)
                .WithFooter(footer =>
                {
                    footer
                        .WithText($"In channel {message.Channel.Name}");
                })
                .WithAuthor(author =>
                {
                    author
                        .WithName($"{message.Author.Username}")
                        .WithIconUrl(message.Author.GetAvatarUrl());
                })
                .AddField("Original message", message.Content);
            var embed = builder.Build();
            await ReplyAsync("", false, embed);
            await Task.Delay(1000);
            await Context.Message.DeleteAsync();
        }

        [Command("coinflip"), Summary("Flip a coin and see the result. Syntax : !coinflip")]
        [Alias("flipcoin")]
        async Task CoinFlip()
        {
            Random rand = new Random();
            var coin = new[] {"Heads", "Tails"};

            await ReplyAsync($"**{Context.User.Username}** flipped a coin and got **{coin[rand.Next() % 2]}** !");
            await Task.Delay(1000);
            await Context.Message.DeleteAsync();
        }

        [Command("pinfo"), Summary("Information on how to get the publisher role. Syntax : !pinfo")]
        [Alias("publisherinfo")]
        async Task PublisherInfo()
        {
            if (Context.Channel.Id != Settings.GetBotCommandsChannel())
            {
                await Task.Delay(1000);
                await Context.Message.DeleteAsync();
                return;
            }

            Random rand = new Random();
            var coin = new[] {"Heads", "Tails"};

            await ReplyAsync($"\n" +
                             "**Publisher - BOT COMMANDS : ** ``these commands are not case-sensitive.``\n" +
                             "``!pkg ID`` - To add your package to Publisher everyday Advertising , ID means the digits on your package link.\n" +
                             "``!verify packageId verifCode`` - Verify your package with the code send to your email.");

            await Task.Delay(10000);
            await Context.Message.DeleteAsync();
        }

        [Command("pkg"), Summary("Add your published package to the daily advertising. Syntax : !pkg packageId")]
        [Alias("package")]
        async Task Package(uint packageId)
        {
            if (Context.Channel.Id != Settings.GetBotCommandsChannel())
            {
                await Task.Delay(1000);
                await Context.Message.DeleteAsync();
                return;
            }

            (bool, string) verif = await _publisher.VerifyPackage(packageId);
            await ReplyAsync(verif.Item2);
        }

        [Command("verify"), Summary("Verify a package with the code received by email. Syntax : !verify packageId code")]
        async Task VerifyPackage(uint packageId, string code)
        {
            if (Context.Channel.Id != Settings.GetBotCommandsChannel())
            {
                await Task.Delay(1000);
                await Context.Message.DeleteAsync();
                return;
            }

            string verif = await _publisher.ValidatePackageWithCode(Context.Message.Author, packageId, code);
            await ReplyAsync(verif);
        }

        [Group("role")]
        public class RoleModule : ModuleBase
        {
            private readonly LoggingService _logging;

            public RoleModule(LoggingService logging)
            {
                _logging = logging;
            }

            [Command("add"), Summary("Add a role to yourself. Syntax : !role add role")]
            async Task AddRoleUser(IRole role)
            {
                if (Context.Channel.Id != Settings.GetBotCommandsChannel())
                {
                    await Task.Delay(1000);
                    await Context.Message.DeleteAsync();
                    return;
                }

                if (!Settings.IsRoleAssignable(role))
                {
                    await ReplyAsync("This role is not assigneable");
                    return;
                }
                var u = Context.User as IGuildUser;

                await u.AddRoleAsync(role);
                await ReplyAsync($"{u.Username} you now have the role of `{role.Name}`");
                await _logging.LogAction($"{Context.User.Username} has added role {role} to himself in {Context.Channel.Name}");
            }

            [Command("remove"), Summary("Remove a role from yourself. Syntax : !role remove role")]
            [Alias("delete")]
            async Task RemoveRoleUser(IRole role)
            {
                if (Context.Channel.Id != Settings.GetBotCommandsChannel())
                {
                    await Task.Delay(1000);
                    await Context.Message.DeleteAsync();
                    return;
                }

                if (!Settings.IsRoleAssignable(role))
                {
                    await ReplyAsync("Role is not assigneable");
                    return;
                }

                var u = Context.User as IGuildUser;

                await u.RemoveRoleAsync(role);
                await ReplyAsync($"{u.Username} your role of `{role.Name}` has been removed");
                await _logging.LogAction($"{Context.User.Username} has removed role {role} from himself in {Context.Channel.Name}");
            }

            [Command("list"), Summary("Display the list of roles. Syntax : !role list")]
            async Task ListRole()
            {
                if (Context.Channel.Id != Settings.GetBotCommandsChannel())
                {
                    await Task.Delay(1000);
                    await Context.Message.DeleteAsync();
                    return;
                }

                await ReplyAsync("**The following roles are available on this server** :\n" +
                                 "\n" +
                                 "We offer multiple roles to show what you specialize in, so if you are particularly good at anything, assign your role! \n" +
                                 "You can have multiple specialties and your color is determined by the highest role you hold \n" +
                                 "\n" +
                                 "```To get the publisher role type **!pinfo** and follow the instructions." +
                                 "https://www.assetstore.unity3d.com/en/#!/search/page=1/sortby=popularity/query=publisher:7285 <= Example Digits```\n");
                await ReplyAsync("```!role add/remove Artists - The Graphic Designers, Artists and Modellers. \n" +
                                 "!role add/remove 3DModelers - People behind every vertex. \n" +
                                 "!role add/remove Coders - The valiant knights of programming who toil away, without rest. \n" +
                                 "!role add/remove C# - If you are using C# to program in Unity3D \n" +
                                 "!role add/remove Javascript - If you are using Javascript to program in Unity3D \n" +
                                 "!role add/remove Game-Designers - Those who specialise in writing, gameplay design and level design.\n" +
                                 "!role add/remove Audio-Artists - The unsung heroes of sound effects .\n" +
                                 "!role add/remove Generalists - Generalist may refer to a person with a wide array of knowledge.\n" +
                                 "!role add/remove Hobbyists - A person who is interested in Unity3D or Game Making as a hobby.\n" +
                                 "!role add/remove Vector-Artists - The people who love to have infinite resolution.\n" +
                                 "!role add/remove Voxel-Artist - People who love to voxelize the world.\n" +
                                 "!role add/remove Students - The eager learners among us, never stop learning. \n" +
                                 "!role add/remove VR-Developers - Passionate people who wants to bridge virtual world with real life. \n" +
                                 "--------------------------------------------------------------------------------------------\n" +
                                 "!role add/remove Streamer - If you stream on twitch/youtube or other discord integrated platforms content about tutorials and gaming. \n" +
                                 "```");
            }
        }
    }
}