#pragma warning disable CS0618
using HarmonyLib;
using Il2CppInterop.Runtime.InteropTypes.Arrays;
using Il2CppSystem.Linq;
using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using TMPro;
using UnityEngine;

namespace LanguageAdder
{
    [HarmonyPatch]
    internal static class Patch
    {
        internal static IEnumerable<LanguageButton> VanillaLanguageButtons { get; set; } 

        #region PATCH THE BUTTON THAT LEADS TO LANGUAGE MENU
        [HarmonyPatch(typeof(SettingsLanguageMenu), nameof(SettingsLanguageMenu.Awake))]
        [HarmonyPostfix]
        static void SetLanguageButtonPatch(SettingsLanguageMenu __instance)
        {
            Main.Logger.LogInfo($"===== {nameof(SettingsLanguageMenu)}.{nameof(SettingsLanguageMenu.Awake)}() =====");

            if (!LanguageManager.IsUsingCustomLanguage) return;

            if (!__instance.selectedLangText)
            {
                Main.Logger.LogWarning($"{nameof(SettingsLanguageMenu)}::{nameof(SettingsLanguageMenu.selectedLangText)} is null");
                return;
            }

            __instance.selectedLangText.text = LanguageManager.CurrentCustomLanguage.LanguageName;

            Main.Logger.LogInfo($"Set the text of {nameof(SettingsLanguageMenu.selectedLangText)} successfully");
        }
        #endregion

        #region LANGUAGE MENU PATCH
        [HarmonyPatch(typeof(LanguageSetter), nameof(LanguageSetter.Start))]
        [HarmonyPostfix]
        static void SetLangaugeMenuPatch(LanguageSetter __instance)
        {
            Main.Logger.LogInfo($"===== {nameof(LanguageSetter)}::{nameof(LanguageSetter.Start)}() =====");

            VanillaLanguageButtons = __instance.AllButtons.Where(button => TranslationController.Instance.Languages.ContainsValue(button.Language));

            // Highlight custom language button if selected
            CustomLanguage.AllLanguages.ForEach(language =>
            {
                var button = CreateLanguageButton(__instance, language.LanguageName, TranslationController.Instance.Languages[language.BaseLanguage]);

                if (LanguageManager.CurrentCustomLanguage == language)
                {
                    VanillaLanguageButtons.Do(vanillaButton => vanillaButton.Title.color = Color.white);
                    button.Title.color = Color.green;
                }
            });

            // Add custom logic to vanilla language behaviors
            VanillaLanguageButtons.Do(button => button.Button.OnClick.AddListener(new Action(() =>
            {
                LanguageManager.IsUsingCustomLanguage = false;
                Main.Logger.LogInfo($"Changing vanilla language to {button.Title.text}...");

                __instance.SetLanguage(button); // If we not do so, selecting vanilla language after selecting custom language wont work and you have to click vanilla language button twice to let the game apply language change
                __instance.Close(); // MANUALLY CLOSE TO FIX THE PATCH SelectedLangTextFix BECAUSE Close() IS CALLED IN SetLanguage(LanguageButton)

                LanguageManager.RecordLastCustomLanguage(null); // Clear last custom language record
            })));

            // Set the scrolling range of the scroller
            __instance.ButtonParent.SetBoundsMax(__instance.AllButtons.Length * __instance.ButtonHeight - 2f * __instance.ButtonStart - 0.1f, 0f);
        }

        [HarmonyPatch(typeof(LanguageSetter), nameof(LanguageSetter.SetLanguage))]
        [HarmonyPrefix]
        static bool SelectedLangTextFix() => !LanguageManager.IsUsingCustomLanguage;

        [HarmonyPatch(typeof(MainMenuManager), nameof(MainMenuManager.LateUpdate))]
        [HarmonyPostfix]
        static void EnterMenuButtonTextPatch()
        {
            var setterMenu = Object.FindObjectOfType<LanguageSetter>(true);

            if (setterMenu && setterMenu.parentLangButton)
            {
                if (LanguageManager.IsUsingCustomLanguage)
                    setterMenu.parentLangButton.text = LanguageManager.CurrentCustomLanguage.LanguageName;
                else
                    setterMenu.parentLangButton.text = TranslationController.Instance.Languages[TranslationController.Instance.currentLanguage.languageID].Name;
            }
        }
        #endregion

        #region LANGUAGE BUTTON CREATION
        private static LanguageButton CreateLanguageButton(LanguageSetter __instance, string languageName, TranslatedImageSet baseLanguage)
        {
            var lastButtonTransform = __instance.AllButtons.LastOrDefault().transform;

            if (!lastButtonTransform && __instance.AllButtons != null && __instance.AllButtons.Count < 2) // Why did i write these conditions?
                return null; // This if() check seems unnecessary according to my tests, but keeping it for safety is not a bad idea

            var languageButton = Object.Instantiate(__instance.ButtonPrefab, lastButtonTransform.parent);

            languageButton.Title.text = languageName;
            languageButton.Language = baseLanguage;

            var customLanguage = CustomLanguage.AllLanguages.Where(l => l.LanguageName == languageName).FirstOrDefault();

            customLanguage.LanguageButton = languageButton;

            languageButton.Button.OnClick = new();
            languageButton.Button.OnClick.AddListener(new Action(() =>
            {
                LanguageManager.SetCustomLanguage(customLanguage);

                __instance.Close(); // ALSO MANUALLY CLOSE TO FIX THE PATCH SelectedLangTextFix
            }));

            var vector = new Vector3(0, __instance.ButtonStart - __instance.AllButtons.Count * __instance.ButtonHeight, -0.5f);

            languageButton.transform.localPosition = vector;
            languageButton.gameObject.SetActive(true);

            __instance.AllButtons = new(__instance.AllButtons.AddItem(languageButton).ToArray()); // Add new button to vanilla button list

            return languageButton;
        }
        #endregion

        #region MOD STAMP
        [HarmonyPatch(typeof(ModManager), nameof(ModManager.LateUpdate))]
        [HarmonyPostfix]
        static void ShowModStampPatch(ModManager __instance) => __instance.ShowModStamp();
        #endregion

        #region INITIALIZE
        [HarmonyPatch(typeof(TranslationController), nameof(TranslationController.Initialize))]
        [HarmonyPostfix]
        static void InitCustomLanguage(TranslationController __instance) => LanguageManager.LoadCustomLanguages();
        #endregion

        #region TRANSLATION CONTROLLER PATCH
        [HarmonyPatch(typeof(TranslationController), nameof(TranslationController.GetString), new[] { typeof(string), typeof(string), typeof(Il2CppReferenceArray<Il2CppSystem.Object>) })]
        [HarmonyPrefix]
        public static bool GetStringPatch(TranslationController __instance, string id, string defaultStr, Il2CppReferenceArray<Il2CppSystem.Object> parts, ref string __result)
        {
            if (!LanguageManager.IsUsingCustomLanguage) return true;

            var value = LanguageManager.LanguageRoot[id]?.ToString() ?? "";

            if (parts.Any())
                __result = Il2CppSystem.String.Format(value, parts);
            else
                __result = value;

            if (__result.IsNullOrWhiteSpace())
                __result = Il2CppSystem.String.Format(defaultStr, parts);
            if (__result.IsNullOrWhiteSpace())
                __result = __instance.fallbackLanguage.GetString(id, parts);
            if (__result.IsNullOrWhiteSpace())
                __result = "STRMISS";

            return false;
        }
        #endregion

        #region HARD-CODED TEXT REPLACEMENT
        [HarmonyPatch(typeof(TMP_Text), nameof(TMP_Text.text), MethodType.Setter)]
        [HarmonyPrefix]
        static void HardcodedTextPatch([HarmonyArgument(0)] ref string value)
        {
            if (LanguageManager.IsUsingCustomLanguage)
                ReplaceCustom(ref value);
        }

        public static bool ReplaceCustom(ref string origin)
        {
            {
                if (LanguageManager.NonRegexReplacementConfigs.TryGetValue(origin, out var value))
                {
                    origin = value;
                    return true;
                }
            }

            foreach (var (regex, value) in LanguageManager.RegexReplacementConfigs)
            {
                if (regex.IsMatch(origin))
                {
                    try
                    {
                        origin = regex.Replace(origin, value);
                    }
                    catch
                    {
                        return false;
                    }

                    return true;
                }
            }

            return false;
        }
        #endregion
    }
}
#pragma warning restore CS0618
