using AmongUs.Data;
using Cpp2IL.Core.Extensions;
using HarmonyLib;
using Il2CppInterop.Runtime.InteropTypes.Arrays;
using System;
using System.Linq;
using UnityEngine;
using UnityEngine.Events;

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
            __instance.selectedLangText.text = CustomLanguage.GetCustomLanguageById(Data.CurrentCustomLanguageId).LanguageName;
            Main.Logger.LogInfo("Set text success");
        }
        #endregion

        #region PATCH LANGUAGE MENU
        [HarmonyPatch(typeof(LanguageSetter), nameof(LanguageSetter.Start))]
        [HarmonyPostfix]
        static void SetLangaugeMenuPatch(LanguageSetter __instance)
        {
            Main.Logger.LogInfo($"===== {nameof(LanguageSetter)}.{nameof(LanguageSetter.Start)}() =====");

            var vanillaLanguageButtons = __instance.AllButtons.ToList().Clone();

            CustomLanguage.AllLanguages.ForEach(l =>
            {
                var button = CreateLanguageButton(__instance, l.LanguageName, TranslationController.Instance.Languages[l.BaseLanguage]);
                if (Data.CurrentCustomLanguageId == l.LanguageId)
                {
                    UnselectAllButtons(__instance);
                    l.LanguageButton.Button.SelectButton(true);
                    l.LanguageButton.Title.color = Color.green;
                }
            });

            vanillaLanguageButtons.ToList().ForEach(b => b.Button.OnClick.AddListener(new Action(() =>
            {
                Data.IsUsingCustomLanguage = false;
                Main.Logger.LogInfo("Changed vanilla language to " + b.Title.text);

                UnselectAllButtons(__instance);
                b.Button.SelectButton(true);
                b.Title.color = Color.green;

                __instance.Close(); // MANUALLY CLOSE TO FIX THE PATCH SelectedLangTextFix BECAUSE Close() IS CALLED IN SetLanguage(LanguageButton)
                TranslationController.Instance.SetLanguage(b.Language.languageID);
            })));

            __instance.ButtonParent.SetBoundsMax(__instance.AllButtons.Length * __instance.ButtonHeight - 2f * __instance.ButtonStart - 0.1f, 0f);

            if (!Data.IsUsingCustomLanguage) return;

            __instance.SetLanguage(CustomLanguage.GetCustomLanguageById(Data.CurrentCustomLanguageId).LanguageButton);
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
                    setterMenu.parentLangButton.text = CustomLanguage.GetCustomLanguageById(Data.CurrentCustomLanguageId).LanguageName;
                else
                    setterMenu.parentLangButton.text = TranslationController.Instance.Languages[TranslationController.Instance.currentLanguage.languageID].Name;
            }
        }
        #endregion

        private static LanguageButton CreateLanguageButton(LanguageSetter __instance, string langName, TranslatedImageSet baseLang)
        {
            var lastButtonTransform = __instance.AllButtons.LastOrDefault().transform;
            if (!lastButtonTransform && __instance.AllButtons != null && __instance.AllButtons.Count < 2) return null;
            var buttonPosPrefab = lastButtonTransform.transform.localPosition;

            var langButton = Object.Instantiate(__instance.ButtonPrefab, lastButtonTransform.parent);

            langButton.Title.text = langName;
            langButton.Language = baseLang;

            var customLanguage = CustomLanguage.AllLanguages.Where(l => l.LanguageName == langButton.Title.text && l.BaseLanguage == langButton.Language.languageID).FirstOrDefault();

            customLanguage.LanguageButton = langButton;

            langButton.Button.OnClick = new();
            langButton.Button.OnClick.AddListener(new Action(() =>
            {
                Data.SetCustomLanguage(customLanguage);

                UnselectAllButtons(__instance);
                langButton.Button.SelectButton(true);
                langButton.Title.color = Color.green;

                __instance.Close(); // ALSO MANUALLY CLOSE TO FIX THE PATCH SelectedLangTextFix
            }));

            var vector = new Vector3(0, __instance.ButtonStart - __instance.AllButtons.Count * __instance.ButtonHeight, -0.5f);

            langButton.transform.localPosition = vector;
            langButton.gameObject.SetActive(true);

            __instance.AllButtons = new(__instance.AllButtons.AddItem(langButton).ToArray());

            return langButton;
        }

        static void UnselectAllButtons(LanguageSetter __instance)
        {
            __instance.AllButtons.ToArray().Do(b =>
            {
                b.Button.SelectButton(false);
                b.Title.color = Color.white;
            });
        }

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
            bool error = false;
            Main.CheckCreateFiles(ref error);
            if (!error) Data.LoadCustomLanguages();
        }
        #endregion

        #region TRANSLATION CONTROLLER PATCH
        [HarmonyPatch(typeof(TranslationController), nameof(TranslationController.GetString), new[] { typeof(string), typeof(string), typeof(Il2CppReferenceArray<Il2CppSystem.Object>) })]
        [HarmonyPrefix]
        public static bool GetStringPatch(TranslationController __instance, string id, string defaultStr, Il2CppReferenceArray<Il2CppSystem.Object> parts, ref string __result)
        {
            if (!Data.IsUsingCustomLanguage) return true;

            var value = Data.Root[id]?.ToString() ?? "";

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
    }
}
