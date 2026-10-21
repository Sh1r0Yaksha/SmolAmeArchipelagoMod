using BepInEx;
using BepInEx.Logging;
using HarmonyLib;
using Newtonsoft.Json;
using Smol_Ame_Archipelago;
using SmolAmeArchipelagoMod.Source;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Net;
using UnityEngine;

namespace SmolAmeArchipelago
{
    [BepInPlugin(GUID, NAME, VERSION)]
    [BepInDependency("com.Shir0Yaksha.SmolAmePauseMenuCursor")]
    internal class ArchipelagoPlugin : BaseUnityPlugin
    {
        public const string GUID = "com.Shir0Yaksha.SmolAmeArchipelagoMod";
        public const string NAME = "Smol Ame Archipelago Plugin";
        public const string VERSION = "0.1.1";

        internal static ManualLogSource Log;


        private Harmony Harmony;

        private void Awake()
        {
            Log = base.Logger;
            Log.LogInfo($"[Archipelago Client]{NAME} v{VERSION} loaded!");
            
            string AreaItemsJSON = GetAreaItemsJSON();
            SmolAmeData.AreaItems = JsonConvert.DeserializeObject<
                Dictionary<string, Dictionary<string, string>>
            >(AreaItemsJSON);

            SmolAmeData.AreaLocationCollected = SmolAmeData.AreaItems.ToDictionary(
                area => area.Key,
                area => area.Value.Values.ToDictionary(
                    item => item,
                    item => false
                )
            );

            SmolAmeData.LocationNameToID =
                SmolAmeData.AreaItems
                .SelectMany(region => region.Value.Select(item => item.Value)
                    .Select((locationName, index) => new
                    {
                        Name = $"{region.Key}-{locationName}",
                        Index = index
                    })
                )
                .Select((location, index) => new
                {
                    location.Name,
                    ID = index + 1
                })
                .ToDictionary(location => location.Name, location => location.ID);

            Log.LogInfo($"[Archipelago Client]JSON Loaded");

            GameObject ArchipelagoClient = new GameObject("ArchipelagoClient");
            ArchipelagoClient.AddComponent<ArchipelagoClientScript>();
            ArchipelagoClient.AddComponent<RandomizerPatches>();

            DontDestroyOnLoad( ArchipelagoClient );

            Harmony = new Harmony(GUID);
            Harmony.PatchAll();
        }

        private void OnDestroy()
        {
            Harmony?.UnpatchSelf();
        }

        private string GetAreaItemsJSON()
        {
            string jsonURL = "https://raw.githubusercontent.com/Sh1r0Yaksha/SmolAmeAPWorld/main/src/Area_items.json";

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
                return reader.ReadToEnd();
            }
        }
    }
}
