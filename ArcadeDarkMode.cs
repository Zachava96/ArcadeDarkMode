using System;
using BepInEx;
using BepInEx.Logging;
using BepInEx.Configuration;
using HarmonyLib;
using Arcade.UI;
using Arcade.UI.Other;

namespace ArcadeDarkMode
{
    [BepInPlugin(PLUGIN_GUID, PLUGIN_NAME, PLUGIN_VERSION)]
    [BepInProcess("UNBEATABLE [DEMO].exe")]
    public class ArcadeDarkMode : BaseUnityPlugin
    {
        public const string PLUGIN_GUID = "net.zachava.arcadedarkmode";
        public const string PLUGIN_NAME = "Arcade Dark Mode";
        public const string PLUGIN_VERSION = "1.0.0";
        internal static new ManualLogSource Logger;
        public static ConfigEntry<bool> DarkModeEnabled;
        private static readonly Harmony Harmony = new Harmony(PLUGIN_GUID);

        private void Awake()
        {
            Logger = base.Logger;
            DarkModeEnabled = Config.Bind(
                "General",
                "DarkModeEnabled",
                true,
                "If Arcade Dark Mode should be enabled."
            );
            Logger.LogInfo($"Plugin {PLUGIN_GUID} is loaded!");

            Type[] classesToPatch = { typeof(KonamiCodePatches) };
            foreach (var toPatch in classesToPatch)
            {
                try
                {
                    Logger.LogDebug($"Patching {toPatch.Name}");
                    Harmony.CreateAndPatchAll(toPatch, PLUGIN_GUID);
                }
                catch (Exception e)
                {
                    Logger.LogError($"Failed to patch {toPatch.Name}: {e}");
                }

            }
        }
    }

   
    static class KonamiCodePatches
    {
        [HarmonyPatch(typeof(KonamiCode))]
        [HarmonyPatch("Awake")]
        static void Postfix(KonamiCode __instance)
        {
            //ArcadeDarkMode.Logger.LogInfo("KonamiCode.Awake postfix patch entered.");
            if (ArcadeDarkMode.DarkModeEnabled.Value)
            {
                var onCodeEntered = AccessTools.Field(__instance.GetType(), "OnCodeEntered")?.GetValue(__instance);
                if (onCodeEntered is UnityEngine.Events.UnityEvent unityEvent)
                {
                    unityEvent.Invoke();
                }

                var resetMethod = AccessTools.Method(__instance.GetType(), "ResetSequence");
                resetMethod?.Invoke(__instance, null);
            }
        }

        [HarmonyPatch(typeof(KonamiCode))]
        [HarmonyPatch("ProcessInput")]
        static bool Prefix(KonamiCode __instance)
        {
            //ArcadeDarkMode.Logger.LogInfo("KonamiCode.ProcessInput prefix patch entered.");
            // Access private fields using reflection
            var indexField = AccessTools.Field(__instance.GetType(), "_index");
            var sequenceField = AccessTools.Field(__instance.GetType(), "_sequence");
            var timerField = AccessTools.Field(__instance.GetType(), "_timer");
            var onCodeEnteredField = AccessTools.Field(__instance.GetType(), "OnCodeEntered");
            var resetSequenceMethod = AccessTools.Method(__instance.GetType(), "ResetSequence");

            int index = (int)indexField.GetValue(__instance);
            var sequence = sequenceField.GetValue(__instance) as System.Collections.IList;

            index++;
            indexField.SetValue(__instance, index);

            if (sequence != null && index == sequence.Count)
            {
                if (UIColorPaletteUpdater.SelectedPalette == 0)
                {
                    ArcadeDarkMode.DarkModeEnabled.Value = true;
                    UIColorPaletteUpdater.SelectedPalette = 1;
                }
                else
                {
                    ArcadeDarkMode.DarkModeEnabled.Value = false;
                    UIColorPaletteUpdater.SelectedPalette = 0;
                }
                resetSequenceMethod.Invoke(__instance, null);
                return false; // Skip original
            }

            timerField.SetValue(__instance, 2f);

            return false; // Skip original
        }
    }
}
