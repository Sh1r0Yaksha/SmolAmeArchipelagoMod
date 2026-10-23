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
using UnityEngine.UI;


namespace Smol_Ame_Archipelago
{
    internal class RandomizerPatches : MonoBehaviour
    {
        // Item Name: "{region_name}-{item_name}"
        // Location Name: "{region_name}-{location_name}"
        static string RegionName;
        Dictionary<string, bool> ItemsCollectedInTheRegion;
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
            ItemsCollectedInTheRegion = null;
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
                    ChangeMOMButtonCollectedState(Buttons[i], false);
            }

            OnItemReceived();

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

            ArchipelagoClientScript.CollectCompletedLocations();

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
                }
            }

            ArchipelagoPlugin.Log.LogInfo(
                $"Finished initializing region: {RegionName}");
        }




        void Update()
        {
            if (string.IsNullOrEmpty(RegionName) ||
                LocationsCollectedInTheRegion == null ||
                LevelManager.levelManager == null ||
                SmolAmeClient.Session == null)
                return;


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
                            LocationsCollectedInTheRegion[coinName] = true;
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
                        LocationsCollectedInTheRegion[stacheQuota] = true;
                        SendLocationCheck(locationName);
                    }

                }
            }
            // Sending Stache Quota Checks
            CheckMomButtonPressed();
        }

        public void OnItemReceived()
        {
            if (string.IsNullOrEmpty(RegionName))
                return;

            if (SmolAmeData.AreaItemCollected == null ||
                !SmolAmeData.AreaItemCollected.TryGetValue(
                RegionName, out var regionItems))
            {
                return;
            }

            ItemsCollectedInTheRegion = regionItems;


            if (Buttons == null)
            {
                ArchipelagoPlugin.Log.LogWarning("[Archipelago Client] Can't find Buttons in the Level");
                return;
            }

            if (LevelManager.levelManager == null)
                return;

            if (SmolAmeClient.Session == null)
                return;

            if (Buttons.Length > 0 &&
                ItemsCollectedInTheRegion.TryGetValue("MOM Button 1", out bool mom1Collected))
                ChangeMOMButtonCollectedState(Buttons[0], mom1Collected);

            if (Buttons.Length > 1 &&
                ItemsCollectedInTheRegion.TryGetValue("MOM Button 2", out bool mom2Collected))
                ChangeMOMButtonCollectedState(Buttons[1], mom2Collected);
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
            return SmolAmeData.GameNametoCommonName.TryGetValue(sceneName, out var name) ? name : null;
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

        private void ChangeMOMButtonCollectedState(BoxCollider2D button, bool collected)
        {
            if (button == null) return;

            button.enabled = collected;
            foreach (var renderer in button.GetComponentsInChildren<Renderer>(true))
            {
                renderer.enabled = collected;
            }
        }
        private void CheckMomButtonPressed()
        {
            if (Buttons == null || VictoryField == null)
                return;

            CheckMomButton(0, "MOM Button 1");
            CheckMomButton(1, "MOM Button 2");
        }

        private void CheckMomButton(int index, string buttonName)
        {
            if (index >= Buttons.Length || Buttons[index] == null)
                return;

            if (!LocationsCollectedInTheRegion.TryGetValue(buttonName, out bool alreadyCollected) ||
                alreadyCollected)
                return;

            var levelButton = Buttons[index].GetComponent<LevelClearButton>();
            if (levelButton == null || !(bool)VictoryField.GetValue(levelButton))
                return;

            LocationsCollectedInTheRegion[buttonName] = true;
            SendLocationCheck($"{RegionName}-{buttonName}");
            MOMButtonPressed = true;
            SmolAmeData.MOMButtonsPressed++;
        }

        void SetStacheQuota(bool value)
        {
            if (LevelManager.levelManager == null) return;

            if (value)
            {
                LevelManager.levelManager.collectedMoustaches = LevelManager.levelManager.moustacheQuotaInt;
            }
        }
    }
}
