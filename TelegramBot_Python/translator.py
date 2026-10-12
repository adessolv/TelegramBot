import deepl
import logging

logger = logging.getLogger(__name__)

class DeeplService:
    """Обёртка над официальным SDK DeepL для перевода текста."""

    def __init__(self, auth_key: str):
        self.auth_key = auth_key
        self.translator = deepl.Translator(auth_key)

        self.languages = self.translator.get_target_languages()
        logger.info("✅ DeepL Translator initialized")

    def translate_text(self, text: str, target_lang: str) -> str:
        """Перевод текста с помощью DeepL API."""
        try:
            if not text.strip():
                return "⚠️ Empty message — nothing to translate."
            result = self.translator.translate_text(text, target_lang=target_lang.upper())
            return result.text
        except deepl.DeepLException as e:
            logger.error(f"❌ DeepL API error: {e}")
            return f"DeepL API error: {e}"
        except Exception as e:
            logger.error(f"❌ Translation error: {e}")
            return f"Translation error: {e}"