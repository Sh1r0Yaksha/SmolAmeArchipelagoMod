using BepInEx;
using HarmonyLib;
using Newtonsoft.Json;
using Smol_Ame_Archipelago;
using SmolAmeArchipelagoMod.Source;
using System;
using System.Collections.Generic;
using System.IO;
using System.Net;
using UnityEngine;

namespace SmolAmeArchipelago
{
    [BepInPlugin(GUID, NAME, VERSION)]
    internal class ArchipelagoPlugin : BaseUnityPlugin
    {
        public const string GUID = "com.Shir0Yaksha.SmolAmeArchipelagoMod";
        public const string NAME = "Smol Ame Archipelago Plugin";
        public const string VERSION = "0.3.0";

        internal static RegionSyncController RegionSync;

        private Harmony Harmony;

        private void Awake()
        {
            // Keep this method tiny on purpose. If a dependency DLL is missing, the runtime fails
            // on the first method that *references* it. Doing the real work in AwakeInternal means
            // that failure is caught below and logged, instead of killing Awake before any log line.
            Dbg.Init(Logger);
            Dbg.Info("Plugin", $"Awake() entered - {NAME} v{VERSION} ({GUID})");

            try
            {
                AwakeInternal();
                Dbg.Info("Plugin", "Awake() finished OK - plugin fully initialised");
            }
            catch (Exception e)
            {
                Dbg.Error("Plugin", "Awake() FAILED. Everything after the last 'Step' line above did NOT run.", e);
            }
        }

        private void AwakeInternal()
        {
            // ---------------------------------------------------------------- Step 1
            Dbg.Info("Plugin", "Step 1/6: checking dependency DLLs");
            LogDependency("Newtonsoft.Json", () => typeof(JsonConvert));
            LogDependency("Archipelago.MultiClient.Net", () => typeof(global::Archipelago.MultiClient.Net.ArchipelagoSession));

            // ---------------------------------------------------------------- Step 2
            Dbg.Info("Plugin", "Step 2/6: downloading IDs.json");
            string IDsJSON = GetAreaItemsJSON();
            string preview = IDsJSON.Substring(0, Math.Min(200, IDsJSON.Length)).Replace("\n", " ").Replace("\r", "");
            Dbg.Info("Plugin", $"  downloaded {IDsJSON.Length} chars. Starts with: {preview}");

            // ---------------------------------------------------------------- Step 3
            Dbg.Info("Plugin", "Step 3/6: deserialising IDs.json");
            SmolAmeData.RegionsData = JsonConvert.DeserializeObject<Dictionary<string, RegionData>>(IDsJSON);

            if (SmolAmeData.RegionsData == null)
                throw new InvalidOperationException("IDs.json deserialised to null (is the file valid JSON of the expected shape?)");

            Dbg.Info("Plugin", $"  {SmolAmeData.RegionsData.Count} region(s) in IDs.json");

            // ---------------------------------------------------------------- Step 4
            Dbg.Info("Plugin", "Step 4/6: building lookup tables");

            // Defensive: make sure the outer dictionaries exist whichever way fix #1 was done.
            if (SmolAmeData.AreaItemCollected == null)
            {
                Dbg.Warn("Plugin", "  SmolAmeData.AreaItemCollected was null - creating it");
                SmolAmeData.AreaItemCollected = new Dictionary<string, Dictionary<string, bool>>();
            }
            if (SmolAmeData.AreaChecksCollected == null)
            {
                Dbg.Warn("Plugin", "  SmolAmeData.AreaChecksCollected was null - creating it");
                SmolAmeData.AreaChecksCollected = new Dictionary<string, Dictionary<string, bool>>();
            }
            if (SmolAmeData.LocationIDToCheck == null)
            {
                Dbg.Warn("Plugin", "  SmolAmeData.LocationIDToCheck was null - creating it");
                SmolAmeData.LocationIDToCheck = new Dictionary<int, Tuple<string, string>>();
            }

            int totalItems = 0;
            int totalChecks = 0;

            foreach (var regionData in SmolAmeData.RegionsData)
            {
                string region = regionData.Key;
                RegionData data = regionData.Value;

                if (data == null)
                {
                    Dbg.Warn("Plugin", $"  Region '{region}' has null data in IDs.json - skipped");
                    continue;
                }

                SmolAmeData.AreaItemCollected[region] = new Dictionary<string, bool>();
                SmolAmeData.AreaChecksCollected[region] = new Dictionary<string, bool>();

                foreach (var item in data.Items)
                {
                    SmolAmeData.AreaItemCollected[region][item.Key] = false;
                }
                foreach (var Check in data.Checks)
                {
                    SmolAmeData.AreaChecksCollected[region][Check.Key] = false;

                    if (SmolAmeData.LocationIDToCheck.ContainsKey(Check.Value))
                    {
                        Tuple<string, string> existing = SmolAmeData.LocationIDToCheck[Check.Value];
                        Dbg.Warn("Plugin", $"  Duplicate location ID {Check.Value}: '{existing.Item1} / {existing.Item2}' is overwritten by '{region} / {Check.Key}'");
                    }

                    SmolAmeData.LocationIDToCheck[Check.Value] = new Tuple<string, string>(region, Check.Key);
                }

                totalItems += data.Items.Count;
                totalChecks += data.Checks.Count;

                string note = SmolAmeData.CommonNametoSceneName.ContainsKey(region)
                    ? ""
                    : "   <-- NOT in SmolAmeData.CommonNametoSceneName: no scene will ever map to this region";

                Dbg.Info("Plugin", $"  '{region}': {data.Items.Count} items, {data.Checks.Count} checks{note}");
            }

            // Reverse check: regions the mod knows about but IDs.json doesn't have.
            foreach (string knownRegion in SmolAmeData.CommonNametoSceneName.Keys)
            {
                if (SmolAmeData.RegionsData.ContainsKey(knownRegion))
                    continue;

                if (SmolAmeData.NonLevelRegions.Contains(knownRegion))
                    Dbg.Info("Plugin", $"  Region '{knownRegion}' is not in IDs.json (fine, it is a non-level region)");
                else
                    Dbg.Warn("Plugin", $"  Region '{knownRegion}' is in the mod's scene table but NOT in IDs.json - it will do nothing in-game");
            }

            Dbg.Info("Plugin", $"  totals: {totalItems} items, {totalChecks} checks, {SmolAmeData.LocationIDToCheck.Count} unique location IDs");

            // ---------------------------------------------------------------- Step 5
            Dbg.Info("Plugin", "Step 5/6: creating the ArchipelagoClient GameObject");
            GameObject ArchipelagoClient = new GameObject("ArchipelagoClient");

            RegionSync = ArchipelagoClient.AddComponent<RegionSyncController>();
            Dbg.Info("Plugin", "  RegionSyncController added: " + (RegionSync != null));

            var checker = ArchipelagoClient.AddComponent<CompletionRuleChecker>();
            Dbg.Info("Plugin", "  CompletionRuleChecker added: " + (checker != null));

            var console = ArchipelagoClient.AddComponent<SmolAmeArchipelagoMod.Source.Console>();
            Dbg.Info("Plugin", "  Console (HUD flusher) added: " + (console != null));

            DontDestroyOnLoad(ArchipelagoClient);
            Dbg.Info("Plugin", "  DontDestroyOnLoad applied");

            // ---------------------------------------------------------------- Step 6
            Dbg.Info("Plugin", "Step 6/6: applying Harmony patches");
            Harmony = new Harmony(GUID);
            Harmony.PatchAll();

            int patchedCount = 0;
            foreach (var method in Harmony.GetPatchedMethods())
            {
                patchedCount++;
                Dbg.Info("Plugin", $"  patched: {method.DeclaringType.FullName}.{method.Name}");
            }

            // Expected: HUDScript.Start, PauseScreen.Start, PauseOption.Update
            if (patchedCount != 3)
                Dbg.Warn("Plugin", $"  Expected 3 patched methods (HUDScript.Start, PauseScreen.Start, PauseOption.Update) but found {patchedCount}");
            else
                Dbg.Info("Plugin", "  3/3 expected methods patched");
        }

        private static void LogDependency(string label, Func<Type> getType)
        {
            try
            {
                Type t = getType();
                Dbg.Info("Plugin", $"  {label}: OK v{t.Assembly.GetName().Version} @ {t.Assembly.Location}");
            }
            catch (Exception e)
            {
                Dbg.Error("Plugin", $"  {label}: FAILED to load", e);
            }
        }

        private void OnDestroy()
        {
            Dbg.Warn("Plugin", "OnDestroy() called - unpatching Harmony");
            Harmony?.UnpatchSelf();
        }

        private string GetAreaItemsJSON()
        {
            //string jsonURL = "https://raw.githubusercontent.com/Sh1r0Yaksha/SmolAmeAPWorld/main/src/IDs.json";

            string jsonURL = "https://raw.githubusercontent.com/Sh1r0Yaksha/SmolAmeAPWorld/dev/src/IDs.json";

            Dbg.Info("Plugin", $"  GET {jsonURL}");
            var sw = System.Diagnostics.Stopwatch.StartNew();

            try
            {
                HttpWebRequest request =
                    (HttpWebRequest)WebRequest.Create(jsonURL);

                request.Method = "GET";
                request.Timeout = 10000;
                request.ReadWriteTimeout = 10000;
                request.UserAgent = $"{NAME}/{VERSION}";

                using (HttpWebResponse response =
                       (HttpWebResponse)request.GetResponse())
                using (Stream stream = response.GetResponseStream())
                using (StreamReader reader = new StreamReader(stream))
                {
                    Dbg.Info("Plugin", $"  HTTP {(int)response.StatusCode} {response.StatusDescription} after {sw.ElapsedMilliseconds} ms");
                    string body = reader.ReadToEnd();
                    Dbg.Info("Plugin", $"  body read after {sw.ElapsedMilliseconds} ms total");
                    return body;
                }
            }
            catch (WebException e)
            {
                HttpWebResponse errorResponse = e.Response as HttpWebResponse;
                string http = errorResponse != null ? ((int)errorResponse.StatusCode).ToString() : "no response";

                Dbg.Error("Plugin",
                    $"  Web request FAILED after {sw.ElapsedMilliseconds} ms. WebExceptionStatus={e.Status}, HTTP={http}. " +
                    "(TrustFailure / SecureChannelFailure = Mono TLS problem; NameResolutionFailure = offline; 404 = wrong branch/path)",
                    e);
                throw;
            }
        }
    }
}