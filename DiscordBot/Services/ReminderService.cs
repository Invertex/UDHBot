using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;
using Discord;
using Discord.WebSocket;

namespace DiscordBot.Services
{
    [Serializable]
    public class ReminderItem {
        public ulong ChannelId { get; set; }
        public ulong MessageId { get; set; }
        public ulong UserId { get; set; }
        public string Message { get; set; }
        public DateTime When { get; set; }
    }

    public class ReminderService
    {
        private bool _isRunning = false;
        private DateTime _nearestReminder = DateTime.Now;
        
        private readonly DiscordSocketClient _client;
        private List<ReminderItem> _reminders;
        
        private bool _hasChangedSinceLastSave = false;
        private DateTime _nextSave;
        private int _saveInterval = 300;
        
        private int _maxUserReminders = 5;

        public ReminderService(DiscordSocketClient client)
        {
            _client = client;
            _nextSave = DateTime.Now.AddSeconds(_saveInterval);
            LoadReminders();

            client.Ready += OnReady;
        }

        private Task OnReady()
        {
            if (!_isRunning)
            {
                Task.Run(CheckReminders);
                _isRunning = true;
            }
            return Task.CompletedTask;
        }

        // Serialize Reminders to file
        public void SaveReminders()
        {
            Utils.SerializeUtil.SerializeFile(@"Settings/reminders.json", _reminders);
        }
        private void LoadReminders()
        {
            _reminders = Utils.SerializeUtil.DeserializeFile<List<ReminderItem>>(@"Settings/reminders.json");
        }
        public void AddReminder(ReminderItem reminder)
        {
            _reminders.Add(reminder);
            _hasChangedSinceLastSave = true;
            
            // We check if this reminder is sooner than the next one
            if (_nearestReminder > reminder.When)
                _nearestReminder = reminder.When;
        }
        
        public bool UserHasTooManyReminders(ulong userId)
        {
            return _reminders.FindAll(x => x.UserId == userId).Count >= _maxUserReminders;
        }
        
        public List<ReminderItem> GetUserReminders(ulong userId)
        {
            return _reminders.FindAll(x => x.UserId == userId);
        }

        public int RemoveReminders(IUser user, int index = 0)
        {
            int count = 0;
            if (index == 0)
                count = _reminders.RemoveAll(x => x.UserId == user.Id);
            else
            {
                var userReminders = GetUserReminders(user.Id);
                if (userReminders.Count < index)
                    return -1;

                _reminders.Remove(userReminders[index - 1]);
                count = 1;
            }

            if (count != 0)
                _hasChangedSinceLastSave = true;
            return count;
        }
        
        // Check if reminders are due in an async task that loops from the constructor
        public async Task CheckReminders()
        {
            while (true)
            {
                var now = DateTime.Now;
                // We wait until we know at least one reminder needs to be checked
                if (now > _nearestReminder && _reminders.Count > 0)
                {
                    // Iterate through list backwards checking dates and replying to messages
                    for (int i = _reminders.Count - 1; i >= 0; i--)
                    {
                        if (now > _reminders[i].When)
                        {
                            var channel = _client.GetChannel(_reminders[i].ChannelId) as IMessageChannel;
                            if (channel != null)
                            {
                                var message = await channel.GetMessageAsync(_reminders[i].MessageId) as IUserMessage;
                                if (message != null)
                                    await message.ReplyAsync(
                                        $"{message.Author.Mention} reminder: {_reminders[i].Message}");
                                else
                                {
                                    var user = _client.GetUser(_reminders[i].UserId);
                                    if (user != null)
                                        await channel.SendMessageAsync(
                                            $"{user.Mention} reminder: {_reminders[i].Message}");
                                }
                            }

                            _reminders.RemoveAt(i);
                            _hasChangedSinceLastSave = true;
                        }
                    }
                    // Find the nearest reminder in _reminders and set if there is at least 1 reminder
                    if (_reminders.Count > 0)
                        _nearestReminder = _reminders.Min(x => x.When);
                }

                // We check if there has been a change to the reminders list since the last update.
                if (_hasChangedSinceLastSave && DateTime.Now > _nextSave)
                {
                    Console.WriteLine("Saved");
                    SaveReminders();
                    _nextSave = DateTime.Now.AddSeconds(_saveInterval);
                    _hasChangedSinceLastSave = false;
                }
                await Task.Delay(1000);
            }
        }
    }
}