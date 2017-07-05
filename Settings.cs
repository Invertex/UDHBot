using System;
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
        private static List<string> _assignableRoles;


        static Settings()
        {
            _assignableRoles = SettingsHandler.LoadValueStringArray("allRoles/roles", JsonFile.Settings).ToList();
        }

        public static bool IsRoleAssignable(IRole role)
        {
            return _assignableRoles.Contains(role.Name);
        }

        public static IRole GetMutedRole(IGuild guild)
        {
            return guild.Roles.Single(x => x.Id == SettingsHandler.LoadValueUlong("mutedRoleID", JsonFile.Settings));
        }

        public static ulong GetBotAnnouncementChannel()
        {
            Console.WriteLine(SettingsHandler.LoadValueUlong("botAnnouncementChannel/id", JsonFile.Settings));
            return SettingsHandler.LoadValueUlong("botAnnouncementChannel/id", JsonFile.Settings);
        }
    }
}