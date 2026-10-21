using HarmonyLib;
using SmolAmeArchipelago;
using SmolAmeArchipelagoMod.Source;
using System;
using System.Collections;
using System.Collections.Generic;
using System.Reflection;
using UnityEngine;
using UnityEngine.SceneManagement;


namespace Smol_Ame_Archipelago
{
    internal class RandomizerPatches : MonoBehaviour
    {
        // Item Name: "{region_name}-{item_name}"
        // Location Name: "{region_name}-{location_name}"
        static string RegionName;
        Dictionary<string, string> ItemsLocationInThisRegion;
        TokenScript[] Bubba;
        LevelButtonScript[] Buttons;
        bool StacheQuota;
        
        void Awake() 
        {
            SceneManager.sceneLoaded += OnSceneLoaded;
        }

        private void OnDestroy()
        {
            SceneManager.sceneLoaded -= OnSceneLoaded;
        }

        private void OnSceneLoaded(Scene Scene, LoadSceneMode mode)
        {
            StartCoroutine(InitializeRegion(Scene.name));

            
        }

        private IEnumerator InitializeRegion(string sceneName)
        {

            yield return null;

            RegionName = GetRegionName(sceneName);

            if (string.IsNullOrEmpty(RegionName))
                yield break;

            Bubba = FindObjectsOfType<TokenScript>();
            Buttons = FindObjectsOfType<LevelButtonScript>();

            //foreach (var button in Buttons)
            //{
            //    // button.canPound = False;
            //}
            StacheQuota = false;

            if (!SmolAmeData.AreaLocationCollected.TryGetValue(
                    RegionName, out Dictionary<string, bool> locationsCollected))
            {
                ArchipelagoPlugin.Log.LogError($"No location data found for region: {RegionName}");
                yield break;
            }

            Dictionary<string, bool> LocationsCollected = SmolAmeData.AreaLocationCollected[RegionName];

            foreach (var location in LocationsCollected)
            {
                if (!location.Value)
                    continue;

                if (location.Key.Contains("Bubba Coin 1"))
                {
                    SetBubbaDead(0, true);
                }
                if (location.Key.Contains("Bubba Coin 2"))
                {
                    SetBubbaDead(1, true);
                }
                if (location.Key.Contains("Bubba Coin 3"))
                {
                    SetBubbaDead(2, true);
                }
            }
        }
        void Update()
        {
            for (int i = 0; i < 3; i++)
            {
                if (LevelManager.levelManager.bubbaTokens[i])
                {
                    string locationName = $"{RegionName}-Bubba Coin {i+1}";
                    SmolAmeClient.Session.Locations.CompleteLocationChecks(SmolAmeData.LocationNameToID[locationName]);
                }
            } 
        }

        string GetRegionName(string sceneName)
        {
            return SmolAmeData.GameNametoCommonName[sceneName];
        }

        private static readonly FieldInfo DeadField =
            AccessTools.Field(typeof(TokenScript), "dead");

        private void SetBubbaDead(int index, bool value)
        {
            if (Bubba == null || index >= Bubba.Length)
            {
                ArchipelagoPlugin.Log.LogError($"Bubba token at index {index} was not found.");
                return;
            }

            if (Bubba[index] == null || DeadField == null)
            {
                ArchipelagoPlugin.Log.LogError("Could not access TokenScript.dead.");
                return;
            }

            DeadField.SetValue(Bubba[index], value);
        }
    }
}
