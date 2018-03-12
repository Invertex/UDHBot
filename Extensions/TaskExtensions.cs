using System;
using System.Threading.Tasks;
using Discord;

namespace DiscordBot.Extensions
{
    public static class TaskExtensions
    {
        public static Task DeleteAfterSeconds(this IDeletable message, double seconds)
        {
            return Task.Delay(TimeSpan.FromSeconds(seconds)).ContinueWith(async _ => await message.DeleteAsync());
        }

        public static Task DeleteAfterSeconds<T>(this Task<T> task, double seconds, bool awaitDeletion = false) where T : IDeletable
        {
            Task deletion = task.Result.DeleteAfterSeconds(seconds);
            return (awaitDeletion) ? deletion : task;
        }
    }
}