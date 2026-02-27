using System;
using System.Collections.Generic;
using System.Linq;
using System.Text.RegularExpressions;
using UnityEngine;

namespace Assets.SimpleLocalization.Scripts
{
    /// <summary>
    /// Localization manager.
    /// </summary>
    public static class LocalizationManager
    {

        public static string[] AllLanguages = new string[] { "English", "German", "Russian", "Turkei" };
        public static string[] LanguageCodes = new string[] { "en", "de", "ru", "tr" };

        public static string GetLanguageByCode(string code)
        {
            for(int i = 0; i < LanguageCodes.Length; i++)
            {
                if (LanguageCodes[i] == code)
                    return AllLanguages[i];
            }

            return AllLanguages[0];
        }

        public static string GetLangeageCodeByLanguage(string language)
        {
            for (int i = 0; i < AllLanguages.Length; i++)
            {
                if (AllLanguages[i] == language)
                    return LanguageCodes[i];
            }

            return LanguageCodes[0];
        }


        private static int _languageIndex = 0;

		/// <summary>
		/// Fired when localization changed.
		/// </summary>
        public static event Action OnLocalizationChanged = () => { }; 

        public static Dictionary<string, Dictionary<string, string>> Dictionary = new();
        private static string _language = "English";

		/// <summary>
		/// Get or set language.
		/// </summary>
        public static string Language
        {
            get => _language;
        }

        public static void Load()
        {
            Read();
        }

        public static void SetLanguage(string language)
        {
            _language = language;

            for (int i = 0; i < AllLanguages.Length; i++)
            {
                if (language == AllLanguages[i])
                {
                    _languageIndex = i;
                    break;
                }
            }


            OnLocalizationChanged();
        }

 
        public static void SetNextLanguage()
        {
            _languageIndex++;

            if (_languageIndex == AllLanguages.Length)
                _languageIndex = 0;

            SetLanguage(AllLanguages[_languageIndex]);
        }


        /// <summary>
        /// Read localization spreadsheets.
        /// </summary>
        public static void Read()
        {
            if (Dictionary.Count > 0) return;

            var keys = new List<string>();

            foreach (var sheet in LocalizationSettings.Instance.Sheets)
            {
                var textAsset = sheet.TextAsset;
                var lines = GetLines(textAsset.text);
				var languages = lines[0].Split(',').Select(i => i.Trim()).ToList();

                if (languages.Count != languages.Distinct().Count())
                {
                    Debug.LogError($"Duplicated languages found in `{sheet.Name}`. This sheet is not loaded.");
                    continue;
                }

                for (var i = 1; i < languages.Count; i++)
                {
                    if (!Dictionary.ContainsKey(languages[i]))
                    {
                        Dictionary.Add(languages[i], new Dictionary<string, string>());
                    }
                }

                for (var i = 1; i < lines.Count; i++)
                {
                    var columns = GetColumns(lines[i]);
                    var key = columns[0];

                    if (key == "") continue;

                    if (keys.Contains(key))
                    {
                        Debug.LogError($"Duplicated key `{key}` found in `{sheet.Name}`. This key is not loaded.");
                        continue;
                    }

                    keys.Add(key);

                    for (var j = 1; j < languages.Count; j++)
                    {
                        if (Dictionary[languages[j]].ContainsKey(key))
                        {
                            Debug.LogError($"Duplicated key `{key}` in `{sheet.Name}`.");
                        }
                        else
                        {
                            Dictionary[languages[j]].Add(key, columns[j]);
                        }
                    }
                }
            }
        }

        /// <summary>
        /// Check if a key exists in localization.
        /// </summary>
        public static bool HasKey(string localizationKey)
        {
            return Dictionary.ContainsKey(Language) && Dictionary[Language].ContainsKey(localizationKey);
        }

        /// <summary>
        /// Get localized value by localization key.
        /// </summary>
        public static string Localize(string localizationKey)
        {
            if (Dictionary.Count == 0)
            {
                throw new KeyNotFoundException("Localization not loaded!");

            }

            if (!Dictionary.ContainsKey(Language)) throw new KeyNotFoundException("Language not found: " + Language);

            var missed = !Dictionary[Language].ContainsKey(localizationKey) || Dictionary[Language][localizationKey] == "";

            if (missed)
            {
                Debug.LogWarning($"Translation not found: {localizationKey} ({Language}).");

                return Dictionary["English"].ContainsKey(localizationKey) ? Dictionary["English"][localizationKey] : localizationKey;
            }

            return Dictionary[Language][localizationKey];   
        }

	    /// <summary>
	    /// Get localized value by localization key.
	    /// </summary>
		public static string Localize(string localizationKey, params object[] args)
        {
            var pattern = Localize(localizationKey);

            return string.Format(pattern, args);
        }

        public static List<string> GetLines(string text)
        {
            text = text.Replace("\r\n", "\n").Replace("\"\"", "[_quote_]");
            
            var matches = Regex.Matches(text, "\"[\\s\\S]+?\"");

            foreach (Match match in matches)
            {
                text = text.Replace(match.Value, match.Value.Replace("\"", null).Replace(",", "[_comma_]").Replace("\n", "[_newline_]"));
            }

            // Making uGUI line breaks to work in asian texts.
            text = text.Replace("。", "。 ").Replace("、", "、 ").Replace("：", "： ").Replace("！", "！ ").Replace("（", " （").Replace("）", "） ").Trim();

            return text.Split('\n').Where(i => i != "").ToList();
        }

        public static List<string> GetColumns(string line)
        {
            return line.Split(',').Select(j => j.Trim()).Select(j => j.Replace("[_quote_]", "\"").Replace("[_comma_]", ",").Replace("[_newline_]", "\n")).ToList();
        }
    }
}