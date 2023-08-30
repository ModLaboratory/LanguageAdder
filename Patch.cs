using Cpp2IL.Core.Extensions;
using HarmonyLib;
using Il2CppInterop.Runtime.InteropTypes.Arrays;
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
        [HarmonyPatch(typeof(TranslationController), nameof(TranslationController.GetString), new Type[] { typeof(StringNames), typeof(Il2CppReferenceArray<Il2CppSystem.Object>) })]
        [HarmonyPostfix]
        static void GetStringPatch(TranslationController __instance, ref string __result, [HarmonyArgument(0)] StringNames id, [HarmonyArgument(1)] Il2CppReferenceArray<Il2CppSystem.Object> parts)
        {
            if (Data.CurrentCustomLanguageId == int.MinValue) return;

            using StreamReader reader = File.OpenText(CustomLanguage.GetCustomLanguageById(Data.CurrentCustomLanguageId).FilePath);
            string line;
            
            while ((line = reader.ReadLine()) != null)
            {
                if (line.StartsWith('#')) continue;
                string[] translationKeyValue = line.Split('\t');

                if (translationKeyValue[0] == id.ToString())
                {
                    if (translationKeyValue.Length == 2)
                    {
                        var replaced = translationKeyValue[1].Replace("\\r", "").Replace("\\n", "\n");
                        if (parts == null)
                        {
                            __result = replaced;
                            break;
                        }
                        else
                        {
                            __result = Il2CppSystem.String.Format(replaced, parts);
                            break;
                        }
                    }
                    break;
                }
            }
        }

        [HarmonyPatch(typeof(SettingsLanguageMenu), nameof(SettingsLanguageMenu.Awake))]
        [HarmonyPostfix]
        static void SetLanguageButtonPatch(SettingsLanguageMenu __instance)
        {
            if (Data.CurrentCustomLanguageId == int.MinValue) return;
            __instance.transform.Find("Text_TMP").GetComponent<TextMeshPro>().text = CustomLanguage.GetCustomLanguageById(Data.CurrentCustomLanguageId).LanguageName;
        }

        [HarmonyPatch(typeof(LanguageSetter), nameof(LanguageSetter.Start))]
        [HarmonyPostfix]
        static void SetLangaugeMenuPatch(LanguageSetter __instance)
        {
            var vanillaLanguageButtons = __instance.AllButtons.ToList().Clone();

            CustomLanguage.CustomLanguages.ForEach(l => CreateLanguageButton(__instance, l.LanguageName, ToTranslationImageSet(l.BaseLanguage)));
            vanillaLanguageButtons.ToList().ForEach(b => b.Button.OnClick.AddListener((UnityAction)(() =>
            {
                Data.CurrentCustomLanguageId = int.MinValue;
                Main.Log.LogInfo("Changed vanilla language to " + b.Title.text);
            })));

            if (Data.CurrentCustomLanguageId == int.MinValue) return;
            __instance.SetLanguage(CustomLanguage.GetCustomLanguageById(Data.CurrentCustomLanguageId).LanguageButton);
        }
        

        internal static TranslatedImageSet ToTranslationImageSet(SupportedLangs lang) => TranslationController.Instance.Languages[lang];
        internal static SupportedLangs? ToSupportedLangs(TranslatedImageSet lang)
        {
            foreach (var pair in TranslationController.Instance.Languages)
                if (pair.Value == lang)
                    return pair.Key;
            return null;
        }

        static void CreateLanguageButton(LanguageSetter __instance, string langName, TranslatedImageSet baseLang)
        {
            var lastButtonTransform = __instance.AllButtons.LastOrDefault().transform;
            if (!lastButtonTransform && __instance.AllButtons != null && __instance.AllButtons.Count < 2) return;
            var buttonPosPrefab = lastButtonTransform.transform.localPosition;

            var lang = Object.Instantiate(__instance.ButtonPrefab, lastButtonTransform.parent);

            lang.Title.text = langName;
            lang.Language = baseLang;

            var customLanguage = CustomLanguage.CustomLanguages.Where(l => l.LanguageName == lang.Title.text && l.BaseLanguage == lang.Language.ToSupportedLangs()).FirstOrDefault();
            
            customLanguage.LanguageButton = lang;

            lang.Button.OnClick = new();
            lang.Button.OnClick.AddListener((UnityAction)(() => Data.SetCustomLanguage(customLanguage)));

            var distance = buttonPosPrefab.y - __instance.AllButtons[__instance.AllButtons.Count - 2].transform.localPosition.y; // -0.5
            var vector = new Vector3(buttonPosPrefab.x, buttonPosPrefab.y + distance, buttonPosPrefab.z);
            
            lang.transform.localPosition = vector;
            lang.gameObject.SetActive(true);

            __instance.AllButtons.AddItem(lang);
        }


        [HarmonyPatch(typeof(ModManager),nameof(ModManager.LateUpdate))]
        [HarmonyPostfix]
        static void ShowModStampPatch(ModManager __instance) => __instance.ShowModStamp();

        [HarmonyPatch(typeof(TranslationController),nameof(TranslationController.Initialize))]
        [HarmonyPostfix]
        static void InitCustomLanguage(TranslationController __instance)
        {
            bool error = false;
            Main.CheckCreateFiles(ref error);
            if (!error) Data.LoadCustomLanguages();
        }

        [HarmonyPatch(typeof(HudManager), nameof(HudManager.Update))]
        [HarmonyPostfix]
        static void ButtonTextUpdate(HudManager __instance)
        {
            Func<StringNames, Il2CppReferenceArray<Il2CppSystem.Object>, string> getString = TranslationController.Instance.GetString;
            __instance.UseButton.OverrideText(getString(StringNames.UseLabel, null));
            __instance.PetButton.OverrideText(getString(StringNames.PetLabel, null));
            __instance.KillButton.OverrideText(getString(StringNames.KillLabel, null));
            __instance.SabotageButton.OverrideText(getString(StringNames.SabotageLabel, null));
            __instance.AdminButton.OverrideText(getString(StringNames.Admin, null));
            __instance.ImpostorVentButton.OverrideText(getString(StringNames.VentLabel, null));
            __instance.ReportButton.OverrideText(getString(StringNames.ReportLabel, null));
        }
        

    }
}
