global using Object = UnityEngine.Object;
using BepInEx;
using BepInEx.Logging;
using BepInEx.Unity.IL2CPP;
using HarmonyLib;
using System;
using System.IO;
using static LanguageAdder.LanguageManager;

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

            Logger.LogInfo($"{nameof(LanguageAdder)} (v{PluginInfo.PLUGIN_VERSION}) loaded successfully!");
        }
    }
}
