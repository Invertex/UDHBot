using System;
using System.Collections.Generic;
using System.Data.SqlClient;
using System.IO;
using System.Linq;
using Discord;
using Discord.Commands;
using Newtonsoft.Json;
using SixLabors.Fonts;

namespace DiscordBot
{
    public class Rule
    {
        public ulong id;
        public string header;
        public string content;
    }

    public static class Settings
    {
        public static List<string> _assignableRoles;

        private static string _commandList;


        static Settings()
        {
            _assignableRoles = SettingsHandler.LoadValueStringArray("allRoles/roles", JsonFile.Settings).ToList();
        }

        public static bool IsRoleAssignable(IRole role)
        {
            return _assignableRoles.Contains(role.Name, StringComparer.CurrentCultureIgnoreCase);
        }

        public static IRole GetMutedRole(IGuild guild)
        {
            return guild.Roles.Single(x => x.Id == SettingsHandler.LoadValueUlong("mutedRoleID", JsonFile.Settings));
        }

        public static ulong GetBotAnnouncementChannel()
        {
            return SettingsHandler.LoadValueUlong("botAnnouncementChannel/id", JsonFile.Settings);
        }

        public static ulong GetUnityNewsChannel()
        {
            return SettingsHandler.LoadValueUlong("unityNewsChannel/id", JsonFile.Settings);
        }

        public static ulong GetBotCommandsChannel()
        {
            return SettingsHandler.LoadValueUlong("botCommandsChannel/id", JsonFile.Settings);
        }

        public static ulong GetAnimeChannel()
        {
            return SettingsHandler.LoadValueUlong("animeChannel/id", JsonFile.Settings);
        }
        
        public static ulong GetCasinoChannel()
        {
            return SettingsHandler.LoadValueUlong("casinoChannel/id", JsonFile.Settings);
        }

        public static ulong GetMusicCommandsChannel()
        {
            return SettingsHandler.LoadValueUlong("musicCommandsChannel/id", JsonFile.Settings);
        }

        public static string GetServerRootPath()
        {
            return SettingsHandler.LoadValueString("serverRootPath", JsonFile.Settings);
        }

        public static void SetCommandList(string commandList)
        {
            _commandList = commandList;
        }

        public static string GetCommandList()
        {
            return _commandList;
        }

        public static Rule GetRule(ulong ruleId)
        {
            List<Rule> rules = JsonConvert.DeserializeObject<List<Rule>>(SettingsHandler.GetJsonString("Rules"));
            return rules.FirstOrDefault(x => x.id == ruleId);
        }

        public static List<(ulong, string)> GetChannelsHeader()
        {
            return JsonConvert.DeserializeObject<List<Rule>>(SettingsHandler.GetJsonString("Rules")).Select(x => (x.id, x.header)).ToList();
        }
    }
}