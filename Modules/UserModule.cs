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
        
        [Command("help"), Summary("Display help for an user")]
        async Task DisplayHelp(IUser user, int arg)
        {
            //TODO: To implement
        }    
        
        [Command("slap"), Summary("Slap the specified user(s)")]
        async Task SlapUser(params IUser[] users)
        {
            StringBuilder sb = new StringBuilder();
            string[] slaps = {"trout", "duck", "truck"};
            var random = new Random();

            sb.Append($"{Context.User.Username} Slaps ");
            foreach (var user in users)
            {
                sb.Append($"{user.Mention} ");
            }
            sb.Append($"around a bit with a large {slaps[random.Next() % 3]}");

            await Context.Channel.SendMessageAsync(sb.ToString());
        }

        [Command("xp"), Summary("Display xp for user")]
        async Task DisplayXP(IUser user)
        {
            var xp = _database.GetUserXp(user.Id);
            await ReplyAsync($"{user.Username} has {xp} xp.");
        }

        [Command("profile"), Summary("Display profile card of user")]
        async Task DisplayProfile(IUser user = null)
        {
            ulong id;
            string username;
            if (user == null)
            {
                id = Context.Client.CurrentUser.Id;
                username = Context.User.Username;
            }
            else
            {
                id = user.Id;
                username = user.Username;
            }

            var xp = _database.GetUserXp(id);
            var karma = _database.GetUserKarma(id);
            var rank = _database.GetUserRank(id);


            await Context.Channel.SendFileAsync(await _user.GenerateProfileCard(user));
            await ReplyAsync($"{username} has {xp} xp and {karma} karma which  makes him #{rank}");
        }

        [Command("quote"), Summary("Quote a message")]
        async Task QuoteMessage(IUserMessage message)
        {
            /*/
            TODO: TO FIX : only work for messages posted while the bot was up
            */
            var builder = new EmbedBuilder()
                    .WithColor(new Color(200, 128, 128))
                    .WithTimestamp(message.Timestamp)
                    .WithFooter(footer => {
                        footer
                            .WithText($"In channel {message.Channel.Name}");
                    })
                .WithAuthor(author => {
                    author
                        .WithName($"{message.Author.Username}")
                        .WithIconUrl(message.Author.GetAvatarUrl());
                })
                .AddField("Original message", message.Content);
            var embed = builder.Build();
            await ReplyAsync("", false, embed);
            await Context.Message.DeleteAsync();
        }

        [Command("coinflip"), Summary("Flip a coin and see the result")]
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
            
            [Command("add"), Summary("Add a role to current user")]
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

            [Command("remove"), Summary("Remove a role from this user")]
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
        }
        
    }
}