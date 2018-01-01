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
        public static void AddCooldown(this Dictionary<ulong, DateTime> cooldowns, ulong userId, int seconds, int minutes = 0, int hours = 0, int days = 0, bool ignoreExisting = false)
        {
            TimeSpan cooldownTime = new TimeSpan(days, hours, minutes, seconds);

            if (cooldowns.HasUser(userId))
            {
                if(ignoreExisting)
                {
                    cooldowns[userId] = DateTime.Now.Add(cooldownTime);
                    return;
                }
                cooldowns[userId].Add(cooldownTime);
                return;
            }
            cooldowns.Add(userId, DateTime.Now.Add(cooldownTime));
        }
    #endregion
    }
}
