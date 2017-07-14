using System;
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
        
        public UserModule(LoggingService logging, DatabaseService database, UserService user)
        {
            _logging = logging;
            _database = database;
            _user = user;
        }

        [Command("help"), Summary("Display available commands (this). Syntax : !help")]
        async Task DisplayHelp()
        {
            await ReplyAsync(Settings.GetCommandList());
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
        }

        [Command("profile"), Summary("Display current user profile card. Syntax : !profile")]
        async Task DisplayProfile()
        {
            ulong id;
            string username;
            id = Context.Message.Author.Id;
            username = Context.Message.Author.Username;

            var xp = _database.GetUserXp(id);
            var karma = _database.GetUserKarma(id);
            var rank = _database.GetUserRank(id);


            await Context.Channel.SendFileAsync(await _user.GenerateProfileCard(Context.Message.Author));
            //await ReplyAsync($"{username} has {xp} xp and {karma} karma which  makes him #{rank}");
        }

        [Command("profile"), Summary("Display profile card of mentionned user. Syntax : !profile @user")]
        async Task DisplayProfile(IUser user)
        {
            ulong id;
            string username;
            id = user.Id;
            username = user.Username;

            var xp = _database.GetUserXp(id);
            var karma = _database.GetUserKarma(id);
            var rank = _database.GetUserRank(id);


            await Context.Channel.SendFileAsync(await _user.GenerateProfileCard(user));
            //await ReplyAsync($"{username} has {xp} xp and {karma} karma which  makes him #{rank}");
        }

        [Command("quote"), Summary("Quote a message. Syntax : !quote #channelname messageid")]
        async Task QuoteMessage(IMessageChannel channel, ulong id)
        {
            /*/
            TODO: TO FIX : only work for messages posted while the bot was up
            */
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
            await Context.Message.DeleteAsync();
        }

        [Command("coinflip"), Summary("Flip a coin and see the result. Syntax : !coinflip")]
        [Alias("flipcoin")]
        async Task CoinFlip()
        {
            Random rand = new Random();
            var coin = new[] {"Heads", "Tails"};

            await ReplyAsync($"**{Context.User.Username}** flipped a coin and got **{coin[rand.Next() % 2]}** !");
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
            [RequireUserPermission(GuildPermission.ManageRoles)]
            async Task AddRoleUser(IRole role)
            {
                if (!Settings.IsRoleAssignable(role))
                {
                    await ReplyAsync("This role is not assigneable");
                    return;
                }
                var u = Context.User as IGuildUser;

                u.AddRoleAsync(role);
                await ReplyAsync($"{u.Username} you now have the role of `{role.Name}`");
                _logging.LogAction($"{Context.User.Username} has added role {role} to himself in {Context.Channel.Name}");
            }

            [Command("remove"), Summary("Remove a role from yourself. Syntax : !role remove role")]
            [Alias("delete")]
            async Task RemoveRoleUser(IRole role)
            {
                if (!Settings.IsRoleAssignable(role))
                {
                    await ReplyAsync("Role is not assigneable");
                    return;
                }

                var u = Context.User as IGuildUser;

                u.RemoveRoleAsync(role);
                await ReplyAsync($"{u.Username} your role of `{role.Name}` has been removed");
                _logging.LogAction($"{Context.User.Username} has removed role {role} from himself in {Context.Channel.Name}");
            }

            [Command("list"), Summary("Display the list of roles. Syntax : !role list")]
            async Task ListRole()
            {
                await ReplyAsync("**The following roles are available on this server** :\n" +
                                 "\n" +
                                 "We offer multiple roles to show what you specialize in, so if you are particularly good at anything, assign your role! \n" +
                                 "You can have multiple specialties and your color is determined by the highest role you hold \n" +
                                 "\n" +
                                 "```Publishers role can be assigned only with verification by executing **" +
                                 "!publisher XXXX** digits from Unity Asset Store Publisher Page.\n" +
                                 "You will receive a message on the email **Support E-mail** that's provided, if there's no email provided the verification will fail, contact a moderator for verification. \n" +
                                 "In the email is a verification code that you must type it in chat **" +
                                 "!publisher verify XXXX** code that's provided in email. \n" +
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
                                 "!role add/remove Helpers - A friend and helper of all those who seek to live in the spirit..\n" +
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