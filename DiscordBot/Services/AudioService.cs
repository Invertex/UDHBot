using System;
using System.Diagnostics;
using System.IO;
using System.Threading.Tasks;
using Discord;
using Discord.Audio;
using Discord.WebSocket;

namespace DiscordBot.Services
{
    public class AudioService
    {
        private ILoggingService _logging;
        private DiscordSocketClient _client;
        private IAudioClient _audioClient;

        private readonly Settings.Deserialized.Settings _settings;

        public AudioService(ILoggingService logging, DiscordSocketClient client, Settings.Deserialized.Settings settings)
        {
            _logging = logging;
            _client = client;
            _settings = settings;
        }

        private async Task ConnectAudioClient()
        {
            IVoiceChannel channel = _client.GetChannel(344902923517427712) as IVoiceChannel;
            _audioClient = await channel.ConnectAsync();
        }

        public async void Music()
        {
            await ConnectAudioClient();
            if (_audioClient == null)
                return;

            while (true)
            {
                try
                {
                    await SendAsync(_audioClient, _settings.ServerRootPath + @"/music/kanashii.mp3");
                    await Task.Delay(1000);
                    await SendAsync(_audioClient, _settings.ServerRootPath + @"/music/oddloop.mp3");
                }
                catch (Exception)
                {
                }
            }
        }

        private Process CreateStream(string path)
        {
            return Process.Start(new ProcessStartInfo
            {
                FileName = "ffmpeg",
                Arguments = $"-hide_banner -loglevel panic -i \"{path}\" -ac 2 -f s16le -ar 48000 pipe:1",
                UseShellExecute = false,
                RedirectStandardOutput = true
            });
        }

        private async Task SendAsync(IAudioClient client, string path)
        {
            // Create FFmpeg using the previous example
            Process ffmpeg = CreateStream(path);
            Stream output = ffmpeg.StandardOutput.BaseStream;
            AudioOutStream discord = client.CreatePCMStream(AudioApplication.Music, 48000);
            Console.WriteLine("before copy");
            await output.CopyToAsync(discord);
            Console.WriteLine("copied");
            await discord.FlushAsync();
        }
    }
}