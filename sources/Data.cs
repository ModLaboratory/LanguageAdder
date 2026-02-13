using BepInEx.Unity.IL2CPP.Utils.Collections;
using Il2CppSystem.Linq;
using Newtonsoft.Json;
using Newtonsoft.Json.Linq;
using System;
using System.Collections;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Text;
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
        public static string RegisteredLangFileName => "Languages.json";
        public static string RegisteredLangFilePath => $@"{DataFolderPath}\{RegisteredLangFileName}";
        public static string LastLanguageFileName => "LastLanguage.dat";
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
                        Main.Logger.LogError("Error reading last custom language: " + e);
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

                root[key] = value;
            }

            var json = root.ToString(Formatting.Indented);
            File.WriteAllText(ExampleLangFilePath, json);
        }

        public static void LoadCustomLanguages()
        {
            if (CustomLanguage.AllLanguages.Count != 0)
            {
                LanguageSetter instance = DestroyableSingleton<LanguageSetter>.Instance;

                var btns = instance ? new List<LanguageButton>(instance.AllButtons) : null;

                CustomLanguage.AllLanguages.ForEach(l =>
                {
                    if (instance) btns.Remove(l.LanguageButton);
                    CustomLanguage.AllLanguages.Remove(l);
                    if (l.LanguageButton) UnityEngine.Object.Destroy(l.LanguageButton.gameObject);
                });

                if (instance) instance.AllButtons = btns.ToArray();
            }

            if (!File.Exists(RegisteredLangFilePath) || !Directory.Exists(DataFolderPath))
            {
                Main.Logger.LogError("Error reading file(s): Not exist.");
                return;
            }

            var languagesJson = File.ReadAllText(RegisteredLangFilePath);
            var root = JObject.Parse(languagesJson);

            var properties = root.Properties().ToList();

            foreach (var property in properties)
            {
                var name = property.Name;

                try
                {
                    var path = property.Value["path"].ToString();
                    var @base = property.Value["base"].ToString();

                    if (!Enum.TryParse<SupportedLangs>(@base, out var baseLang))
                        throw new InvalidDataException($"Invalid {baseLang}");

                    _ = new CustomLanguage(name, path, baseLang);
                }
                catch (Exception e)
                {
                    Main.Logger.LogError("Invalid language registry for: " + name + "with " + e);
                    continue;
                }
            }

            //using StreamReader reader = File.OpenText(RegisteredLangFilePath);
            //List<string> langFiles = new();
            //string line;
            //int count = 0;
            //while ((line = reader.ReadLine()) != null)
            //{
            //    count++;
            //    if (line.StartsWith('#')) continue;

            //    string[] args = line.Split('\t');
            //    if (args.Length != 3)
            //    {
            //        Log.LogError($"Error reading register file at line {count}: Format error.");
            //        continue;
            //    }

            //    int langId = 0;
            //    SupportedLangs? lang = null;
            //    try
            //    {
            //        langId = int.Parse(args[2]);
            //        lang = (SupportedLangs)langId;
            //    }
            //    catch (Exception e)
            //    {
            //        Log.LogError($"Error reading registry file at line {count}: Incorrect language id!\r\n{e}");
            //        continue;
            //    }

            //    _ = new CustomLanguage(args[0], $@"{DataFolderPath}\{args[1]}", lang.Value);
            //}
        }

        public static void SaveLastLanguage(CustomLanguage lang) => LastCustomLanguage = lang;

        public static void SetCustomLanguage(CustomLanguage customLanguage)
        {
            CurrentCustomLanguageId = customLanguage.LanguageId;
            var langButton = customLanguage.LanguageButton;

            DestroyableSingleton<LanguageSetter>.Instance.SetLanguage(langButton);
            TranslationController.Instance.SetLanguage(customLanguage.BaseLanguage);
            UpdateStrings();

            try
            {
                DestroyableSingleton<SettingsLanguageMenu>.Instance.selectedLangText.text = langButton.Title.text;
            }
            catch (Exception e)
            {
                Main.Logger.LogWarning("Gotcha! " + e);
            }

            Main.Logger.LogInfo($"Changed custom language to {langButton.Title.text} (Base language: {langButton.Language.Name})");
            SaveLastLanguage(customLanguage);
        }

        public static JObject Root { get; set; } = new();

        public static void UpdateStrings()
        {
            if (CurrentCustomLanguageId == int.MinValue) return;

            var fullTranslations = File.ReadAllText(CustomLanguage.GetCustomLanguageById(CurrentCustomLanguageId).FilePath);
            Root = JObject.Parse(fullTranslations);



        //    using StreamReader reader = File.OpenText(CustomLanguage.GetCustomLanguageById(CurrentCustomLanguageId).FilePath);
        //    string line;
        //    StringBuilder log = new();

        //    while ((line = reader.ReadLine()) != null)
        //    {
        //        if (line.StartsWith('#')) continue;
        //        string[] translationKeyValue = line.Split('\t');
        //        string valueStr;
        //        string key = translationKeyValue[0];

        //        if (Enum.TryParse(key, out StringNames id))
        //        {
        //            if (translationKeyValue.Length == 2)
        //            {
        //                valueStr = translationKeyValue[1].Replace("\\r", "\r").Replace("\\n", "\n");

        //                TranslationMap[id.ToString()] = valueStr;

        //                //TranslationController.Instance.currentLanguage.AllStrings[id.ToString()] = valueStr;
        //                //log.AppendLine($"Updated {id} to {valueStr}");
        //                //log.AppendLine($"\tCheck: {TranslationController.Instance.currentLanguage.AllStrings[id.ToString()]}");

        //                continue;
        //            }
        //            break;
        //        }
        //    }

        //    //Log.LogMessage(log.ToString());
        }
    }

    public class CustomLanguage
    {
        public static List<CustomLanguage> AllLanguages { get; private set; } = new();

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
            LanguageId = (AllLanguages.LastOrDefault() ?? (int)Enum.GetValues<SupportedLangs>().ToList().LastOrDefault()) + 1;
            AllLanguages.Add(this);
            Main.Logger.LogInfo($"Language registered: {LanguageName} {FilePath} {BaseLanguage.ToString()}: {LanguageId}");
        }

        public static CustomLanguage GetCustomLanguageById(int id) => AllLanguages.Where(l => l.LanguageId == id).FirstOrDefault();

        public static implicit operator int(CustomLanguage l) => l.LanguageId;
    }
}
