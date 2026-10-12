import logging

from telegram import Update, InlineKeyboardButton, InlineKeyboardMarkup, BotCommand
from telegram.ext import ContextTypes
from translator import DeeplService

logger = logging.getLogger(__name__)

target_lang: str = "EN"

LANGUAGES = [
    [("English 🇺🇸", "EN"), ("Russian 🇷🇺", "RU"), ("Latvian 🇱🇻", "LV")],
    [("Spanish 🇪🇸", "ES"), ("French 🇫🇷", "FR"), ("German 🇩🇪", "DE")],
    [("Italian 🇮🇹", "IT"), ("Ukrainian 🇺🇦", "UK"), ("Polish 🇵🇱", "PL")],
    [("Lithuanian  🇱🇹", "LT"), ("Swedish 🇸🇪", "SV"),("Czech 🇨🇿", "CS")],
]

def get_language_name(code: str) -> str:
    for row in LANGUAGES:
        for name, lang_code in row:
            if lang_code.upper() == code.upper():
                return name
    return code

async def start(update: Update, context: ContextTypes.DEFAULT_TYPE):
    await context.bot.set_my_commands([
        BotCommand("start", "Start the bot"),
        BotCommand("help", "Get help"),
        BotCommand("settarget", "Set target language"),
        BotCommand("usage", "Show usage")
    ])

    await update.message.reply_text(
        "👋 Hi! I am a translator bot based on DeepL.\n"
        "To set your target language, enter /settarget:"
    )

# --------------------
# COMMANDS
#---------------------

async def help(update: Update, context: ContextTypes.DEFAULT_TYPE):
    await update.message.reply_text(
        "🗺️\nThis bot translates any text into carefully selected 12 languages.\n"
        "To start, type /start."
    )

async def settarget(update: Update, context: ContextTypes.DEFAULT_TYPE):
    keyboard = [
        [InlineKeyboardButton(name, callback_data=code) for name, code in row]
        for row in LANGUAGES
    ]

    await update.message.reply_text("🌍 Choose your target language:", reply_markup=InlineKeyboardMarkup(keyboard))

async def usage(update: Update, context: ContextTypes.DEFAULT_TYPE):
    service: DeeplService = context.bot_data["deepl_service"]
    try:
        usage_info = service.translator.get_usage()

        message = (
            "💡 Usage:\n\n"
            f"Character limit: {usage_info.character.limit}\n"
            f"Character count: {usage_info.character.count}\n"
            f"Characters remaining: {usage_info.character.limit - usage_info.character.count}"
        )
        await update.message.reply_text(message)
    except Exception as ex:
        await update.message.reply_text(f"Error retrieving usage: {ex}")

async def lang_callback(update: Update, context: ContextTypes.DEFAULT_TYPE):
    global target_lang
    query = update.callback_query
    await query.answer()

    target_lang = query.data
    name = get_language_name(target_lang)

    await query.message.reply_text(
        f"Target language set to {name}. You can now send your text to translate."
    )

async def translate_text(update: Update, context: ContextTypes.DEFAULT_TYPE):
    text = update.message.text
    service: DeeplService = context.bot_data["deepl_service"]

    try:
        result = service.translate_text(text, target_lang=target_lang)
        await update.message.reply_text(result)

    except Exception as ex:
        await update.message.reply_text(f"Translation error: {ex}")

async def error_handler(update: Update, context: ContextTypes.DEFAULT_TYPE):
    logging.error(f"Telegram error: {context.error}")