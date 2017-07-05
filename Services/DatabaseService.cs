using System.Xml.Linq;
using System;
using System.Data.SqlClient;
using System.Runtime.InteropServices.ComTypes;
using System.Threading.Tasks;
using MySql.Data.MySqlClient;

namespace DiscordBot
{
    public class DatabaseService
    {
        private string _connection { get; }

        public DatabaseService()
        {
            _connection = SettingsHandler.LoadValueString("dbConnectionString", JsonFile.Settings);
        }

        public void AddUserXp(ulong id, uint xp)
        {
            uint oldXp;
            string reader = GetAttributeFromUser(id, "exp");

            oldXp = Convert.ToUInt32(reader);
            UpdateAttributeFromUser(id, "exp", oldXp + xp);
        }
        
        public void AddUserLevel(ulong id, uint level)
        {
            uint oldLevel;
            string reader = GetAttributeFromUser(id, "level");

            oldLevel = Convert.ToUInt32(reader);
            UpdateAttributeFromUser(id, "level", oldLevel + level);
        }

        public void AddUserKarma(ulong id, uint karma)
        {
            uint oldKarma;
            string reader = GetAttributeFromUser(id, "karma");

            oldKarma = Convert.ToUInt32(reader);
            UpdateAttributeFromUser(id,  "karma", oldKarma + karma);
        }
        
        public uint GetUserXp(ulong id)
        {
            uint xp;
            string reader = GetAttributeFromUser(id, "exp");

            xp = System.Convert.ToUInt32(reader);

            return xp;
        }
        
        public uint GetUserKarma(ulong id)
        {
            uint karma = 0;
            string reader = GetAttributeFromUser(id, "karma");

            karma = System.Convert.ToUInt32(reader);

            return karma;
        }
        
        public uint GetUserRank(ulong id)
        {
            uint rank = 0;
            string reader = GetAttributeFromUser(id, "rank");

            rank = System.Convert.ToUInt32(reader);

            return rank;
        }
        
        public uint GetUserLevel(ulong id)
        {
            uint level = 0;
            string reader = GetAttributeFromUser(id, "level");

            level = System.Convert.ToUInt32(reader);

            return level;
        }

        private void UpdateAttributeFromUser(ulong id, string attribute, uint value)
        {
            using (var connection = new MySqlConnection(_connection))
            {
                var command = new MySqlCommand($"UPDATE users SET {attribute}={value} WHERE userid='{id}'", connection);
                connection.Open();
                command.ExecuteNonQuery();
            }
        }

        private string GetAttributeFromUser(ulong id, string attribute)
        {
            using (var connection = new MySqlConnection(_connection))
            {
                var command = new MySqlCommand($"Select {attribute} FROM users WHERE userid='{id}'", connection);
                connection.Open();
                MySqlDataReader reader;
                using (reader = command.ExecuteReader())
                {
                    while (reader.Read())
                    {
                        return reader[attribute].ToString();
                    }
                }
            }

            return null;
        }
    }
}