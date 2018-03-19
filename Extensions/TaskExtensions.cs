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
        
        public static Task DeleteAfterSeconds(this IDeletable message, TimeSpan timeSpan)
        {
            return Task.Delay(timeSpan).ContinueWith(async _ => await message.DeleteAsync());
        }

        public static Task DeleteAfterSeconds<T>(this Task<T> task, double seconds, bool awaitDeletion = false) where T : IDeletable
        {
            var deletion = Task.Run(async () => await (await task).DeleteAfterSeconds(seconds));
            return awaitDeletion ? deletion : task;
        }
        
        public static Task DeleteAfterTimeSpan<T>(this Task<T> task, TimeSpan timeSpan, bool awaitDeletion = false) where T : IDeletable
        {
            var deletion = Task.Run(async () => await (await task).DeleteAfterSeconds(timeSpan));
            return awaitDeletion ? deletion : task;
        }
    }
}