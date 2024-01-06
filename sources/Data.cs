using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Text.RegularExpressions;
using TMPro;
using static LanguageAdder.Main;

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
        public static string DataFolderName => "Language_Data";
        /// <summary>
        /// Path to the language data folder.
        /// </summary>
        public static string DataFolderPath => $@"{GamePath}\{DataFolderName}";
        public static string ExampleLangFileName => $"{TranslationController.Instance.currentLanguage.languageID}_Example.lang";
        public static string ExampleLangFilePath => $@"{DataFolderPath}\{ExampleLangFileName}";
        public static string RegisterLangFileName => "language.dat";
        public static string RegisterLangFilePath => $@"{DataFolderPath}\{RegisterLangFileName}";
        public static string LastLanguageFileName => "lastLanguage.dat";
        public static string LastLanguageFilePath => $@"{DataFolderPath}\{LastLanguageFileName}";

        /// <summary>
        /// Get current custom language ID.
        /// </summary>
        public static int CurrentCustomLanguageId { get; set; } = int.MinValue;
        public static CustomLanguage LastCustomLanguage
        {
            get
            {
                CustomLanguage lastCustomLang = null;
                if (File.Exists(LastLanguageFilePath))
                {
                    try
                    {
                        using StreamReader reader = File.OpenText(LastLanguageFilePath);
                        lastCustomLang = CustomLanguage.GetCustomLanguageById(int.Parse(reader.ReadLine()));
                    }
                    catch (Exception e)
                    {
                        Log.LogError("Error reading last custom language: " + e);
                    }
                }
                else
                {
                    using StreamWriter writer = File.CreateText(LastLanguageFilePath);
                    writer.WriteLine(CurrentCustomLanguageId);
                }
                return CurrentCustomLanguageId == int.MinValue ? lastCustomLang : CustomLanguage.GetCustomLanguageById(CurrentCustomLanguageId);
            }
            set
            {
                try
                {
                    using StreamWriter writer = File.CreateText(LastLanguageFilePath);
                    writer.WriteLine(value.LanguageId);
                }
                catch (Exception e)
                {
                    Log.LogError("Error saving last custom language: " + e);
                }
            }
        }


        public static void GenerateCurrentLanguageExampleFile()
        {
            if (File.Exists(ExampleLangFilePath)) return;

            using StreamWriter writer = File.CreateText(ExampleLangFilePath);
            foreach (var stringName in Enum.GetValues<StringNames>()) writer.WriteLine(stringName.ToString() + "\t" + $@"{Regex.Replace(TranslationController.Instance.GetString(stringName), @"\r?\n", @"\r\n")}");
        }

        public static void LoadCustomLanguages()
        {
            if (CustomLanguage.CustomLanguages.Count != 0)
            {
                LanguageSetter instance = DestroyableSingleton<LanguageSetter>.Instance;

                var btns = instance ? new List<LanguageButton>(instance.AllButtons) : null;

                CustomLanguage.CustomLanguages.ForEach(l =>
                {
                    if (instance) btns.Remove(l.LanguageButton);
                    CustomLanguage.CustomLanguages.Remove(l);
                    if (l.LanguageButton) UnityEngine.Object.Destroy(l.LanguageButton.gameObject);
                });

                if (instance) instance.AllButtons = btns.ToArray();
            }

            if (!File.Exists(RegisterLangFilePath) || !Directory.Exists(DataFolderPath))
            {
                Log.LogError("Error reading file(s): Not exist.");
                return;
            }
            using StreamReader reader = File.OpenText(RegisterLangFilePath);
            List<string> langFiles = new();
            string line;
            int count = 0;
            while ((line = reader.ReadLine()) != null)
            {
                count++;
                if (line.StartsWith('#')) continue;

                string[] args = line.Split('\t');
                if (args.Length != 3)
                {
                    Log.LogError($"Error reading register file at line {count}: Format error.");
                    continue;
                }

                int langId = 0;
                SupportedLangs? lang = null;
                try
                {
                    langId = int.Parse(args[2]);
                    lang = (SupportedLangs)langId;
                }
                catch (Exception e)
                {
                    Log.LogError($"Error reading register file at line {count}: Incorrect language id!\r\n{e}");
                    continue;
                }

                _ = new CustomLanguage(args[0], $@"{DataFolderPath}\{args[1]}", lang.Value);
            }
        }

        public static void SaveLastLanguage(CustomLanguage lang) => LastCustomLanguage = lang;

        public static void SetCustomLanguage(CustomLanguage customLanguage)
        {
            CurrentCustomLanguageId = customLanguage.LanguageId;
            var langButton = customLanguage.LanguageButton;

            DestroyableSingleton<LanguageSetter>.Instance.SetLanguage(langButton);
            TranslationController.Instance.SetLanguage(langButton.Language);
            UpdateStrings();

            DestroyableSingleton<SettingsLanguageMenu>.Instance.transform.Find("Text_TMP").GetComponent<TextMeshPro>().text = langButton.Title.text;

            Log.LogInfo($"Changed custom language to {langButton.Title.text} (Base language: {langButton.Language.Name})");
            SaveLastLanguage(customLanguage);
        }

        public static void UpdateStrings()
        {
            if (CurrentCustomLanguageId == int.MinValue) return;

            using StreamReader reader = File.OpenText(CustomLanguage.GetCustomLanguageById(Data.CurrentCustomLanguageId).FilePath);
            string line;

            while ((line = reader.ReadLine()) != null)
            {
                if (line.StartsWith('#')) continue;
                string[] translationKeyValue = line.Split('\t');
                string valueStr;
                string key = translationKeyValue[0];
                StringNames id;

                if (Enum.TryParse(key, out id))
                {
                    if (translationKeyValue.Length == 2)
                    {
                        valueStr = translationKeyValue[1].Replace("\\r", "\r").Replace("\\n", "\n");

                        TranslationController.Instance.currentLanguage.AllStrings[id.ToString()] = valueStr;

                        continue;
                    }
                    break;
                }
            }
        }

    }

    public class CustomLanguage
    {
        public static List<CustomLanguage> CustomLanguages { get; private set; } = new();

        public string LanguageName { get; init; }
        public string FilePath { get; init; }
        public SupportedLangs BaseLanguage { get; init; }
        public int LanguageId { get; init; }
        public LanguageButton LanguageButton { get; internal set; }

        public CustomLanguage(string languageName, string filePath, SupportedLangs baseLanguage)
        {
            LanguageName = languageName;
            FilePath = filePath;
            BaseLanguage = baseLanguage;
            LanguageId = (CustomLanguages.LastOrDefault() ?? (int)Enum.GetValues<SupportedLangs>().ToList().LastOrDefault()) + 1;
            CustomLanguages.Add(this);
            Log.LogInfo($"Language registered: {LanguageName} {FilePath} {BaseLanguage.ToString()}: {LanguageId}");
        }

        public static CustomLanguage GetCustomLanguageById(int id) => CustomLanguages.Where(l => l.LanguageId == id).FirstOrDefault();

        public static implicit operator int(CustomLanguage l) => l.LanguageId;
    }
}
