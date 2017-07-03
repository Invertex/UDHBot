using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading;
using System.Threading.Tasks;

namespace DiscordBot
{
    class HoldingPenService
    {
        private const int minutesToAddRole = 1;

        public static Timer Timer;
        private static Dictionary<ulong, DateTime> storedUsers = new Dictionary<ulong, DateTime>();

        public static void StartTimer()
        {
            Timer = new Timer(UpdateTimer, null, 0, 60 * 1000);
        }

        public static void UpdateTimer(object obj)
        {
            foreach (ulong id in storedUsers.Keys)
            {
                if (storedUsers[id].Subtract(DateTime.Now).TotalMinutes >= minutesToAddRole)
                {
                    PurgeUser(id);
                    storedUsers.Remove(id);
                }
            }
        }

        private static void PurgeUser(ulong id)
        {
        }
    }
}