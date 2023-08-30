using System;
using UnityEngine;
using Object = UnityEngine.Object;

namespace LanguageAdder
{
    public class KeyboardListener : MonoBehaviour
    {
        public void Start()
        {
            Object.DontDestroyOnLoad(this);
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