using System;
using System.Collections.Generic;

namespace DiscordBot.Extensions
{
    public static class UserServiceExtensions
    {
        #region Cooldown related

        /// <summary>
        /// Checks to see if user is on this cooldown list. User is automatically removed from list if their time is up and will return false.
        /// </summary>
        /// <param name="cooldowns"></param>
        /// <param name="userId"></param>
        /// <returns></returns>
        public static bool HasUser(this Dictionary<ulong, DateTime> cooldowns, ulong userId)
        {
            if (cooldowns.ContainsKey(userId))
            {
                if (cooldowns[userId] > DateTime.Now)
                {
                    return true;
                }

                cooldowns.Remove(userId);
            }

            return false;
        }

        /// <summary>
        /// Adds user to cooldown list with given amount of time. If user already on list, time is added to existing time.
        /// </summary>
        /// <param name="cooldowns"></param>
        /// <param name="userId"></param>
        /// <param name="seconds"></param>
        /// <param name="minutes"></param>
        /// <param name="hours"></param>
        /// <param name="days"></param>
        public static void AddCooldown(this Dictionary<ulong, DateTime> cooldowns, ulong userId, int seconds = 0, int minutes = 0,
            int hours = 0, int days = 0, bool ignoreExisting = false)
        {
            TimeSpan cooldownTime = new TimeSpan(days, hours, minutes, seconds);

            if (cooldowns.HasUser(userId))
            {
                if (ignoreExisting)
                {
                    cooldowns[userId] = DateTime.Now.Add(cooldownTime);
                    return;
                }

                cooldowns[userId].Add(cooldownTime);
                return;
            }

            cooldowns.Add(userId, DateTime.Now.Add(cooldownTime));
        }

        /// <summary>
        /// Set a max days (permanent) cooldown for the given user, or removes the permanent cooldown if set false.
        /// </summary>
        /// <param name="cooldowns"></param>
        /// <param name="userId"></param>
        /// <param name="enabled">Set to true for permanent, or false to remove it.</param>
        public static void SetPermanent(this Dictionary<ulong, DateTime> cooldowns, ulong userId, bool enabled)
        {
            cooldowns.AddCooldown(userId, days: (enabled) ? 9999 : 0, ignoreExisting: true);
        }

        public static bool IsPermanent(this Dictionary<ulong, DateTime> cooldowns, ulong userId)
        {
            return cooldowns.Days(userId) > 5000;
        }

        public static int Days(this Dictionary<ulong, DateTime> cooldowns, ulong userId)
        {
            if (!cooldowns.HasUser(userId))
            {
                return 0;
            }

            return cooldowns[userId].Subtract(DateTime.Now).Days;
        }

        public static int Hours(this Dictionary<ulong, DateTime> cooldowns, ulong userId)
        {
            if (!cooldowns.HasUser(userId))
            {
                return 0;
            }

            return cooldowns[userId].Subtract(DateTime.Now).Hours;
        }

        public static int Minutes(this Dictionary<ulong, DateTime> cooldowns, ulong userId)
        {
            if (!cooldowns.HasUser(userId))
            {
                return 0;
            }

            return cooldowns[userId].Subtract(DateTime.Now).Minutes;
        }

        public static int Seconds(this Dictionary<ulong, DateTime> cooldowns, ulong userId)
        {
            if (!cooldowns.HasUser(userId))
            {
                return 0;
            }

            return cooldowns[userId].Subtract(DateTime.Now).Seconds;
        }

        #endregion
    }
}