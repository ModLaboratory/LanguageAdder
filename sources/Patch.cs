using Cpp2IL.Core.Extensions;
using HarmonyLib;
using Il2CppInterop.Runtime.InteropTypes.Arrays;
using Il2CppSystem;
using System;
using System.IO;
using System.Linq;
using TMPro;
using UnityEngine;
using UnityEngine.Events;
using Object = UnityEngine.Object;

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
            Main.Logger.LogInfo("Awake entered");
            if (Data.CurrentCustomLanguageId == int.MinValue) return;
            if (!__instance.selectedLangText)
            {
                Main.Logger.LogWarning("selectedLangText is null");
                return;
            }
            __instance.selectedLangText.text = CustomLanguage.GetCustomLanguageById(Data.CurrentCustomLanguageId).LanguageName;
            Main.Logger.LogInfo("Set selectLangText success");
        }
        #endregion

        #region PATCH LANGUAGE MENU
        [HarmonyPatch(typeof(LanguageSetter), nameof(LanguageSetter.Start))]
        [HarmonyPostfix]
        static void SetLangaugeMenuPatch(LanguageSetter __instance)
        {
            var vanillaLanguageButtons = __instance.AllButtons.ToList().Clone();

            CustomLanguage.AllLanguages.ForEach(l => CreateLanguageButton(__instance, l.LanguageName, ToTranslationImageSet(l.BaseLanguage)));
            vanillaLanguageButtons.ToList().ForEach(b => b.Button.OnClick.AddListener((UnityAction)(() =>
            {
                Data.CurrentCustomLanguageId = int.MinValue; // set lang id to min value when vanilla langs are set
                Main.Logger.LogInfo("Changed vanilla language to " + b.Title.text);
            })));

            __instance.ButtonParent.SetBoundsMax(__instance.AllButtons.Length * __instance.ButtonHeight - 2f * __instance.ButtonStart - 0.1f, 0f);

            if (Data.CurrentCustomLanguageId == int.MinValue) return;
            __instance.SetLanguage(CustomLanguage.GetCustomLanguageById(Data.CurrentCustomLanguageId).LanguageButton);
        }

        [HarmonyPatch(typeof(MainMenuManager), nameof(MainMenuManager.LateUpdate))]
        [HarmonyPostfix]
        static void EnterMenuButtonTextPatch()
        {
            SettingsLanguageMenu button;
            if (button = DestroyableSingleton<SettingsLanguageMenu>.Instance)
            {
                if (Data.CurrentCustomLanguageId == int.MinValue || !button.selectedLangText) return;
                button.selectedLangText.text = CustomLanguage.GetCustomLanguageById(Data.CurrentCustomLanguageId).LanguageName;
            }
        }
        #endregion

        #region CONVERSIONS
        internal static TranslatedImageSet ToTranslationImageSet(SupportedLangs lang) => TranslationController.Instance.Languages[lang];
        internal static SupportedLangs? ToSupportedLangs(TranslatedImageSet lang)
        {
            foreach (var pair in TranslationController.Instance.Languages)
                if (pair.Value == lang)
                    return pair.Key;
            return null;
        }
        #endregion

        static void CreateLanguageButton(LanguageSetter __instance, string langName, TranslatedImageSet baseLang)
        {
            var lastButtonTransform = __instance.AllButtons.LastOrDefault().transform;
            if (!lastButtonTransform && __instance.AllButtons != null && __instance.AllButtons.Count < 2) return;
            var buttonPosPrefab = lastButtonTransform.transform.localPosition;

            var lang = Object.Instantiate(__instance.ButtonPrefab, lastButtonTransform.parent);

            lang.Title.text = langName;
            lang.Language = baseLang;

            var customLanguage = CustomLanguage.AllLanguages.Where(l => l.LanguageName == lang.Title.text && l.BaseLanguage == lang.Language.ToSupportedLangs()).FirstOrDefault();
            
            customLanguage.LanguageButton = lang;

            lang.Button.OnClick = new();
            lang.Button.OnClick.AddListener((UnityAction)(() => Data.SetCustomLanguage(customLanguage)));

            var vector = new Vector3(0, __instance.ButtonStart - __instance.AllButtons.Count * __instance.ButtonHeight, -0.5f);
            
            lang.transform.localPosition = vector;
            lang.gameObject.SetActive(true);

            __instance.AllButtons = new(__instance.AllButtons.AddItem(lang).ToArray());
        }

        #region MOD STAMP
        [HarmonyPatch(typeof(ModManager),nameof(ModManager.LateUpdate))]
        [HarmonyPostfix]
        static void ShowModStampPatch(ModManager __instance) => __instance.ShowModStamp();
        #endregion

        #region INITIALIZE
        [HarmonyPatch(typeof(TranslationController),nameof(TranslationController.Initialize))]
        [HarmonyPostfix]
        static void InitCustomLanguage(TranslationController __instance)
        {
            bool error = false;
            Main.CheckCreateFiles(ref error);
            if (!error) Data.LoadCustomLanguages();
        }
        #endregion

        [HarmonyPatch(typeof(TranslationController), nameof(TranslationController.GetString), new[] { typeof(string), typeof(string), typeof(Il2CppReferenceArray<Il2CppSystem.Object>) })]
        [HarmonyPrefix]
        public static bool GetStringPatch(string id, string defaultStr, Il2CppReferenceArray<Il2CppSystem.Object> parts, ref string __result)
        {
            if (Data.CurrentCustomLanguageId == int.MinValue) return true;

            __result = Il2CppSystem.String.Format(Data.Root[id]?.ToString() ?? "EMPTY", parts);

            if (__result.IsNullOrWhiteSpace())
                __result = Il2CppSystem.String.Format(defaultStr, parts);
            if (__result.IsNullOrWhiteSpace())
                __result = "STRMISS";

            return false;
        }

        [HarmonyPatch(typeof(TranslationController), nameof(TranslationController.SetLanguage))]
        [HarmonyPostfix]
        public static void SetLanguagePatch(SupportedLangs language)
        {
            Main.Logger.LogInfo("Set vanilla language to: " + language);
        }
    }
}
