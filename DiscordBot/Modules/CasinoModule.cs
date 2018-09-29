using System.Threading.Tasks;
using Discord;
using Discord.Commands;
using DiscordBot.Services;

namespace DiscordBot.Modules
{
    public class CasinoModule : ModuleBase
    {
        private readonly CasinoService _casinoService;
        private readonly Settings.Deserialized.Settings _settings;


        public CasinoModule(CasinoService casinoService, Settings.Deserialized.Settings settings)
        {
            _casinoService = casinoService;
            _settings = settings;
        }

        [Command("slot"), Summary("Play the slot machine. Syntax : !slot amount")]
        [Alias("slotmachine")]
        private async Task SlotMachine(int amount)
        {
            if (Context.Channel.Id != _settings.CasinoChannel.Id)
            {
                await Task.Delay(1000);
                await Context.Message.DeleteAsync();
                return;
            }

            IUser user = Context.User;

            var reply = await _casinoService.PlaySlotMachine(user, amount);
            if (reply.imagePath != null)
                await Context.Channel.SendFileAsync((string)reply.imagePath, reply.reply);
            else
                await ReplyAsync(reply.reply);
        }

        [Command("udc"), Summary("Get the amount of UDC. Syntax : !udc")]
        [Alias("coins", "coin")]
        private async Task Coins()
        {
            if (Context.Channel.Id != _settings.CasinoChannel.Id)
            {
                await Task.Delay(1000);
                await Context.Message.DeleteAsync();
                return;
            }

            IUser user = Context.User;

            await ReplyAsync($"{user.Mention} you have **{_casinoService.GetUserUdc(user.Id)}**UDC");
        }
        
        [Command("jackpot"), Summary("Get the slot machine jackpot. Syntax : !jackpot")]
        private async Task Jackpot()
        {
            if (Context.Channel.Id != _settings.CasinoChannel.Id)
            {
                await Task.Delay(1000);
                await Context.Message.DeleteAsync();
                return;
            }

            await ReplyAsync(_casinoService.SlotCashPool());
        }
    }
}