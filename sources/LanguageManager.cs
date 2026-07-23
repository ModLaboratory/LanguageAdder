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

        private const string JsonMemberVersionName = "version";
        private const string JsonMemberLastLanguageName = "lastLanguage";

        public static void RecordLastCustomLanguage(CustomLanguage language)
        {
            var jsonRoot = new JObject();

            jsonRoot[JsonMemberVersionName] = PluginInfo.PLUGIN_VERSION;
            jsonRoot[JsonMemberLastLanguageName] = language?.LanguageName ?? "";

            File.WriteAllText(ModConstants.LastLanguageFilePath, jsonRoot.ToString(Formatting.Indented));
        }

        public static CustomLanguage ReadLastCustomLanguage()
        {
            if (File.Exists(ModConstants.LastLanguageFilePath))
            {
                var content = File.ReadAllText(ModConstants.LastLanguageFilePath);

                if (int.TryParse(content, out var id))
                {
                    var legacyLastLanguage = CustomLanguage.GetCustomLanguageById(id);

                    RecordLastCustomLanguage(legacyLastLanguage);

                    return legacyLastLanguage;
                }
                else
                {
                    try
                    {
                        var jsonRoot = JObject.Parse(content);
                        var configVersionString = jsonRoot[JsonMemberVersionName].ToString();
                        var lastLanguageName = jsonRoot[JsonMemberLastLanguageName].ToString();

                        if (lastLanguageName.IsNullOrWhiteSpace())
                            return null;

                        if (Version.TryParse(configVersionString, out var configVersion))
                        {
                            if (configVersion > PluginInfo.ModVersion)
                                Main.Logger.LogWarning($"Last language config file comes from a newer version ({configVersionString}) !");
                        }
                        else
                        {
                            Main.Logger.LogWarning($"{ModConstants.LastLanguageFileName} file has an invalid version string: {configVersionString}");
                        }

                        return CustomLanguage.AllLanguages.FirstOrDefault(language => language.LanguageName == lastLanguageName);
                    }
                    catch
                    {
                        RecordLastCustomLanguage(null); // Override invalid JSON

                        return null;
                    }
                }
            }

            return null;
        }

        public static void GenerateCurrentLanguageExampleFile()
        {
            var jsonRoot = new JObject();

            foreach (var stringName in Enum.GetValues<StringNames>())
            {
                var key = stringName.ToString();
                var value = TranslationController.Instance.GetString(stringName);

                if (value == "STRMISS")
                    value = ""; // Let the game proceed missing strings while they're being read

                jsonRoot[key] = value;
            }

            var json = jsonRoot.ToString(Formatting.Indented);
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
            try
            {
                Directory.CreateDirectory(ModConstants.DataFolderPath);
            }
            catch (Exception e)
            {
                Main.Logger.LogError($"Error creating data folder: {ModConstants.DataFolderPath}\r\n{e}");
                return;
            }

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
            var success = false;

            if (lastCustomLanguage != null)
                success = SetCustomLanguage(lastCustomLanguage);

            if (SceneManager.GetActiveScene().name == Constants.MAIN_MENU_SCENE && success)
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

        public static bool SetCustomLanguage(CustomLanguage customLanguage)
        {
            var wasUsingCustomLanguage = IsUsingCustomLanguage;

            _currentCustomLanguageId = customLanguage.LanguageId;

            var languageButton = customLanguage.LanguageButton;
            var languageSetter = Object.FindObjectOfType<LanguageSetter>(true);

            if (languageSetter)
                languageSetter.SetLanguage(languageButton);
            else
                Main.Logger.LogWarning("Unable to find an instance of " + nameof(LanguageSetter));

            var cachedLanguage = SupportedLangs.English;

            if (!wasUsingCustomLanguage)
                cachedLanguage = CurrentGameLanguage;

            var fullTranslations = File.ReadAllText(CurrentCustomLanguage.TranslationFilePath);

            try
            {
                LanguageRoot = JObject.Parse(fullTranslations);
            }
            catch (Exception e)
            {
                Main.Logger.LogError($"The provided {ModConstants.TranslationDataFileName} is an invalid JSON! Details: {e}");

                FallBackToVanillaLanguage();
                CloseMenus();
                DisplayPopup(FormatErrorMessage(ModConstants.TranslationDataFileName));

                return false;
            }

            ClearReplacementConfigCaches();

            if (customLanguage.ForceTextReplacementEnabled)
            {
                var replacementRuleConfig = File.ReadAllText(customLanguage.ForceReplacementConfigPath);

                try
                {
                    CacheReplacementConfig(JsonConvert.DeserializeObject<JArray>(replacementRuleConfig));
                }
                catch (Exception e)
                {
                    Main.Logger.LogError($"The provided {ModConstants.CustomReplacementRuleFileName} is an invalid JSON! Details: {e}");

                    CloseMenus();
                    DisplayPopup(FormatErrorMessage(ModConstants.CustomReplacementRuleFileName));
                }
            }

            void CloseMenus()
            {
                var clientOptionsMenu = Object.FindObjectOfType<OptionsMenuBehaviour>(true);

                if (clientOptionsMenu)
                    clientOptionsMenu.Close();

                if (languageSetter)
                    languageSetter.Close();
            }

            void FallBackToVanillaLanguage()
            {
                Patch.VanillaLanguageButtons.FirstOrDefault(button => button && button.Language.languageID == cachedLanguage).Button.OnClick.Invoke();

                IsUsingCustomLanguage = false;
            }

            TranslationController.Instance.SetLanguage(customLanguage.BaseLanguage);

            // Apply language change to UI
            if (languageButton && languageButton.Title)
            {
                if (languageSetter && languageSetter.parentLangButton)
                {
                    languageSetter.parentLangButton.text = languageButton.Title.text;

                    languageSetter.AllButtons.ToArray().Do(button => button.Title.color = Color.white);
                    languageButton.Title.color = Color.green;
                }
            }

            TranslationController.Instance.ActiveTexts.ToArray().Do(t => t.ResetText()); // Refresh texts

            Main.Logger.LogInfo($"Changed custom language to {customLanguage.LanguageName} (Base language: {CustomLanguage.GetCustomLanguageById(_currentCustomLanguageId).BaseLanguage})");
            
            RecordLastCustomLanguage(customLanguage);

            return true;

            string FormatErrorMessage(string fileName)
            {
                switch (cachedLanguage)
                {
                    case SupportedLangs.SChinese:
                        return $"您想要设置的自定义语言的 {fileName} 的 JSON 格式无效。";
                    case SupportedLangs.TChinese:
                        return $"您想要設定的自訂語言 {fileName} 其 JSON 格式無效。";
                    case SupportedLangs.English:
                    default:
                        return $"The JSON format of {fileName} of the custom language you want to set is invalid.";
                }
            }
        }

        public static void DisplayPopup(string message)
        {
            if (DisconnectPopup.InstanceExists && !GameManager.Instance)
                DisconnectPopup.Instance.ShowCustom(message);
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

        private static readonly int MaxVanillaLanguageId = Enum.GetValues<SupportedLangs>().Select(l => (int)l).Max() + 1;

        public string LanguageName { get; init; }
        public string TranslationFilePath { get; init; }
        [Obsolete("Unnecessary now. It is English by default.")] public SupportedLangs BaseLanguage { get; init; }
        public string ForceReplacementConfigPath { get; init; }
        public bool ForceTextReplacementEnabled => !ForceReplacementConfigPath.IsNullOrWhiteSpace();

        public int LanguageId { get; init; }
        public LanguageButton LanguageButton { get; internal set; }

        public CustomLanguage(string languageName, string filePath, SupportedLangs baseLanguage = SupportedLangs.English, string forceReplacementConfigPath = "")
        {
            LanguageName = languageName;
            TranslationFilePath = filePath;
            BaseLanguage = baseLanguage;
            ForceReplacementConfigPath = forceReplacementConfigPath;
            LanguageId = AssignNewId();

            AllLanguages.Add(this);

            Main.Logger.LogInfo($"Language registered: {LanguageName} {TranslationFilePath} {BaseLanguage.ToString()}: {LanguageId} {forceReplacementConfigPath}");
        }

        private static int AssignNewId()
        {
            if (AllLanguages.Any())
                return AllLanguages.Select(l => l.LanguageId).Max() + 1;
            else
                return MaxVanillaLanguageId;
        }

        public static CustomLanguage GetCustomLanguageById(int id) => AllLanguages.FirstOrDefault(l => l.LanguageId == id);

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
