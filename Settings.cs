using System.Collections.Generic;
using System.Data.SqlClient;
using System.Linq;
using Discord;
using Discord.Commands;
using SixLabors.Fonts;

namespace DiscordBot
{
    public static class Settings
    {
        private static List<string> _unassignableRoles;


        static Settings()
        {
            _unassignableRoles = new List<string>();
            
            _unassignableRoles.Add("Administrator");
            _unassignableRoles.Add("ModSquad");
            _unassignableRoles.Add("Bots");
            _unassignableRoles.Add("Enforcers");
            _unassignableRoles.Add("@everyone");
            _unassignableRoles.Add("Publishers");
            _unassignableRoles.Add("Helpers");
            _unassignableRoles.Add("Mod-Artists");
            _unassignableRoles.Add("Mod-Coders");
            _unassignableRoles.Add("Mod-Helpers");
            _unassignableRoles.Add("Streaming");
            _unassignableRoles.Add("Patreon");
            _unassignableRoles.Add("Patreon-Bot");
            _unassignableRoles.Add("Overwatch");
            _unassignableRoles.Add("UnityOfficial");
            _unassignableRoles.Add("Carbon");
            

        }
        
        public static bool IsRoleAssignable(IRole role)
        {
            return !_unassignableRoles.Contains(role.Name);
        }

        public static IRole GetMutedRole(IGuild guild)
        {
            return guild.Roles.Single(x => x.Name == "Muted");
        }

        public static ulong GetBotAnnouncementChannel()
        {
            return 329629726610030592;
        }

        public static string GetDbConnectionString()
        {
            return "";
        }

        public static string GetServerRootPath()
        {
            return @"";
        }       
    }
}