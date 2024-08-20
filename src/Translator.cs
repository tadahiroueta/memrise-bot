using Newtonsoft.Json;

namespace MemriseBot {
    /// <summary>
    /// Responsible for learning and providing answers, with long-term memory saved in json file.
    /// </summary>

    class Translator {
        private const string translationsPath = "../../../data/translations.json";
        private Dictionary<string, string> ?translations = new Dictionary<string, string>();
        private Dictionary<string, string> ?inverseTranslations = new Dictionary<string, string>(); // to translate back to the original language

        /// <summary>
        /// Initializes a new instance of the <see cref="Translator"/> class, by fetching previous translations.
        /// </summary>
        public Translator() {
            // if there are no translations yet
            if (!File.Exists(translationsPath)) {
                File.WriteAllText(translationsPath, "{}");
                translations = new Dictionary<string, string>();
                inverseTranslations = new Dictionary<string, string>();
                return;
            }

            translations = JsonConvert.DeserializeObject<Dictionary<string, string>>(File.ReadAllText(translationsPath));
            inverseTranslations = translations!.ToDictionary(x => x.Value, x => x.Key);
        }

        /// <summary>
        /// Learns a new word by recording its translation in short-term and long-term memory.
        /// </summary>
        /// <param name="word">The word a different language.</param>
        /// <param name="translation">The meaning of the word translated to known language (likely English).</param>
        public void Learn(string word, string translation) {
            translations![word] = translation;
            File.WriteAllText(translationsPath, JsonConvert.SerializeObject(translations));
        }

        /// <summary>
        /// Translates a word from a different language to the known language.
        /// </summary>
        /// <param name="word"></param>
        /// <returns>The translation or null if unknown.</returns>        
        public string? Translate(string word) {
            return translations!.ContainsKey(word) ? translations![word] : null;
        }

        /// <summary>
        /// Translates a word from the known language to a different language.
        /// </summary>
        /// <param name="translation"></param>
        /// <returns>The original word or null if unknown.</returns>
        public string? TranslateBack(string translation) {
            return inverseTranslations!.ContainsKey(translation) ? inverseTranslations![translation] : null;
        }

        /// <summary>
        /// Forgets all translations in short-term and long-term memory.
        /// </summary>
        /// <remarks>Only for testing purposes.</remarks>
        internal void ForgetAll() {
            translations = new Dictionary<string, string>();
            inverseTranslations = new Dictionary<string, string>();
            File.WriteAllText(translationsPath, "{}");
        }
    }
}