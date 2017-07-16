using System.Xml.Linq;
using System;
using System.Data.SqlClient;
using System.Runtime.CompilerServices;
using System.Runtime.InteropServices.ComTypes;
using System.Threading.Tasks;
using Discord.WebSocket;
using MySql.Data.MySqlClient;

namespace DiscordBot
{
    public class DatabaseService
    {
        private string _connection { get; }

        private readonly LoggingService _logging;

        public DatabaseService(LoggingService logging)
        {
            _connection = SettingsHandler.LoadValueString("dbConnectionString", JsonFile.Settings);
            _logging = logging;
        }

        /*
        **Publisher Stuff
        */
        public uint GetPublisherAdCount()
        {
            using (var connection = new MySqlConnection(_connection))
            {
                var command = new MySqlCommand("SELECT COUNT(*) FROM advertisment", connection);
                connection.Open();
                return Convert.ToUInt32(command.ExecuteScalar());
            }
        }

        public (uint, string) GetPublisherAd(uint id)
        {
            using (var connection = new MySqlConnection(_connection))
            {
                var command = new MySqlCommand($"Select username, discriminator, packageID FROM advertisment WHERE id='{id}'", connection);
                connection.Open();
                MySqlDataReader reader;
                using (reader = command.ExecuteReader())
                {
                    while (reader.Read())
                    {
                        return (Convert.ToUInt32(reader["packageID"]), $"{reader["username"]}#{reader["discriminator"]}");
                    }
                }
            }
            return (0, null);
        }

        public void AddPublisherPackage(string username, string discriminator, string userid, uint packageID)
        {
            try
            {
                using (var connection = new MySqlConnection(_connection))
                {
                    var command = new MySqlCommand(
                        $"INSERT INTO advertisment SET username='{username}', discriminator='{discriminator}', userid='{userid}', packageID='{packageID}', date='{DateTime.Now:yyyy-MM-dd HH:mm:ss}'",
                        connection);
                    connection.Open();
                    command.ExecuteNonQuery();
                }
            }
            catch (Exception e)
            {
                _logging.LogAction($"Error when trying to add package {packageID} from {username}#{discriminator} - {userid} : {e}");
            }
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
            UpdateAttributeFromUser(id, "karma", oldKarma + karma);
        }

        public uint GetUserXp(ulong id)
        {
            uint xp;
            string reader = GetAttributeFromUser(id, "exp");

            xp = Convert.ToUInt32(reader);

            return xp;
        }

        public uint GetUserKarma(ulong id)
        {
            uint karma;
            string reader = GetAttributeFromUser(id, "karma");

            karma = Convert.ToUInt32(reader);

            return karma;
        }

        public uint GetUserRank(ulong id)
        {
            uint rank;
            string reader = GetAttributeFromUser(id, "rank");

            rank = Convert.ToUInt32(reader);

            return rank;
        }

        public uint GetUserLevel(ulong id)
        {
            uint level;
            string reader = GetAttributeFromUser(id, "level");

            level = Convert.ToUInt32(reader);

            return level;
        }

        public string GetUserJoinDate(ulong id)
        {
            return GetAttributeFromUser(id, "joinDate");
        }

        public void UpdateUserName(ulong id, string name)
        {
            UpdateAttributeFromUser(id, "username", name);
        }

        public void UpdateUserAvatar(ulong id, string avatar)
        {
            UpdateAttributeFromUser(id, "avatarUrl", avatar);
        }

        public async void AddNewUser(SocketGuildUser user)
        {
            try
            {
                using (var connection = new MySqlConnection(_connection))
                {
                    var command = new MySqlCommand(
                        $"INSERT INTO users SET username='{user.Username}', userid='{user.Id}', discriminator='{user.DiscriminatorValue}'," +
                        $"avatar='{user.AvatarId}', " +
                        $"avatarURL='{user.GetAvatarUrl()}'," +
                        $"bot='{(user.IsBot ? 1 : 0)}', status='{user.Status}', joinDate='{DateTime.Now:yyyy-MM-dd HH:mm:ss}'", connection);
                    connection.Open();
                    command.ExecuteNonQuery();
                    await _logging.LogAction($"User {user.Username}#{user.DiscriminatorValue} succesfully added to the databse.", true,
                        false);
                }
            }
            catch (Exception e)
            {
                await _logging.LogAction($"Error when trying to add user {user.Id} to the database : {e}", true, false);
            }
        }

        public async void DeleteUser(ulong id)
        {
            try
            {
                using (var connection = new MySqlConnection(_connection))
                {
                    var command = new MySqlCommand($"DELETE FROM users WHERE userid='{id}'", connection);
                    var command2 = new MySqlCommand($"INSERT users_remove SELECT * FROM users WHERE userid='{id}'", connection);
                    connection.Open();
                    command2.ExecuteNonQuery();
                    command.ExecuteNonQuery();
                }
            }
            catch (Exception e)
            {
                await _logging.LogAction($"Error when trying to delete user {id} from the database : {e}", true, false);
            }
        }

        private async void UpdateAttributeFromUser(ulong id, string attribute, uint value)
        {
            try
            {
                using (var connection = new MySqlConnection(_connection))
                {
                    var command = new MySqlCommand($"UPDATE users SET {attribute}={value} WHERE userid='{id}'", connection);
                    connection.Open();
                    command.ExecuteNonQuery();
                }
            }
            catch (Exception e)
            {
                await _logging.LogAction($"Error when trying to edit attribute {attribute} from user {id} with value {value} : {e}", true,
                    false);
            }
        }

        private async void UpdateAttributeFromUser(ulong id, string attribute, string value)
        {
            try
            {
                value = MySqlHelper.EscapeString(value);
                using (var connection = new MySqlConnection(_connection))
                {
                    var command = new MySqlCommand($"UPDATE users SET {attribute}={value} WHERE userid='{id}'", connection);
                    connection.Open();
                    command.ExecuteNonQuery();
                }
            }
            catch (Exception e)
            {
                await _logging.LogAction($"Error when trying to edit attribute {attribute} from user {id} with value {value} : {e}", true,
                    false);
            }
        }

        private string GetAttributeFromUser(ulong id, string attribute)
        {
            try
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
            }
            catch (Exception e)
            {
                _logging.LogAction($"Error when trying to get attribute {attribute} from user {id} : {e}", true,
                    false);
            }

            return null;
        }
    }
}