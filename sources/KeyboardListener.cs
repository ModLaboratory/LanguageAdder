using HarmonyLib;
using Newtonsoft.Json.Linq;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text.RegularExpressions;
using TMPro;
using UnityEngine;
using UnityEngine.SceneManagement;

namespace LanguageAdder
{
    public class KeyboardListener : MonoBehaviour
    {
        public void Start()
        {
            Object.DontDestroyOnLoad(this);
        }

        public static List<TMP_Text> CachedTexts { get; set; } = new();

        public void Update()
        {
            if (!TranslationController.InstanceExists) return;
            if (Input.GetKeyDown(KeyCode.F1)) Data.GenerateCurrentLanguageExampleFile();
            if (Input.GetKeyDown(KeyCode.F2)) Data.LoadCustomLanguages();

            if (!Data.IsUsingCustomLanguage) return;

            CachedTexts.RemoveAll(tmp => !tmp);

            CachedTexts.Do(t =>
            {
                var str = t.text;
                ReplaceCustom(ref str);
                t.SetText(str);
            });
        }

        public bool ReplaceCustom(ref string origin)
        {
            try
            {
                foreach (var item in Data.ReplacementRoot._values)
                {
                    var obj = item.Cast<JObject>();
                    var key = obj["key"].ToString(); // pattern for regex
                    var value = obj["value"].ToString(); // replacement for regex
                    var regex = false;

                    try
                    {
                        regex = (bool)obj["isRegex"];
                    }
                    catch
                    {
                    }

                    if (regex)
                    {
                        if (Regex.IsMatch(origin, key))
                        {
                            origin = Regex.Replace(origin, key, value);
                            break;
                        }
                    }
                    else
                    {
                        if (key == origin)
                        {
                            origin = value;
                            break;
                        }
                    }
                }

                return false;
            }
            catch
            {
            }

            return true;
        }

        public KeyboardListener() { }
        public KeyboardListener(IntPtr ptr) : base(ptr) { }
    }
}