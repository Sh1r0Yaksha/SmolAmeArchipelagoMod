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
            RegionName = null;
            LocationsCollectedInTheRegion = null;
            Bubba = null;
            Buttons = null;
            MOMButtonPressed = false;

            StartCoroutine(InitializeRegion(Scene.name));

            
        }


        private IEnumerator InitializeRegion(string sceneName)
        {
            // Wait one frame for the scene objects to initialize.
            yield return null;

            RegionName = GetRegionName(sceneName);

            if (string.IsNullOrEmpty(RegionName))
            {
                ArchipelagoPlugin.Log.LogWarning(
                    $"No region mapping found for scene: {sceneName}");
                yield break;
            }

            ArchipelagoPlugin.Log.LogInfo(
                $"Initializing region: {RegionName}");

            // Find Bubba tokens in the scene.
            Bubba = FindObjectsOfType<TokenScript>();

            // Find level-clear buttons and their colliders.
            LevelClearButton[] levelButtons =
                FindObjectsOfType<LevelClearButton>();

            Buttons = new BoxCollider2D[levelButtons.Length];

            for (int i = 0; i < levelButtons.Length; i++)
            {
                Buttons[i] = levelButtons[i].GetComponent<BoxCollider2D>();

                ArchipelagoPlugin.Log.LogInfo(
                    $"Found level button [{i}]: " +
                    $"{levelButtons[i].gameObject.name}");

                // Disable the collider until its location is collected.
                if (Buttons[i] != null)
                    Buttons[i].enabled = false;
            }

            MOMButtonPressed = false;

            // Ensure the region location data exists.
            if (SmolAmeData.AreaLocationCollected == null ||
                !SmolAmeData.AreaLocationCollected.TryGetValue(
                    RegionName, out LocationsCollectedInTheRegion))
            {
                ArchipelagoPlugin.Log.LogError(
                    $"No location data found for region: {RegionName}");
                yield break;
            }

            ArchipelagoPlugin.Log.LogInfo(
                $"Loaded {LocationsCollectedInTheRegion.Count} locations " +
                $"for region: {RegionName}");

            // Apply locations that have already been collected.
            foreach (var location in LocationsCollectedInTheRegion)
            {
                if (!location.Value)
                    continue;

                string locationName = location.Key;

                if (RegionName == "Hub")
                {
                    // Add Hub-specific location handling here if needed.
                    continue;
                }

                switch (locationName)
                {
                    case "Bubba Coin 1":
                        SetBubbaDead(0, true);
                        break;

                    case "Bubba Coin 2":
                        SetBubbaDead(1, true);
                        break;

                    case "Bubba Coin 3":
                        SetBubbaDead(2, true);
                        break;

                    case "Stache Quota":
                        SetStacheQuota(true);
                        break;

                    case "MOM Button":
                        // This assumes Buttons[0] is the MOM Button.
                        // Replace this with the actual MOM Button reference
                        // if multiple level-clear buttons exist.
                        if (Buttons.Length > 0 && Buttons[0] != null)
                        {
                            Buttons[0].enabled = true;
                        }
                        else
                        {
                            ArchipelagoPlugin.Log.LogWarning(
                                "MOM Button collider was not found.");
                        }
                        break;
                }
            }

            ArchipelagoPlugin.Log.LogInfo(
                $"Finished initializing region: {RegionName}");
        }




        void Update()
        {
            if (string.IsNullOrEmpty(RegionName))
                return;

            if (SmolAmeData.AreaLocationCollected == null ||
                !SmolAmeData.AreaLocationCollected.TryGetValue(
                RegionName, out var regionLocations))
            {
                return; 
            }

            LocationsCollectedInTheRegion = regionLocations;


            if (Buttons == null)
            {
                ArchipelagoPlugin.Log.LogWarning("[Archipelago Client] Can't find Buttons in the Level");
                return;
            }

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
                        string coinName = $"Bubba Coin {i + 1}";
                        string locationName = $"{RegionName}-{coinName}";
                        if (LocationsCollectedInTheRegion.TryGetValue(
                            coinName, out bool alreadyCollected) &&
                            !alreadyCollected)
                        {
                            SendLocationCheck(locationName);
                        }
                    }
                }
            } 
            {
                if (LevelManager.levelManager.collectedMoustaches >= LevelManager.levelManager.moustacheQuotaInt)
                {
                    string stacheQuota = "Stache Quota";
                    string locationName = $"{RegionName}-{stacheQuota}";
                    if (LocationsCollectedInTheRegion.TryGetValue(
                        stacheQuota, out bool alreadyCollected) &&
                    !alreadyCollected)
                    {
                        SendLocationCheck(locationName);
                    }

                }
            }
            // Sending Stache Quota Checks
            CheckMomButtonPressed();
        }

        void SendLocationCheck(string locationName)
        {
            if (SmolAmeClient.Session == null)
            {
                ArchipelagoPlugin.Log.LogWarning("Cannot send location check: session is null.");
                return;
            }

            if (!SmolAmeData.LocationNameToID.TryGetValue(locationName, out int id))
            {
                ArchipelagoPlugin.Log.LogError(
                    $"Location ID not found for '{locationName}'.");
                return;
            }

            ArchipelagoPlugin.Log.LogInfo($"Sending location check: {locationName} (ID: {id})");

            SmolAmeClient.Session.Locations.CompleteLocationChecks(id);
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
                    SendLocationCheck(locationName);
                    MOMButtonPressed = true;
                    SmolAmeData.MOMButtonsPressed++;
                    break;
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
