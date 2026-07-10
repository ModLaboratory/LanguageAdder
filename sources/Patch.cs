using AmongUs.Data;
using Cpp2IL.Core.Extensions;
using HarmonyLib;
using Il2CppInterop.Runtime.InteropTypes.Arrays;
using Il2CppSystem.Linq;
using Newtonsoft.Json.Linq;
using System;
using System.Linq;
using System.Text.RegularExpressions;
using TMPro;
using UnityEngine;
using UnityEngine.EventSystems;

namespace LanguageAdder
{
    [HarmonyPatch]
    internal static class Patch
    {
        #region PATCH THE BUTTON THAT LEADS TO LANGUAGE MENU
        [HarmonyPatch(typeof(SettingsLanguageMenu), nameof(SettingsLanguageMenu.Awake))]
        [HarmonyPostfix]
        static void SetLanguageButtonPatch(SettingsLanguageMenu __instance)
        {
            Main.Logger.LogInfo($"===== {nameof(SettingsLanguageMenu)}.{nameof(SettingsLanguageMenu.Awake)}() =====");

            if (!Data.IsUsingCustomLanguage) return;

            if (!__instance.selectedLangText)
            {
                Main.Logger.LogWarning("selected language text is null");
                return;
            }

            __instance.selectedLangText.text = Data.CurrentCustomLanguage.LanguageName;

            Main.Logger.LogInfo($"Set the text of {nameof(SettingsLanguageMenu.selectedLangText)} success");
        }
        #endregion

        #region LANGUAGE MENU PATCH
        [HarmonyPatch(typeof(LanguageSetter), nameof(LanguageSetter.Start))]
        [HarmonyPostfix]
        static void SetLangaugeMenuPatch(LanguageSetter __instance)
        {
            Main.Logger.LogInfo($"===== {nameof(LanguageSetter)}::{nameof(LanguageSetter.Start)}() =====");

            var vanillaLanguageButtons = __instance.AllButtons.Where(button => TranslationController.Instance.Languages.ContainsValue(button.Language));

            // Highlight custom language button if selected
            CustomLanguage.AllLanguages.ForEach(language =>
            {
                var button = CreateLanguageButton(__instance, language.LanguageName, TranslationController.Instance.Languages[language.BaseLanguage]);

                if (Data.CurrentCustomLanguage == language)
                {
                    vanillaLanguageButtons.Do(vanillaButton => vanillaButton.Title.color = Color.white);
                    button.Title.color = Color.green;
                }
            });

            // Add custom logic to vanilla language behaviors
            vanillaLanguageButtons.ToList().ForEach(button => button.Button.OnClick.AddListener(new Action(() =>
            {
                Data.IsUsingCustomLanguage = false;
                Main.Logger.LogInfo("Changed vanilla language to " + button.Title.text);

                __instance.SetLanguage(button); // If we not do so, selecting vanilla language after selecting custom language wont work and you have to click vanilla language button twice to let the game apply language change
                __instance.Close(); // MANUALLY CLOSE TO FIX THE PATCH SelectedLangTextFix BECAUSE Close() IS CALLED IN SetLanguage(LanguageButton)
                
                Data.RecordLastCustomLanguage(null); // Clear last custom language record
            })));

            // Set the scrolling range of the scroller
            __instance.ButtonParent.SetBoundsMax(__instance.AllButtons.Length * __instance.ButtonHeight - 2f * __instance.ButtonStart - 0.1f, 0f);
        }

        [HarmonyPatch(typeof(LanguageSetter), nameof(LanguageSetter.SetLanguage))]
        [HarmonyPrefix]
        static bool SelectedLangTextFix() => !Data.IsUsingCustomLanguage;

        [HarmonyPatch(typeof(MainMenuManager), nameof(MainMenuManager.LateUpdate))]
        [HarmonyPostfix]
        static void EnterMenuButtonTextPatch()
        {
            var setterMenu = Object.FindObjectOfType<LanguageSetter>(true);

            if (setterMenu && setterMenu.parentLangButton)
            {
                if (Data.IsUsingCustomLanguage)
                    setterMenu.parentLangButton.text = Data.CurrentCustomLanguage.LanguageName;
                else
                    setterMenu.parentLangButton.text = TranslationController.Instance.Languages[TranslationController.Instance.currentLanguage.languageID].Name;
            }
        }
        #endregion

        #region LANGUAGE BUTTON CREATION
        private static LanguageButton CreateLanguageButton(LanguageSetter __instance, string langName, TranslatedImageSet baseLang)
        {
            var lastButtonTransform = __instance.AllButtons.LastOrDefault().transform;
            if (!lastButtonTransform && __instance.AllButtons != null && __instance.AllButtons.Count < 2) return null;
            var buttonPosPrefab = lastButtonTransform.transform.localPosition;

            var langButton = Object.Instantiate(__instance.ButtonPrefab, lastButtonTransform.parent);

            langButton.Title.text = langName;
            langButton.Language = baseLang;

            var customLanguage = CustomLanguage.AllLanguages.Where(l => l.LanguageName == langName).FirstOrDefault();
            
            customLanguage.LanguageButton = langButton;
            
            langButton.Button.OnClick = new();
            langButton.Button.OnClick.AddListener(new Action(() =>
            {
                Data.SetCustomLanguage(customLanguage);

                __instance.Close(); // ALSO MANUALLY CLOSE TO FIX THE PATCH SelectedLangTextFix
            }));
            
            var vector = new Vector3(0, __instance.ButtonStart - __instance.AllButtons.Count * __instance.ButtonHeight, -0.5f);
            
            langButton.transform.localPosition = vector;
            langButton.gameObject.SetActive(true);
            
            __instance.AllButtons = new(__instance.AllButtons.AddItem(langButton).ToArray()); // Add new button to vanilla button list

            return langButton;
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
        static void InitCustomLanguage(TranslationController __instance)
        {
            var hasError = false;
            Main.CheckCreateFiles(ref hasError);

            if (!hasError) 
                Data.LoadCustomLanguages();
        }
        #endregion

        #region TRANSLATION CONTROLLER PATCH
        [HarmonyPatch(typeof(TranslationController), nameof(TranslationController.GetString), new[] { typeof(string), typeof(string), typeof(Il2CppReferenceArray<Il2CppSystem.Object>) })]
        [HarmonyPrefix]
        public static bool GetStringPatch(TranslationController __instance, string id, string defaultStr, Il2CppReferenceArray<Il2CppSystem.Object> parts, ref string __result)
        {
            if (!Data.IsUsingCustomLanguage) return true;

            var value = Data.LanguageRoot[id]?.ToString() ?? "";

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
            if (Data.IsUsingCustomLanguage)
                ReplaceCustom(ref value);
        }

        public static bool ReplaceCustom(ref string origin)
        {
            {
                if (Data.NonRegexReplacementConfig.TryGetValue(origin, out var value))
                {
                    origin = value;
                    return true;
                }
            }

            foreach (var (regex, value) in Data.RegexReplacementConfig)
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
