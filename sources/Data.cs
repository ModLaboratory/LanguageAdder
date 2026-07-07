using HarmonyLib;
using Il2CppSystem.Linq;
using Newtonsoft.Json;
using Newtonsoft.Json.Linq;
using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using UnityEngine.SceneManagement;

namespace LanguageAdder
{
    public class Data
    {
        /// <summary>
        /// Path to your game root directory.
        /// </summary>
        public static string GamePath => Directory.GetCurrentDirectory();
        /// <summary>
        /// The name of language data folder.
        /// </summary>
        public const string DataFolderName = "Language_Data";
        /// <summary>
        /// Path to the language data folder.
        /// </summary>
        public static string DataFolderPath => $@"{GamePath}\{DataFolderName}";
        public static string ExampleLangFileName => $"{TranslationController.Instance.currentLanguage.languageID}_Example.lang";
        public static string ExampleLangFilePath => $@"{DataFolderPath}\{ExampleLangFileName}";
        public const string RegisteredLangFileName = "Languages.json";
        public static string RegisteredLangFilePath => $@"{DataFolderPath}\{RegisteredLangFileName}";
        public const string LastLanguageFileName = "LastLanguage.dat";
        public static string LastLanguageFilePath => $@"{DataFolderPath}\{LastLanguageFileName}";

        /// <summary>
        /// Get current custom language ID.
        /// </summary>
        public static int CurrentCustomLanguageId { get; set; } = int.MinValue;
        public static bool IsUsingCustomLanguage
        {
            get => CurrentCustomLanguageId != int.MinValue;
            set
            {
                if (!value)
                {
                    CurrentCustomLanguageId = int.MinValue;
                    Main.Logger.LogInfo($"Manually set {nameof(IsUsingCustomLanguage)} to false");
                }
                else
                {
                    Main.Logger.LogWarning($"No need to set {nameof(IsUsingCustomLanguage)} to true");
                }
            }
        }
        public static CustomLanguage CurrentCustomLanguage => CustomLanguage.GetCustomLanguageById(CurrentCustomLanguageId);

        public static JObject LanguageRoot { get; set; } = new();
        public static JArray ReplacementRoot { get; set; } = new();

        public static CustomLanguage LastCustomLanguage
        {
            get
            {
                int customLanguageId = -1;
                CustomLanguage lastCustomLang = null;

                if (File.Exists(LastLanguageFilePath))
                {
                    try
                    {
                        customLanguageId = int.Parse(File.ReadAllText(LastLanguageFilePath));
                    }
                    catch (Exception e)
                    {
                        Main.Logger.LogError("Error reading last custom language: " + e);
                    }

                    lastCustomLang = CustomLanguage.GetCustomLanguageById(customLanguageId);
                }
                else
                {
                    File.WriteAllText(LastLanguageFilePath, CurrentCustomLanguageId.ToString());
                }

                return IsUsingCustomLanguage ? CurrentCustomLanguage : lastCustomLang;
            }
            set
            {
                try
                {
                    File.WriteAllText(LastLanguageFilePath, value.LanguageId.ToString());
                }
                catch (Exception e)
                {
                    Main.Logger.LogError("Error saving last custom language: " + e);
                }
            }
        }

        public static void GenerateCurrentLanguageExampleFile()
        {
            JObject root = new();

            foreach (var stringName in Enum.GetValues<StringNames>())
            {
                var key = stringName.ToString();
                var value = TranslationController.Instance.GetString(stringName);

                if (value == "STRMISS")
                    value = ""; // Let the game proceed missing strings

                root[key] = value;
            }

            var json = root.ToString(Formatting.Indented);
            File.WriteAllText(ExampleLangFilePath, json);
        }

        public static void LoadCustomLanguages()
        {
            if (IsUsingCustomLanguage)
            {
                TranslationController.Instance.SetLanguage(CurrentCustomLanguage.BaseLanguage);
                IsUsingCustomLanguage = false;
            }

            if (CustomLanguage.AllLanguages.Count != 0)
            {
                var instance = Object.FindObjectOfType<LanguageSetter>(true);

                var btns = instance ? new List<LanguageButton>(instance.AllButtons) : null;

                CustomLanguage.AllLanguages.ForEach(l => // Exception
                {
                    if (instance) btns.Remove(l.LanguageButton);
                    CustomLanguage.AllLanguages.Remove(l);
                    if (l.LanguageButton) Object.Destroy(l.LanguageButton.gameObject);
                });

                if (instance) instance.AllButtons = btns.ToArray();
            }

            if (!File.Exists(RegisteredLangFilePath) || !Directory.Exists(DataFolderPath))
            {
                Main.Logger.LogError("Error reading file(s): Not exist.");
                return;
            }

            var languagesJson = File.ReadAllText(RegisteredLangFilePath);

            if (languagesJson.IsNullOrWhiteSpace()) return;

            var root = JObject.Parse(languagesJson);

            var properties = root.Properties().ToList();

            foreach (var property in properties)
            {
                var name = property.Name;

                try
                {
                    var path = property.Value["path"].ToString();
                    var @base = property.Value["base"].ToString();
                    var forceReplacementConfigPath = "";

                    try
                    {
                        forceReplacementConfigPath = property.Value["forceReplacementConfigPath"].ToString();
                    }
                    catch
                    {
                    }

                    if (!Enum.TryParse<SupportedLangs>(@base, out var baseLang))
                        throw new InvalidDataException($"Invalid {nameof(baseLang)}: {baseLang}");

                    _ = new CustomLanguage(name, path, baseLang, forceReplacementConfigPath);
                }
                catch (Exception e)
                {
                    Main.Logger.LogError("Invalid language registry for: " + name + " with " + e);
                    continue;
                }
            }

            SetCustomLanguage(LastCustomLanguage);

            if (SceneManager.GetActiveScene().name == Constants.MAIN_MENU_SCENE)
                SceneManager.LoadScene(Constants.MAIN_MENU_SCENE); // Reload the main menu
        }

        public static void SaveLastLanguage(CustomLanguage lang) => LastCustomLanguage = lang;

        public static void SetCustomLanguage(CustomLanguage customLanguage)
        {
            CurrentCustomLanguageId = customLanguage.LanguageId;
            var langButton = customLanguage.LanguageButton;
            var langSetter = Object.FindObjectOfType<LanguageSetter>(true);

            if (langSetter)
                langSetter.SetLanguage(langButton);
            else
                Main.Logger.LogWarning("Unable to find an instance of " + nameof(LanguageSetter));

            TranslationController.Instance.SetLanguage(customLanguage.BaseLanguage);

            var fullTranslations = File.ReadAllText(CustomLanguage.GetCustomLanguageById(CurrentCustomLanguageId).FilePath);
            LanguageRoot = JObject.Parse(fullTranslations);

            if (customLanguage.ForceTextReplacementEnabled)
            {
                var replacementRuleConfig = File.ReadAllText(customLanguage.ForceReplacementConfigPath);
                ReplacementRoot = JsonConvert.DeserializeObject<JArray>(replacementRuleConfig);
            }
            
            var menu = Object.FindObjectOfType<SettingsLanguageMenu>(true);

            if (langButton && langButton.Title)
            {
                if (langSetter && langSetter.parentLangButton)
                {
                    langSetter.parentLangButton.text = langButton.Title.text;
                }
                else
                {
                    Main.Logger.LogWarning("Null: " + nameof(LanguageSetter.parentLangButton));
                    if (menu && menu.selectedLangText)
                    {
                        menu.selectedLangText.text = langButton.Title.text;
                    }
                }
            }

            TranslationController.Instance.ActiveTexts.ToArray().Do(t => t.ResetText()); // Refresh texts

            Main.Logger.LogInfo($"Changed custom language to {CustomLanguage.GetCustomLanguageById(CurrentCustomLanguageId).LanguageName} (Base language: {CustomLanguage.GetCustomLanguageById(CurrentCustomLanguageId).BaseLanguage})");
            SaveLastLanguage(customLanguage);
        }
    }

    public class CustomLanguage
    {
        public static List<CustomLanguage> AllLanguages { get; private set; } = new();

        public string LanguageName { get; init; }
        public string FilePath { get; init; }
        public SupportedLangs BaseLanguage { get; init; }
        public string ForceReplacementConfigPath { get; init; }
        public bool ForceTextReplacementEnabled => !ForceReplacementConfigPath.IsNullOrWhiteSpace();

        public int LanguageId { get; init; }
        public LanguageButton LanguageButton { get; internal set; }

        public CustomLanguage(string languageName, string filePath, SupportedLangs baseLanguage, string forceReplacementConfigPath = "")
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
    }
}
