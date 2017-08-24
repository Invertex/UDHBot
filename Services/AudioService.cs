using System;
using System.Diagnostics;
using System.IO;
using System.Threading.Tasks;
using Discord;
using Discord.Audio;
using Discord.WebSocket;

namespace DiscordBot
{
    public class AudioService
    {
        private LoggingService _logging;
        private DiscordSocketClient _client;
        private IAudioClient _audioClient;
        
        public AudioService(LoggingService logging, DiscordSocketClient client)
        {
            _logging = logging;
            _client = client;
            

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
                    await SendAsync(_audioClient, Settings.GetServerRootPath() + @"/music/kanashii.mp3");
                    await Task.Delay(1000);
                    await SendAsync(_audioClient, Settings.GetServerRootPath() + @"/music/oddloop.mp3");
                }
                catch (Exception e)
                {}
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
            System.Console.WriteLine("before copy");
            await output.CopyToAsync(discord);
            System.Console.WriteLine("copied");
            await discord.FlushAsync();
        }
    }
}