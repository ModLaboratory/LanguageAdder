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

        private void TryLoadCustomLanguages()
        {
            var attempts = 0;
            var hasException = false;

            do
            {
                hasException = false;

                try
                {
                    LanguageManager.LoadCustomLanguages();
                }
                catch
                {
                    hasException = true;
                    attempts++;

                    Main.Logger.LogWarning($"Caught error thrown by {nameof(LanguageManager)}::{nameof(LanguageManager.LoadCustomLanguages)}(), which might be caused by some collection being iterated while attempting to modify the collection. Making another attempt... (Attempt {attempts + 1})");
                }
            }
            while (hasException && attempts < 3);

            if (hasException)
                Main.Logger.LogError("Failed to load custom languages after 3 attempts!");
        }

        public KeyboardListener() { }
        public KeyboardListener(IntPtr ptr) : base(ptr) { }
    }
}