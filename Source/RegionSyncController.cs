using HarmonyLib;
using SmolAmeArchipelago;
using SmolAmeArchipelagoMod.Source;
using System.Collections;
using System.Collections.Generic;
using System.Reflection;
using UnityEngine;
using UnityEngine.SceneManagement;


namespace Smol_Ame_Archipelago
{
    internal class RegionSyncController : MonoBehaviour
    {
        static string RegionName;
        Dictionary<string, bool> ItemsCollectedInTheRegion;
        Dictionary<string, bool> LocationsCollectedInTheRegion;
        TokenScript[] Bubba;
        LevelClearButton[] MOMButtons;

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

            StartCoroutine(InitializeRegion(Scene.name));
        }

        private IEnumerator InitializeRegion(string sceneName)
        {
            // Wait one frame for the scene objects to initialize.
            yield return null;

            RegionName = GetRegionName(sceneName);

            if (string.IsNullOrEmpty(RegionName))
            {
                yield break;
            }

            if (string.Equals(RegionName, "Hub"))
            {
                foreach (var empty in LevelEmptyManager.manager.empties)
                {
                    empty.gameObject.SetActive(true);
                }
            }
            else
            {
                if (SmolAmeData.Session == null || !SmolAmeData.Session.Socket.Connected)
                {
                    InfoHUD.Set("Session is not connected... Switching back to the Hub");
                    LevelLoader.loader.LoadLevel(0);
                }
                
            }

            // Find Bubba tokens in the scene.
            var bubbas = FindObjectsOfType<TokenScript>();
            Bubba = new TokenScript[bubbas.Length];

            foreach (var bubba in bubbas)
            {
                Bubba[bubba.tokenNum] = bubba;
            }

            MOMButtons = FindObjectsOfType<LevelClearButton>();

            // Process MOM buttons.
            foreach (var button in MOMButtons)
            {
                if (button == null)
                    continue;

                ChangeGameObjectState(button.gameObject, false);
            }

            // Process Bubba tokens.
            foreach (var token in Bubba)
            {
                if (token == null)
                    continue;

                ChangeGameObjectState(token.gameObject, false);
            }

            OnItemReceived();

            // Ensure the region location data exists.
            if (SmolAmeData.AreaLocationCollected == null ||
                !SmolAmeData.AreaLocationCollected.TryGetValue(
                    RegionName, out LocationsCollectedInTheRegion))
            {
                yield break;
            }

            SmolAmeClient.CollectCompletedLocations();

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
        }

        void Update()
        {
            if (string.IsNullOrEmpty(RegionName) ||
                LocationsCollectedInTheRegion == null ||
                LevelManager.levelManager == null ||
                SmolAmeData.Session == null)
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
                            SmolAmeData.BubbaCollected++;
                            SendLocationCheck(locationName);
                        }
                    }
                }
            }

            // Sending Stache Quota Checks
            if (LevelManager.levelManager.collectedMoustaches >= LevelManager.levelManager.moustacheQuotaInt)
            {
                string stacheQuota = "Stache Quota";
                string locationName = $"{RegionName}-{stacheQuota}";
                if (LocationsCollectedInTheRegion.TryGetValue(
                    stacheQuota, out bool alreadyCollected) &&
                !alreadyCollected)
                {
                    LocationsCollectedInTheRegion[stacheQuota] = true;
                    SmolAmeData.StacheQuotaCollected++;
                    
                    SendLocationCheck(locationName);
                }

            }

            
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

            if (LevelManager.levelManager == null)
                return;

            if (SmolAmeData.Session == null)
                return;

            if (MOMButtons != null && MOMButtons.Length > 0 &&
                MOMButtons[0] != null &&
                ItemsCollectedInTheRegion.TryGetValue(
                    "MOM Button 1", out bool mom1Collected))
            {
                ChangeGameObjectState(MOMButtons[0].gameObject, mom1Collected);
            }

            if (MOMButtons != null && MOMButtons.Length > 1 &&
                MOMButtons[1] != null &&
                ItemsCollectedInTheRegion.TryGetValue(
                    "MOM Button 2", out bool mom2Collected))
            {
                ChangeGameObjectState(MOMButtons[1].gameObject, mom2Collected);
            }

            // Enable or disable Bubba coins.
            if (Bubba == null)
                return;

            for (int i = 0; i < Bubba.Length && i < 3; i++)
            {
                if (Bubba[i] == null)
                    continue;

                string itemName = $"Bubba Coin {i + 1}";

                if (ItemsCollectedInTheRegion.TryGetValue(
                    itemName, out bool coinReceived))
                {
                    ChangeGameObjectState(Bubba[i].gameObject, coinReceived);
                }
            }
        }

        string GetRegionName(string sceneName)
        {
            return SmolAmeData.SceneNametoCommonName.TryGetValue(sceneName, out var name) ? name : null;
        }

        void SendLocationCheck(string locationName)
        {
            if (SmolAmeData.Session == null)
            {
                return;
            }

            if (!SmolAmeData.LocationNameToID.TryGetValue(locationName, out int id))
            {
                return;
            }
            InfoHUD.Set($"Collected {locationName}");
            SmolAmeData.Session.Locations.CompleteLocationChecks(id);
        }

        private void ChangeGameObjectState(GameObject gameObject, bool collected)
        {
            if (gameObject == null) return;

            gameObject.SetActive(collected);
        }

        private static readonly FieldInfo DeadField =
            AccessTools.Field(typeof(TokenScript), "dead");

        private static readonly FieldInfo VictoryField =
            AccessTools.Field(typeof(LevelClearButton), "victory");

        private void SetBubbaDead(int index, bool value)
        {
            if (Bubba == null || index >= Bubba.Length)
            {
                return;
            }

            if (Bubba[index] == null || DeadField == null)
            {
                return;
            }

            DeadField.SetValue(Bubba[index], value);
        }

        private void CheckMomButtonPressed()
        {
            if (MOMButtons == null || VictoryField == null)
                return;

            CheckMomButton(0, "MOM Button 1");
            CheckMomButton(1, "MOM Button 2");
        }

        private void CheckMomButton(int index, string buttonName)
        {
            if (index >= MOMButtons.Length || MOMButtons[index] == null)
                return;

            if (!LocationsCollectedInTheRegion.TryGetValue(buttonName, out bool alreadyCollected) ||
                alreadyCollected)
                return;

            var levelButton = MOMButtons[index];
            if (levelButton == null || !(bool)VictoryField.GetValue(levelButton))
                return;

            LocationsCollectedInTheRegion[buttonName] = true;
            SendLocationCheck($"{RegionName}-{buttonName}");
            SmolAmeData.MOMButtonsPressed.Add(RegionName);
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
