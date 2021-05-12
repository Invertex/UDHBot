using System;
using System.Threading.Tasks;
using Discord;

namespace DiscordBot.Extensions
{
    public static class MessageExtensions
    {
        public static async Task<bool> TrySendMessage(this IDMChannel channel, string message)
        {
            try
            {
                await channel.SendMessageAsync(message);
            }
            catch (Exception)
            {
                return false;
            }
            return true;
        }
    }
}