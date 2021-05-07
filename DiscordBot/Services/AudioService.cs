using System;
using System.Diagnostics;
using System.Threading.Tasks;
using Discord;
using Discord.Audio;
using Discord.WebSocket;

namespace DiscordBot.Services
{
    public class AudioService
    {
        private readonly Settings.Deserialized.Settings _settings;
        private IAudioClient _audioClient;
        private readonly DiscordSocketClient _client;

        public AudioService(DiscordSocketClient client, Settings.Deserialized.Settings settings)
        {
            _client = client;
            _settings = settings;
        }

        private async Task ConnectAudioClient()
        {
            if (_client.GetChannel(344902923517427712) is IVoiceChannel channel) _audioClient = await channel.ConnectAsync();
        }

        public async Task Music()
        {
            await ConnectAudioClient();
            if (_audioClient == null)
                return;

            while (true)
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

        private Process CreateStream(string path) =>
            Process.Start(new ProcessStartInfo
            {
                FileName = "ffmpeg",
                Arguments = $"-hide_banner -loglevel panic -i \"{path}\" -ac 2 -f s16le -ar 48000 pipe:1",
                UseShellExecute = false,
                RedirectStandardOutput = true
            });

        private async Task SendAsync(IAudioClient client, string path)
        {
            // Create FFmpeg using the previous example
            var ffmpeg = CreateStream(path);
            var output = ffmpeg.StandardOutput.BaseStream;
            var discord = client.CreatePCMStream(AudioApplication.Music, 48000);
            Console.WriteLine("before copy");
            await output.CopyToAsync(discord);
            Console.WriteLine("copied");
            await discord.FlushAsync();
        }
    }
}