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
            DontDestroyOnLoad(this);
        }

        public void Update()
        {
            if (!TranslationController.InstanceExists) return;
            if (Input.GetKeyDown(KeyCode.F1)) Data.GenerateCurrentLanguageExampleFile();
            if (Input.GetKeyDown(KeyCode.F2)) Data.LoadCustomLanguages();
        }

        public KeyboardListener() { }
        public KeyboardListener(IntPtr ptr) : base(ptr) { }
    }
}