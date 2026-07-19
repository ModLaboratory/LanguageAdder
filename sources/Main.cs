global using Object = UnityEngine.Object;
using BepInEx;
using BepInEx.Logging;
using BepInEx.Unity.IL2CPP;
using HarmonyLib;
using System;
using System.IO;
using System.Linq;
using static LanguageAdder.Data;

namespace LanguageAdder
{
    [BepInPlugin(PluginInfo.PLUGIN_GUID, PluginInfo.PLUGIN_NAME, PluginInfo.PLUGIN_VERSION)]
    public class Main : BasePlugin
    {
        public static ManualLogSource Logger { get; private set; }
        public static Harmony Harmony { get; private set; }

        public override void Load()
        {
            Logger = Log;
            Harmony = new(PluginInfo.PLUGIN_GUID);

            AddComponent<KeyboardListener>();
            Harmony.PatchAll();

            Logger.LogInfo($"LanguageAdder loaded successfully!");
        }

        internal static void CheckCreateFiles(ref bool hasError)
        {
            try
            {
                if (!Directory.Exists(DataFolderPath)) 
                    Directory.CreateDirectory(DataFolderPath);
            }
            catch (Exception e)
            {
                Logger.LogError($"Error creating data folder: {DataFolderPath}\r\n{e}");
                hasError = true;
                return;
            }

            try
            {
                GenerateCurrentLanguageExampleFile();
            }
            catch (Exception e)
            {
                Logger.LogError($"Error creating example file: {ExampleLangFilePath}\r\n{e}");
                hasError = true;
                return;
            }

            try
            {
                if (!File.Exists(RegisteredLanguageFilePath))
                    File.WriteAllText(RegisteredLanguageFilePath, "");
            }
            catch (Exception e)
            {
                Logger.LogError($"Error creating registry file: {RegisteredLanguageFilePath}\r\n{e}");
                hasError = true;
                return;
            }
        }
    }
}
