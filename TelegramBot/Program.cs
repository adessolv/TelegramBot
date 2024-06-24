using Microsoft.Extensions.Configuration;
using Telegram.Bot;
using Telegram.Bot.Polling;
using static TelegramBot.BotHandlers;

namespace TelegramBot
{
    internal class Program
    {
        private static void Main()
        {
            var configuration = new ConfigurationBuilder()
                .AddJsonFile("appsettings.json")
                .Build();

            var botToken = configuration["BotSettings:Token"];
            var deeplKey = configuration["DeeplAPI:DeeplAPIKey"];

            if (string.IsNullOrEmpty(botToken) || (string.IsNullOrEmpty(deeplKey)))
            {
                Console.WriteLine("Bot token or Deelp key is missing in appsettings.json");
                return;
            }

            var botClient = new TelegramBotClient(botToken);
            using var cts = new CancellationTokenSource();

            InitializeHandlersAsync(deeplKey);

            botClient.StartReceiving(HandleUpdateAsync,
                HandleErrorAsync,
                new ReceiverOptions { AllowedUpdates = { } },
                cts.Token
                );

            var me = botClient.GetMeAsync().Result;
            Console.WriteLine($"Start listening for @{me.Username}");

            Console.WriteLine("Bot is up and running. Press any key to exit");
            Console.Read();

            cts.Cancel();
        }
    }
}