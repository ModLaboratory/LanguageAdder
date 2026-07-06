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

            //CachedTexts.RemoveAll(tmp => !tmp);

            //CachedTexts.Do(t =>
            //{
            //    var str = t.text;
            //    ReplaceCustom(ref str);
            //    t.SetText(str);
            //});
        }

        public KeyboardListener() { }
        public KeyboardListener(IntPtr ptr) : base(ptr) { }
    }
}