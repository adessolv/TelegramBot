using Telegram.Bot.Exceptions;
using Telegram.Bot.Types.Enums;
using Telegram.Bot.Types;
using Telegram.Bot;
using Telegram.Bot.Types.ReplyMarkups;
using DeepL;
using DeepL.Model;

namespace TelegramBot
{
    public static class BotHandlers
    {
        private static Translator? _translator;
        private static string? _targetLang = LanguageCode.EnglishAmerican; // Default target language
        private static List<Language>? availLangs;

        // Constants for the menu
        private const string? StartCommand = "/start";

        private const string? HelpCommand = "/help";
        private const string? SetTargetCommand = "/settarget";
        private const string? UsageCommand = "/usage";

        public static async void InitializeHandlersAsync(string deeplKey)
        {
            _translator = new Translator(deeplKey);
            availLangs = new List<Language>(await _translator.GetTargetLanguagesAsync());
        }

        private static string GetLanguageName(string code)
        {
            var language = availLangs?.Find(l => l.Code == code);
            return language != null ? language.Name : code;
        }

        public static async Task HandleUpdateAsync(ITelegramBotClient botClient, Update update, CancellationToken cancellationToken)
        {
            // Menu button
            await botClient.SetMyCommandsAsync(new List<BotCommand>
                {
                    new BotCommand { Command = "/start", Description = "Start the bot" },
                    new BotCommand { Command = "/help", Description = "Get help" },
                    new BotCommand { Command = "/settarget", Description = "Set target language" },
                    new BotCommand { Command = "/usage", Description = "Show usage" }
                }, cancellationToken: cancellationToken);

            if (update.Type == UpdateType.Message && update.Message?.Text != null)
            {
                switch (update.Message.Text.ToLower())
                {
                    case StartCommand:
                        await botClient.SendTextMessageAsync(update.Message.Chat.Id,
                        "Welcome to the translation bot! 🤖\nTo set your target language enter /settarget: ",
                        cancellationToken: cancellationToken);
                        break;

                    case HelpCommand:
                        await botClient.SendTextMessageAsync(update.Message.Chat.Id,
                        "🗺️\nThis bot translates any text into carefully selected 12 languages.\nTo start, type /start.",
                        cancellationToken: cancellationToken);
                        break;

                    case SetTargetCommand:
                        var languages = new List<InlineKeyboardButton[]>
                    {
                        new [] { CreateLangButton("English 🇺🇸", LanguageCode.EnglishAmerican),
                                CreateLangButton("Czech 🇨🇿", LanguageCode.Czech),
                                CreateLangButton("French 🇫🇷", LanguageCode.French)},
                        new [] { CreateLangButton("German 🇩🇪", LanguageCode.German),
                                 CreateLangButton("Spanish 🇪🇸", LanguageCode.Spanish),
                                CreateLangButton("Italian 🇮🇹", LanguageCode.Italian),},
                        new [] { CreateLangButton("Swedish 🇸🇪", LanguageCode.Swedish),
                                CreateLangButton("Russian 🇷🇺", LanguageCode.Russian),
                                CreateLangButton("Ukrainian 🇺🇦", LanguageCode.Ukrainian),},
                        new [] { CreateLangButton("Latvian 🇱🇻", LanguageCode.Latvian),
                                CreateLangButton("Lithuanian 🇱🇹", LanguageCode.Lithuanian),
                                CreateLangButton("Polish 🇵🇱", LanguageCode.Polish)}
                    };
                        await botClient.SendTextMessageAsync(update.Message.Chat.Id, "Select your target language:",
                                               replyMarkup: new InlineKeyboardMarkup(languages),
                                               cancellationToken: cancellationToken);
                        break;

                    case UsageCommand:
                        await ShowUsage(botClient, update.Message.Chat.Id, cancellationToken);
                        break;

                    default:
                        await TranslateText(botClient, update.Message.Chat.Id, update.Message.Text, cancellationToken);
                        break;
                }
            }
            else if (update.Type == UpdateType.CallbackQuery)
            {
                await botClient.AnswerCallbackQueryAsync(update.CallbackQuery.Id, cancellationToken: cancellationToken);
                _targetLang = update.CallbackQuery.Data;  // Set the target language

                var languageName = GetLanguageName(_targetLang);

                await botClient.SendTextMessageAsync(update.CallbackQuery.Message.Chat.Id,
                    $"Target language set to {languageName}. You can now send text to translate.",
                    cancellationToken: cancellationToken);
            }
        }

        private static async Task TranslateText(ITelegramBotClient botClient, long chatId, string text, CancellationToken cancellationToken)
        {
            var translation = await _translator.TranslateTextAsync(text, null, _targetLang);
            await botClient.SendTextMessageAsync(chatId, translation.Text, cancellationToken: cancellationToken);
        }

        public static Task HandleErrorAsync(ITelegramBotClient botClient, Exception exception, CancellationToken cancellationToken)
        {
            var errorMessage = exception switch
            {
                ApiRequestException apiRequestException => $"Telegram API Error:\n[{apiRequestException.ErrorCode}]\n{apiRequestException.Message}",
                _ => exception.ToString()
            };
            Console.WriteLine(errorMessage);
            return Task.CompletedTask;
        }

        private static InlineKeyboardButton CreateLangButton(string text, string callbackData)
        {
            return InlineKeyboardButton.WithCallbackData(text, callbackData);
        }

        private static async Task ShowUsage(ITelegramBotClient botClient, long chatId, CancellationToken cancellationToken)
        {
            try
            {
                var usage = await _translator.GetUsageAsync();
                string usageMessage = $"💡 Usage:\n\n" +
                                      $"Character Limit: {usage.Character?.Limit}\n" +
                                      $"Character Usage: {usage.Character?.Count}\n" +
                                      $"Characters Remaining: {usage.Character?.Limit - usage.Character?.Count}";

                await botClient.SendTextMessageAsync(chatId, usageMessage, cancellationToken: cancellationToken);
            }
            catch (Exception ex)
            {
                await botClient.SendTextMessageAsync(chatId, $"Error retrieving usage: {ex.Message}", cancellationToken: cancellationToken);
            }
        }
    }
}