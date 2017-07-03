using Discord;
using Discord.Commands;
using Discord.WebSocket;
using System;
using System.Linq;
using System.Threading;
using System.Threading.Tasks;

namespace DiscordBot
{
    public class WorkingModule : ModuleBase
    {
        [Command("allUsers"), Summary("Here")]
        [RequireUserPermission(GuildPermission.Administrator)]
        public async Task AddMemberToAll()
        {
            var users = await Context.Guild.GetUsersAsync(CacheMode.AllowDownload);
            int currentIndex = 0;
            int maxCount = users.Count;
            foreach(var u in users)
            {
                IRole role = Context.Guild.Roles.FirstOrDefault(r => r.Name == "Member");
                if (u.RoleIds.Contains(role.Id))
                {
                    maxCount--;
                    continue;
                }
                await u.AddRoleAsync(role);
                Console.WriteLine($"Added user: {u.Username}. ({currentIndex}/{maxCount})");
                currentIndex++;
                Thread.Sleep(200);
            }

            Console.WriteLine($"Added Member to all Users. ({maxCount})");
        }
        [Command("pm"), Summary("Here")]
        [RequireUserPermission(GuildPermission.Administrator)]
        public async Task AddPermission()
        {
            IRole role = Context.Guild.Roles.FirstOrDefault(r => r.Name.Contains("@everyone"));
            OverwritePermissions pm = new OverwritePermissions(
                PermValue.Inherit, PermValue.Inherit, PermValue.Inherit,
                PermValue.Deny, PermValue.Deny, PermValue.Deny,
                PermValue.Inherit, PermValue.Inherit, PermValue.Deny,
                PermValue.Inherit, PermValue.Inherit, PermValue.Inherit,
                PermValue.Inherit, PermValue.Inherit, PermValue.Inherit,
                PermValue.Inherit, PermValue.Inherit, PermValue.Inherit,
                PermValue.Inherit, PermValue.Inherit);
            var v = await Context.Guild.GetTextChannelsAsync();
            foreach (var tC in v)
            {
                SocketChannel tc = await Context.Client.GetChannelAsync(tC.Id) as SocketChannel;
                if (tc is SocketTextChannel)
                {
                    SocketTextChannel tc2 = tc as SocketTextChannel;
                    await tc2.AddPermissionOverwriteAsync(role, pm);
                }
            }
        }
    }
}
