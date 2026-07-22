using System;
using UnityEngine;

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
            if (Input.GetKeyDown(KeyCode.F1)) LanguageManager.GenerateCurrentLanguageExampleFile();
            if (Input.GetKeyDown(KeyCode.F2)) LanguageManager.LoadCustomLanguages();
        }

        public KeyboardListener() { }
        public KeyboardListener(IntPtr ptr) : base(ptr) { }
    }
}