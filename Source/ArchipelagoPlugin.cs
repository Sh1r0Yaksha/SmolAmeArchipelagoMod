using BepInEx;
using BepInEx.Logging;
using HarmonyLib;
using SmolAmeArchipelagoMod.Source;
using UnityEngine;

namespace SmolAmeArchipelago
{
    [BepInPlugin(GUID, NAME, VERSION)]
    [BepInDependency("com.Shir0Yaksha.SmolAmePauseMenuCursor")]
    internal class ArchipelagoPlugin : BaseUnityPlugin
    {
        public const string GUID = "com.Shir0Yaksha.SmolAmeArchipelagoMod";
        public const string NAME = "Smol Ame Archipelago Plugin";
        public const string VERSION = "0.1.0";

        internal static ManualLogSource Log;
        private Harmony Harmony;

        private void Awake()
        {
            Log = base.Logger;
            Log.LogInfo($"{NAME} v{VERSION} loaded!");

            GameObject ArchipelagoClient = new GameObject("ArchipelagoClient");
            ArchipelagoClient.AddComponent<ArchipelagoClientScript>();

            Harmony = new Harmony(GUID);
            Harmony.PatchAll();
        }

        private void OnDestroy()
        {
            Harmony?.UnpatchSelf();
        }
    }
}
