using HarmonyLib;
using SmolAmeArchipelago;
using SmolAmeArchipelagoMod.Source;
using System;
using System.Collections;
using System.Collections.Generic;
using System.Reflection;
using System.Runtime.Remoting.Messaging;
using UnityEngine;
using UnityEngine.SceneManagement;


namespace Smol_Ame_Archipelago
{
    internal class RandomizerPatches : MonoBehaviour
    {
        // Item Name: "{region_name}-{item_name}"
        // Location Name: "{region_name}-{location_name}"
        static string RegionName;
        Dictionary<string, bool> LocationsCollectedInTheRegion;
        TokenScript[] Bubba;
        BoxCollider2D[] Buttons;
        bool MOMButtonPressed;

        //private static LevelClearButton TargetMOMButton;
        //private static bool MOMButtonPoundDisabled;

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
            var b = FindObjectsOfType<LevelClearButton>();
            Buttons = new BoxCollider2D[b.Length];

            for (int i = 0; i < b.Length; i++)
            {
                Buttons[i] = b[i].GetComponent<BoxCollider2D>();
                Buttons[i].enabled = false;
                //TargetMOMButton = Buttons[i];
                //ChangeMOMState(false);
            }
            MOMButtonPressed = false;

            if (!SmolAmeData.AreaLocationCollected.TryGetValue(
                    RegionName, out Dictionary<string, bool> locationsCollected))
            {
                ArchipelagoPlugin.Log.LogError($"No location data found for region: {RegionName}");
                yield break;
            }

            LocationsCollectedInTheRegion = SmolAmeData.AreaLocationCollected[RegionName];
                
            // Give checks already collected
            foreach (var location in LocationsCollectedInTheRegion)
            {
                if (!location.Value)
                    continue;

                string locationName = location.Key;
                if (RegionName == "Hub")
                {
                    // For Future if anything in the Hub
                }
                else
                {
                    if (string.Equals(locationName, "Bubba Coin 1"))
                    {
                        SetBubbaDead(0, location.Value);
                    }
                    else if (string.Equals(locationName, "Bubba Coin 2"))
                    {
                        SetBubbaDead(1, location.Value);
                    }
                    else if (string.Equals(locationName, "Bubba Coin 3"))
                    {
                        SetBubbaDead(2, location.Value);
                    }
                    else if (string.Equals(locationName, "Stache Quota"))
                    {
                        SetStacheQuota(location.Value);
                    }
                    else if (string.Equals(locationName, "MOM Button"))
                    {
                        Buttons[0].enabled = true;
                        
                    }
                }
            }
        }

        

        void Update()
        {
            if (LocationsCollectedInTheRegion == null)
                return;

            if (Buttons == null)
                return;

            // Don't run until the game and Archipelago session are ready.
            if (string.IsNullOrEmpty(RegionName))
                return;

            if (LevelManager.levelManager == null)
                return;

            if (SmolAmeClient.Session == null)
                return;

            // Receive Important Checks in Real Time:
            if (LocationsCollectedInTheRegion.TryGetValue("MOM Button", out bool momCollected))
                {
                if (Buttons.Length > 0)
                {
                    Buttons[0].enabled = momCollected;
                }
                else
                {
                    ArchipelagoPlugin.Log.LogWarning("[Archipelago Client] No Button Items were found");
                }
                
            }

            // Sending Bubba Checks
            for (int i = 0; i < 3; i++)
            {
                if (LevelManager.levelManager.bubbaTokens != null)
                {
                    if (LevelManager.levelManager.bubbaTokens[i])
                    {
                        string locationName = $"{RegionName}-Bubba Coin {i + 1}";
                        SmolAmeClient.Session.Locations.CompleteLocationChecks(SmolAmeData.LocationNameToID[locationName]);
                    }
                }
            } 
            {
                if (LevelManager.levelManager.collectedMoustaches >= LevelManager.levelManager.moustacheQuotaInt)
                {
                    string locationName = $"{RegionName}-Stache Quota";
                    SmolAmeClient.Session.Locations.CompleteLocationChecks
                        (SmolAmeData.LocationNameToID[locationName]);
                }
            }
            // Sending Stache Quota Checks
            CheckMomButtonPressed();
        }

        string GetRegionName(string sceneName)
        {
            return SmolAmeData.GameNametoCommonName[sceneName];
        }

        private static readonly FieldInfo DeadField =
            AccessTools.Field(typeof(TokenScript), "dead");

        private static readonly FieldInfo VictoryField =
            AccessTools.Field(typeof(LevelClearButton), "victory");

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

        private void CheckMomButtonPressed()
        {
            if (MOMButtonPressed || 
                Buttons == null || 
                VictoryField == null)
                return;

            if (!LocationsCollectedInTheRegion.TryGetValue(
            "MOM Button", out bool momCollected) || momCollected)
                return;

            string locationName = $"{RegionName}-MOM Button";

            foreach (var button in Buttons)
            {
                if (button == null)
                    continue;

                bool victory = (bool)VictoryField.GetValue(button.GetComponent<LevelClearButton>());

                if (victory)
                {
                    if (SmolAmeData.LocationNameToID.TryGetValue(
                            locationName, out int locationID))
                    {
                        SmolAmeData.MOMButtonsPressed++;
                        SmolAmeClient.Session.Locations.CompleteLocationChecks(locationID);

                        MOMButtonPressed = true;
                        break;
                    }
                }
            }
        }

        void SetStacheQuota(bool value)
        {
            if (value)
            {
                LevelManager.levelManager.collectedMoustaches = LevelManager.levelManager.moustacheQuotaInt;
            }
        }
    }
}
