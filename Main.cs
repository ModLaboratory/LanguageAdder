using BepInEx;
using BepInEx.Logging;
using BepInEx.Unity.IL2CPP;
using HarmonyLib;
using System;
using System.IO;
using static LanguageAdder.Data;

namespace LanguageAdder
{
    [BepInPlugin(PluginInfo.PLUGIN_GUID, PluginInfo.PLUGIN_NAME, PluginInfo.PLUGIN_VERSION)]
    public class Main : BasePlugin
    {
        internal static new ManualLogSource Log { get; private set; }
        public static Harmony Harmony { get; private set; }


        public override void Load()
        {
            Log = base.Log;
            Harmony = new(PluginInfo.PLUGIN_GUID);
            // Plugin startup logic
            Log.LogInfo($"Plugin {PluginInfo.PLUGIN_GUID} is loaded!");
            AddComponent<KeyboardListener>();
            Harmony.PatchAll();
        }

        internal static void CheckCreateFiles(ref bool error)
        {
            try
            {
                if (!Directory.Exists(DataFolderPath)) Directory.CreateDirectory(DataFolderPath);
            }
            catch (Exception e)
            {
                Log.LogError($"Error creating data folder: {DataFolderPath}\r\n{e}");
                error = true;
                return;
            }

            try
            {
                GenerateCurrentLanguageExampleFile();
            }
            catch (Exception e)
            {
                Log.LogError($"Error creating example file: {ExampleLangFilePath}\r\n{e}");
                error = true;
                return;
            }

            try
            {
                if (!File.Exists(RegisterLangFilePath)) using (File.Create(RegisterLangFilePath)) { }
            }
            catch (Exception e)
            {
                Log.LogError($"Error creating register file: {RegisterLangFilePath}\r\n{e}");
                error = true;
                return;
            }
        }
    }
}
