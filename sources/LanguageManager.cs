#pragma warning disable CS0618
using HarmonyLib;
using Il2CppSystem.Linq;
using Newtonsoft.Json;
using Newtonsoft.Json.Linq;
using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Text.RegularExpressions;
using UnityEngine;
using UnityEngine.SceneManagement;

namespace LanguageAdder
{
    public class LanguageManager
    {
        private static int _currentCustomLanguageId = int.MinValue;
        public static CustomLanguage CurrentCustomLanguage => CustomLanguage.GetCustomLanguageById(_currentCustomLanguageId);

        public static bool IsUsingCustomLanguage
        {
            get => _currentCustomLanguageId != int.MinValue;
            set
            {
                if (!value)
                {
                    _currentCustomLanguageId = int.MinValue;
                    Main.Logger.LogInfo($"Manually set {nameof(IsUsingCustomLanguage)} to false");
                }
                else
                {
                    Main.Logger.LogWarning($"No need to set {nameof(IsUsingCustomLanguage)} to true");
                }
            }
        }

        public static JObject LanguageRoot { get; set; } = new();
        public static List<(Regex Regex, string Replacement)> RegexReplacementConfigs { get; } = new();
        public static Dictionary<string, string> NonRegexReplacementConfigs { get; } = new();

        public static SupportedLangs CurrentGameLanguage => TranslationController.Instance.currentLanguage.languageID;


        public static void RecordLastCustomLanguage(CustomLanguage language)
        {
            File.WriteAllText(ModConstants.LastLanguageFilePath, language?.LanguageId.ToString() ?? "");
        }

        public static CustomLanguage ReadLastCustomLanguage()
        {
            if (File.Exists(ModConstants.LastLanguageFilePath))
                if (int.TryParse(File.ReadAllText(ModConstants.LastLanguageFilePath), out var id))
                    return CustomLanguage.GetCustomLanguageById(id);

            return null;
        }

        public static void GenerateCurrentLanguageExampleFile()
        {
            JObject root = new();

            foreach (var stringName in Enum.GetValues<StringNames>())
            {
                var key = stringName.ToString();
                var value = TranslationController.Instance.GetString(stringName);

                if (value == "STRMISS")
                    value = ""; // Let the game proceed missing strings while they're being read

                root[key] = value;
            }

            var json = root.ToString(Formatting.Indented);
            var completeFolderPath = Path.Combine(ModConstants.DataFolderPath, "@" + CurrentGameLanguage.ToString() + "_Example");
            var completePath = Path.Combine(completeFolderPath, ModConstants.TranslationDataFileName);

            try 
            {
                Directory.CreateDirectory(completeFolderPath);
                File.WriteAllText(completePath, json);
            }
            catch (Exception e)
            {
                Main.Logger.LogError($"Failed to generate example language file to {completePath}: {e}");
            }
        }

        public static void LoadCustomLanguages()
        {
            if (IsUsingCustomLanguage)
            {
                TranslationController.Instance.SetLanguage(CurrentCustomLanguage.BaseLanguage);

                IsUsingCustomLanguage = false;
            }

            ClearReplacementConfigCaches();

            if (CustomLanguage.AllLanguages.Count != 0)
            {
                var instance = Object.FindObjectOfType<LanguageSetter>(true);

                List<LanguageButton> buttons = null;

                if (instance && instance.AllButtons != null)
                    buttons = instance.AllButtons.ToList();

                var originalList = CustomLanguage.AllLanguages.ToList();

                CustomLanguage.AllLanguages.ForEach(language =>
                {
                    if (language.LanguageButton)
                    {
                        buttons?.Remove(language.LanguageButton);
                        Object.Destroy(language.LanguageButton.gameObject);
                    }
                });

                CustomLanguage.AllLanguages.Clear();

                if (instance && buttons != null)
                    instance.AllButtons = buttons.ToArray();
            }

            if (File.Exists(ModConstants.LegacyRegisteredLanguageFilePath))
            { 
                Main.Logger.LogInfo("Legacy language registry found. Migrating to the new format...");

                MigrateLegacyLanguageConfigs();
            }
            
            foreach (var folderPath in Directory.EnumerateDirectories(ModConstants.DataFolderPath))
            {
                var languageName = new DirectoryInfo(folderPath).Name;

                if (languageName.StartsWith('@'))
                    continue;

                var translationDataFilePath = Path.Combine(folderPath, ModConstants.TranslationDataFileName);
                var replacementRuleConfigFilePath = Path.Combine(folderPath, ModConstants.CustomReplacementRuleFileName);

                if (!File.Exists(translationDataFilePath))
                {
                    Main.Logger.LogError($"Failed to load custom language {languageName} for not finding the translation data file {translationDataFilePath}");
                    continue;
                }

                if (!File.Exists(replacementRuleConfigFilePath))
                    replacementRuleConfigFilePath = "";

                _ = new CustomLanguage(languageName, translationDataFilePath, forceReplacementConfigPath: replacementRuleConfigFilePath);
            }

            var lastCustomLanguage = ReadLastCustomLanguage();
            if (lastCustomLanguage != null)
                SetCustomLanguage(lastCustomLanguage);

            if (SceneManager.GetActiveScene().name == Constants.MAIN_MENU_SCENE)
                SceneManager.LoadScene(Constants.MAIN_MENU_SCENE); // Reload the main menu
        }

        public static void MigrateLegacyLanguageConfigs()
        {
            var languagesJson = File.ReadAllText(ModConstants.LegacyRegisteredLanguageFilePath);
            var root = JObject.Parse(languagesJson);
            var properties = root.Properties().ToList();

            var hasError = false;

            foreach (var property in properties)
            {
                var languageName = property.Name;

                try
                {
                    var originTranslationDataPath = property.Value["path"].ToString();
                    var forceReplacementConfigPath = "";

                    try
                    {
                        forceReplacementConfigPath = property.Value["forceReplacementConfigPath"].ToString();
                    }
                    catch
                    {
                    }
                    
                    var newLanguageFolderPath = Directory.CreateDirectory(Path.Combine(ModConstants.DataFolderPath, languageName)).FullName;
                    var newTranslationDataPath = Path.Combine(newLanguageFolderPath, ModConstants.TranslationDataFileName);
                    var newReplacementConfigPath = Path.Combine(newLanguageFolderPath, ModConstants.CustomReplacementRuleFileName);

                    File.Move(originTranslationDataPath, newTranslationDataPath);

                    if (File.Exists(forceReplacementConfigPath))
                        File.Move(forceReplacementConfigPath, newReplacementConfigPath);
                }
                catch (Exception e)
                {
                    hasError = true;

                    Main.Logger.LogError("Invalid language registry while migrating for: " + languageName + " with " + e);
                    continue;
                }
            }

            try
            {
                File.Move(ModConstants.LegacyRegisteredLanguageFilePath, ModConstants.LegacyRegisteredLanguageFilePath + ".old");
            }
            catch (Exception e)
            {
                hasError = true;

                Main.Logger.LogError("Failed to rename legacy language registry file. The migration will repeat on next startup and may lead to exceptional results! Exception: " + e);
            }

            if (hasError)
                Main.Logger.LogWarning("Legacy language registry migration completed with errors! This might lead to exceptional results.");
            else
                Main.Logger.LogInfo("Legacy language registry migration completed successfully!");
        }

        [Obsolete("Legacy language registry could be migrated to the new format by calling MigrateLegacyLanguageConfigs(). LoadCustomLanguages() would also call MigrateLegacyLanguageConfigs() to migrate legacy language configurations.")]
        public static void LegacyLoadCustomLanguages()
        {
            var languagesJson = File.ReadAllText(ModConstants.LegacyRegisteredLanguageFilePath);
            var root = JObject.Parse(languagesJson);
            var properties = root.Properties().ToList();

            foreach (var property in properties)
            {
                var name = property.Name;

                try
                {
                    var path = property.Value["path"].ToString();
                    var @base = ""; // Now member "base" is obsolete and becomes optional, default to English if not specified
                    var forceReplacementConfigPath = "";

                    try
                    {
                        @base = property.Value["base"].ToString();
                    }
                    catch
                    {
                    }

                    try
                    {
                        forceReplacementConfigPath = property.Value["forceReplacementConfigPath"].ToString();
                    }
                    catch
                    {
                    }

                    if (!Enum.TryParse<SupportedLangs>(@base, out var baseLang))
                        baseLang = SupportedLangs.English;

                    _ = new CustomLanguage(name, path, baseLang, forceReplacementConfigPath);
                }
                catch (Exception e)
                {
                    Main.Logger.LogError("Invalid language registry for: " + name + " with " + e);
                    continue;
                }
            }

            var lastCustomLanguage = ReadLastCustomLanguage();
            if (lastCustomLanguage != null)
                SetCustomLanguage(lastCustomLanguage);

            if (SceneManager.GetActiveScene().name == Constants.MAIN_MENU_SCENE)
                SceneManager.LoadScene(Constants.MAIN_MENU_SCENE); // Reload the main menu
        }

        public static void SetCustomLanguage(CustomLanguage customLanguage)
        {
            _currentCustomLanguageId = customLanguage.LanguageId;
            var langButton = customLanguage.LanguageButton;
            var langSetter = Object.FindObjectOfType<LanguageSetter>(true);

            if (langSetter)
                langSetter.SetLanguage(langButton);
            else
                Main.Logger.LogWarning("Unable to find an instance of " + nameof(LanguageSetter));

            TranslationController.Instance.SetLanguage(customLanguage.BaseLanguage);

            var fullTranslations = File.ReadAllText(CurrentCustomLanguage.FilePath);
            LanguageRoot = JObject.Parse(fullTranslations);

            ClearReplacementConfigCaches();

            if (customLanguage.ForceTextReplacementEnabled)
            {
                var replacementRuleConfig = File.ReadAllText(customLanguage.ForceReplacementConfigPath);
                CacheReplacementConfig(JsonConvert.DeserializeObject<JArray>(replacementRuleConfig));
            }
            
            // Apply language change to UI
            var menu = Object.FindObjectOfType<SettingsLanguageMenu>(true);

            if (langButton && langButton.Title)
            {
                if (langSetter && langSetter.parentLangButton)
                {
                    langSetter.parentLangButton.text = langButton.Title.text;

                    langSetter.AllButtons.ToArray().Do(button => button.Title.color = Color.white);
                    langButton.Title.color = Color.green;
                }
            }

            TranslationController.Instance.ActiveTexts.ToArray().Do(t => t.ResetText()); // Refresh texts

            Main.Logger.LogInfo($"Changed custom language to {customLanguage.LanguageName} (Base language: {CustomLanguage.GetCustomLanguageById(_currentCustomLanguageId).BaseLanguage})");
            
            RecordLastCustomLanguage(customLanguage);
        }

        private static void CacheReplacementConfig(JArray config)
        {
            ClearReplacementConfigCaches();

            foreach (var item in config._values)
            {
                var jObject = item.Cast<JObject>();
                var key = jObject["key"].ToString(); // pattern for regex
                var value = jObject["value"].ToString(); // replacement for regex
                var usingRegex = false;

                try
                {
                    usingRegex = (bool)jObject["isRegex"];
                }
                catch
                {
                }

                if (usingRegex)
                    RegexReplacementConfigs.Add((new Regex(key, RegexOptions.Compiled), value));
                else
                    NonRegexReplacementConfigs.Add(key, value);
            }
        }

        private static void ClearReplacementConfigCaches()
        {
            RegexReplacementConfigs.Clear();
            NonRegexReplacementConfigs.Clear();
        }
    }


    public class CustomLanguage
    {
        public static List<CustomLanguage> AllLanguages { get; private set; } = new();

        public string LanguageName { get; init; }
        public string FilePath { get; init; }
        [Obsolete("Unnecessary now. It is English by default.")] public SupportedLangs BaseLanguage { get; init; }
        public string ForceReplacementConfigPath { get; init; }
        public bool ForceTextReplacementEnabled => !ForceReplacementConfigPath.IsNullOrWhiteSpace();

        public int LanguageId { get; init; }
        public LanguageButton LanguageButton { get; internal set; }

        public CustomLanguage(string languageName, string filePath, SupportedLangs baseLanguage = SupportedLangs.English, string forceReplacementConfigPath = "")
        {
            LanguageName = languageName;
            FilePath = filePath;
            BaseLanguage = baseLanguage;
            ForceReplacementConfigPath = forceReplacementConfigPath;
            LanguageId = (AllLanguages.LastOrDefault() ?? (int)Enum.GetValues<SupportedLangs>().ToList().LastOrDefault()) + 1;

            AllLanguages.Add(this);

            Main.Logger.LogInfo($"Language registered: {LanguageName} {FilePath} {BaseLanguage.ToString()}: {LanguageId} {forceReplacementConfigPath}");
        }

        public static CustomLanguage GetCustomLanguageById(int id) => AllLanguages.FirstOrDefault(l => l.LanguageId == id);

        public static implicit operator int(CustomLanguage l) => l.LanguageId;

        public static bool operator ==(CustomLanguage left, CustomLanguage right)
        {
            if (left is null && right is null)
                return true;
            if (left is null || right is null)
                return false;

            return left.Equals(right);
        }

        public static bool operator !=(CustomLanguage left, CustomLanguage right) => !(left == right);

        public override bool Equals(object obj) => obj is CustomLanguage language && language.LanguageId == LanguageId;
        public override int GetHashCode() => LanguageId;
    }
}
#pragma warning restore CS0618
