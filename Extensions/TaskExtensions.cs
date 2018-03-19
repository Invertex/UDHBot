using System;
using System.Threading.Tasks;
using Discord;

namespace DiscordBot.Extensions
{
    public static class TaskExtensions
    {
        public static Task DeleteAfterTime(this IDeletable message, int seconds = 0, int minutes = 0, int hours = 0, int days = 0) => message.DeleteAfterSeconds(new TimeSpan(seconds, minutes, hours, days));
        public static Task DeleteAfterSeconds(this IDeletable message, double seconds) => message.DeleteAfterSeconds(TimeSpan.FromSeconds(seconds));
        public static Task DeleteAfterSeconds(this IDeletable message, TimeSpan timeSpan)
        {
            return Task.Delay(timeSpan).ContinueWith(async _ => await message.DeleteAsync());
        }

        public static Task DeleteAfterTime<T>(this Task<T> task, int seconds = 0, int minutes = 0, int hours = 0, int days = 0, bool awaitDeletion = false) where T : IDeletable => task.DeleteAfterTimeSpan(new TimeSpan(seconds, minutes, hours, days), awaitDeletion);
        public static Task DeleteAfterSeconds<T>(this Task<T> task, double seconds, bool awaitDeletion = false) where T : IDeletable => task.DeleteAfterTimeSpan(TimeSpan.FromSeconds(seconds), awaitDeletion);
        public static Task DeleteAfterTimeSpan<T>(this Task<T> task, TimeSpan timeSpan, bool awaitDeletion = false) where T : IDeletable
        {
            var deletion = Task.Run(async () => await (await task).DeleteAfterSeconds(timeSpan));
            return awaitDeletion ? deletion : task;
        }
    }
}