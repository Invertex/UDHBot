using System;
using System.Threading.Tasks;
using Discord;

namespace DiscordBot.Services
{
    public class CasinoService
    {
        private readonly ILoggingService _loggingService;
        private readonly UpdateService _updateService;
        private readonly DatabaseService _databaseService;

        private readonly Settings.Deserialized.Settings _settings;

        private int _slotMachineCashPool;
        private int _lotteryCashPool;

        private const string UDC = "UDC";

        private Random _random;

        public CasinoService(ILoggingService logging, UpdateService updateService, DatabaseService databaseService,
            Settings.Deserialized.Settings settings)
        {
            _loggingService = logging;
            _updateService = updateService;
            _databaseService = databaseService;
            _settings = settings;
            _random = new Random();
            LoadData();
            UpdateLoop();
        }

        private async void UpdateLoop()
        {
            while (true)
            {
                await Task.Delay(10000);
                SaveData();
            }
        }

        private void LoadData()
        {
            var data = _updateService.GetCasinoData();
            _lotteryCashPool = data.LotteryCashPool;
            _slotMachineCashPool = data.SlotMachineCashPool;

            if (_lotteryCashPool == 0)
                _lotteryCashPool = 300000;
            if (_slotMachineCashPool == 0)
                _slotMachineCashPool = 150000;
        }

        private void SaveData()
        {
            CasinoData data = new CasinoData() {LotteryCashPool = _lotteryCashPool, SlotMachineCashPool = _slotMachineCashPool};
            _updateService.SetCasinoData(data);
        }

        public int GetUserUdc(ulong userId)
        {
            return _databaseService.GetUserUdc(userId);
        }

        public async Task<(string imagePath, string reply)> PlaySlotMachine(IUser user, int amount)
        {
            int udc = GetUserUdc(user.Id);

            if (amount > udc)
                return (null, $"Sorry {user.Mention}, you only have **{udc}**{UDC}");

            if (amount < 100)
                return (null, $"Sorry {user.Mention}, you must play at least **100**{UDC} at once");

            if (amount > 10000)
                return (null, $"Sorry {user.Mention}, you can only play **10 000**{UDC} at once.");

            int random = _random.Next(0, 100);

            _databaseService.AddUserUdc(user.Id, -amount);
            _slotMachineCashPool += amount / 2;

            if (random <= 60) //Lose
            {
                return (_settings.ServerRootPath + "/casino/lose.png",
                    $"{user.Mention} You're in a daze staring at the blinking machine rolling away at near the speed of light. Sadly, it stops without any matching icons. You feel your luck is near and that you should try again.");
            }
            else if (random <= 70) //Apple, 2x
            {
                if (Slot(65))
                {
                    _databaseService.AddUserUdc(user.Id, amount * 2);
                    return (_settings.ServerRootPath + "/casino/apple.png",
                        $"{user.Mention} The machine blinks and spins away with a captivating sound. As it begins to slow down, you see tasty apples begin to lock. One, two... and a third. A smile on your face, you almost jumps in joy but realize you *only* doubled your bet. You should try to invest your winnings and make even more money, you think. *You won {amount * 2}UDC*");
                }
                else
                    return (_settings.ServerRootPath + "/casino/apple_nearmiss.png",
                        $"{user.Mention} The machine blinks and spins away with a captivating sound. As it begins to slow down, you see tasty apples begin to lock. One, two... sadly the third didn't make it. You weep silently and decide that your luck will be better next time. You should try again !");
            }
            else if (random <= 85) //Banana, 5x
            {
                if (Slot(35))
                {
                    _databaseService.AddUserUdc(user.Id, amount * 5);
                    return (_settings.ServerRootPath + "/casino/banana.png",
                        $"{user.Mention} The heavy slot machine begins to grumble as the wheels whirls around fiercely. Yellow soon fills your view, locking in place one after the other. A tear drop from your eyes, a big smile draws on your face and you want to jump in joy. You won five times what you bet. You feel like you can win even more and want to try again. *You won {amount * 5}UDC*");
                }
                else
                    return (_settings.ServerRootPath + "/casino/banana_nearmiss.png",
                        $"{user.Mention} The heavy slot machine begins to grumble as the wheels whirls around fiercely. Yellow soon fills your view, locking in place one after the other. But, at the last moment, one of those damn android girl locks in the middle row and ruins your day. You feel like you'll have better luck next time and want to try again.");
            }
            else if (random <= 95) //Donut, 15x
            {
                if (Slot(15))
                {
                    _databaseService.AddUserUdc(user.Id, amount * 15);
                    return (_settings.ServerRootPath + "/casino/donut.png",
                        $"{user.Mention} As the machine starts to spin you feel like it's your lucky day. It goes faster and faster, producing a melodious sound to your ears. Sugar soon hits your mouth, the sugar of those three donuts that just aligned. You jump in joy, having won fifteen times what you bet. It's really your lucky day, you should try again. *You won {amount * 15}UDC*");
                }
                else
                    return (_settings.ServerRootPath + "/casino/donut_nearmiss.png",
                        $"{user.Mention} As the machine starts to spin you feel like it's your lucky day. It goes faster and faster, producing a melodious sound to your ears. Sugar soon hits your mouth, the sugar of two tasty donuts that just aligned. Sadly, an apple makes its way into the middle wheel. This apple tastes bitter to you, but you still feel that you'll have better luck next try. You really think you should try again.");
            }
            else //Jackpot
            {
                if (Slot(4))
                {
                    _databaseService.AddUserUdc(user.Id, _slotMachineCashPool);
                    int won = _slotMachineCashPool;
                    _slotMachineCashPool = 150000;
                    return (_settings.ServerRootPath + "/casino/jackpot.png",
                        $"{user.Mention} The machine spinning wildly is like an epic symphony to your ears. Soon, more and more light start to blink all around, and sounds that trigger your reward center. As you see those sexy android girls aligning, an immense joy fills you. You begin to hear cheer and see coins upon coins falling to the ground. You did it, you won the jackpot. Are you gonna try to win even more ? *You won a jackpot of {won}UDC*");
                }

                return (_settings.ServerRootPath + "/casino/jackpot_nearmiss.png",
                    $"{user.Mention} The machine spinning wildly is like an epic symphony to your ears. Soon, more and more light start to blink all around, and sounds that trigger your reward center. As you see those sexy android girls aligning, an immense joy fills you. Sadly, your world shatters fast as a damn banana makes its way in. But you feel optimist after coming so close to winning this big {_slotMachineCashPool}UDC jackpot and think you should try one more time.");
            }
        }

        private bool Slot(int chance)
        {
            return _random.Next(0, 100) <= chance;
        }

        public string SlotCashPool()
        {
            return $"The Slot Machine Jackpot is **{_slotMachineCashPool}**UDC";
        }
    }
}