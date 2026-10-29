using HarmonyLib;
using SmolAmeArchipelago;
using SmolAmeArchipelagoMod.Source;
using System.Collections;
using System.Collections.Generic;
using System.Linq;
using System.Reflection;
using System.Reflection.Emit;
using UnityEngine;
using UnityEngine.SceneManagement;


namespace Smol_Ame_Archipelago
{
    internal class RegionSyncController : MonoBehaviour
    {
        static string RegionName;
        Dictionary<string, bool> ItemsCollectedInTheRegion;
        Dictionary<string, bool> LocationsCollectedInTheRegion;
        TokenScript[] APCoins;
        LevelClearButton[] MOMButtons;
        
        int lastStache = -1;

        void Awake() 
        {
            SceneManager.sceneLoaded += OnSceneLoaded;
        }

        void Start()
        {
            if (GetRegionName(SceneManager.GetActiveScene().name) == "Hub")
                StartCoroutine(ActivateHubEmptiesWhenReady());
        }

        private void OnDestroy()
        {
            SceneManager.sceneLoaded -= OnSceneLoaded;
        }

        private void OnSceneLoaded(Scene Scene, LoadSceneMode mode)
        {
            RegionName = null;
            ItemsCollectedInTheRegion = null;
            APCoins = null;

            StartCoroutine(InitializeRegion(Scene.name));
        }

        IEnumerator ActivateHubEmptiesWhenReady()
        {
            Scene scene = SceneManager.GetActiveScene();

            for (int frame = 0; ; frame++)
            {
                var m = LevelEmptyManager.manager;
                if (m != null && m.empties != null && m.empties.Any()) break;

                if (!scene.isLoaded) yield break;   // we left the scene while waiting
                if (frame > 600)                    // roughly 10 seconds
                {
                    Dbg.Warn("Region", "LevelEmptyManager never became ready");
                    yield break;
                }
                yield return null;
            }

            yield return null;   // one more frame so the manager's own Start has finished

            if (!scene.isLoaded || LevelEmptyManager.manager == null) yield break;

            foreach (var empty in LevelEmptyManager.manager.empties)
                if (empty != null) empty.gameObject.SetActive(true);
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
                StartCoroutine(ActivateHubEmptiesWhenReady());  
            }
            else
            {
                if (SmolAmeData.Session == null || !SmolAmeData.Session.Socket.Connected)
                {
                    HUD.Set("Session is not connected... Switching back to the Hub");
                    LevelLoader.loader.LoadLevel(0);
                }
                
            }

            // Find Bubba tokens in the scene.
            var apCoins = FindObjectsOfType<TokenScript>();
            APCoins = new TokenScript[apCoins.Length];

            foreach (var coin in apCoins)
            {
                APCoins[coin.tokenNum] = coin;
            }

            MOMButtons = FindObjectsOfType<LevelClearButton>();

            // Process MOM buttons.
            foreach (var button in MOMButtons)
            {
                if (button == null)
                    continue;

                ChangeGameObjectState(button.gameObject, false);
            }

            // Process APCoins.
            for (int i = 0; i < SmolAmeData.APCoins.Length; i++)
            {
                SmolAmeData.APCoins[i] = false;
                HUD.ReplaceTexture(APCoins[i], SmolAmeData.APCoinTexture); 
            }
            if (LevelManager.levelManager != null)
                Dbg.Info("Region", $"{RegionName}: moustaches {LevelManager.levelManager.collectedMoustaches}/{LevelManager.levelManager.moustacheQuotaInt}");

            OnItemReceived();

            // Ensure the region location data exists.
            if (SmolAmeData.AreaChecksCollected == null ||
                !SmolAmeData.AreaChecksCollected.TryGetValue(
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
                        SetAPCoinDead(0, location.Value);
                        break;

                    case "Bubba Coin 2":
                        SetAPCoinDead(1, location.Value);
                        break;

                    case "Bubba Coin 3":
                        SetAPCoinDead(2, location.Value);
                        break;

                    case "Stache Quota":
                        SetStacheQuota(location.Value);
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
                if (SmolAmeData.APCoins != null)
                {
                    if (SmolAmeData.APCoins[i])
                    {
                        string coinName = $"Bubba Coin {i + 1}";
                        if (LocationsCollectedInTheRegion.TryGetValue(
                            coinName, out bool alreadyCollected) &&
                            !alreadyCollected)
                        {
                            LocationsCollectedInTheRegion[coinName] = true;
                            SmolAmeData.BubbaCollected++;
                            SendLocationCheck(coinName);
                        }
                    }
                }
            }

            // Sending Stache Quota Checks
            if (LevelManager.levelManager.collectedMoustaches >= LevelManager.levelManager.moustacheQuotaInt)
            {
                string stacheQuota = "Stache Quota";
                if (LocationsCollectedInTheRegion.TryGetValue(
                    stacheQuota, out bool alreadyCollected) &&
                !alreadyCollected)
                {
                    LocationsCollectedInTheRegion[stacheQuota] = true;
                    SmolAmeData.StacheQuotaCollected++;
                    
                    SendLocationCheck(stacheQuota);
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

            if  (MOMButtons ==  null)
                return;

            if (MOMButtons.Length <= 0)
                return;

            if (MOMButtons[0] == null) 
                return;

            // MOM Button Received
            if (string.Equals(RegionName, "Inumore") ||
                string.Equals(RegionName, "BauDol"))
            {
                if (ItemsCollectedInTheRegion.TryGetValue(
                    "MOM Button 1 Unlock", out bool mom1Collected))
                {
                    ChangeGameObjectState(MOMButtons[0].gameObject, mom1Collected);
                }

                if (MOMButtons.Length > 1 &&
                    MOMButtons[1] != null &&
                    ItemsCollectedInTheRegion.TryGetValue(
                        "MOM Button 2 Unlock", out bool mom2Collected))
                {
                    ChangeGameObjectState(MOMButtons[1].gameObject, mom2Collected);
                }
            }
            else
            {
                if(ItemsCollectedInTheRegion.TryGetValue(
                    "MOM Button Unlock", out bool mom1Collected))
                {
                    ChangeGameObjectState(MOMButtons[0].gameObject, mom1Collected);
                }
            }

            // Bubba Coin received
            for (int i = 0; i < 3; i++)
            {

                string itemName = $"Bubba Coin {i + 1}";

                if (ItemsCollectedInTheRegion.TryGetValue(
                    itemName, out bool coinReceived))
                {
                    LevelManager.levelManager.bubbaTokens[i] = coinReceived;
                }
            }

            // Filler items
            if (SmolAmeData.AreaItemCollected["General"]["Moustache"])
            {
                LevelManager.levelManager.collectedMoustaches++;
                SmolAmeData.AreaItemCollected["General"]["Moustache"] = false;
            }

            if (SmolAmeData.AreaItemCollected["General"]["10 Moustache"])
            {
                LevelManager.levelManager.collectedMoustaches += 10;
                SmolAmeData.AreaItemCollected["General"]["10 Moustache"] = false;
            }
        }

        string GetRegionName(string sceneName)
        {
            return SmolAmeData.SceneNametoCommonName.TryGetValue(sceneName, out var name) ? name : null;
        }

        void SendLocationCheck(string checkName)
        {
            if (SmolAmeData.Session == null)
            {
                return;
            }

            int ID = SmolAmeData.RegionsData[RegionName].Checks[checkName];
            HUD.Set($"Collected {checkName} in {RegionName}");
            SmolAmeData.Session.Locations.CompleteLocationChecks(ID);
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

        private void SetAPCoinDead(int index, bool value)
        {
            if (APCoins == null || index >= APCoins.Length)
            {
                return;
            }

            if (APCoins[index] == null || DeadField == null)
            {
                return;
            }

            DeadField.SetValue(APCoins[index], value);
        }

        private void CheckMomButtonPressed()
        {
            if (MOMButtons == null || VictoryField == null)
                return;

            CheckMomButton(0, "MOM Button");
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
            SendLocationCheck(buttonName);
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

    [HarmonyPatch(typeof(TokenScript), "Update")]
    public static class TokenScript_Update_Patch
    {
        static IEnumerable<CodeInstruction> Transpiler(
            IEnumerable<CodeInstruction> instructions)
        {
            var codes = new List<CodeInstruction>(instructions);

            var tokensField = AccessTools.Field(typeof(LevelManager), "bubbaTokens");
            var replacementField = AccessTools.Field(
                typeof(SmolAmeData), nameof(SmolAmeData.APCoins));

            int patched = 0;

            for (int i = 0; i < codes.Count; i++)
            {
                if (!codes[i].LoadsField(tokensField))
                    continue;

                // ldfld consumed the LevelManager reference; ldsfld doesn't.
                // Pop it first so the stack stays balanced.
                // Reusing this instruction keeps any labels/blocks attached to it.
                codes[i].opcode = OpCodes.Pop;
                codes[i].operand = null;

                codes.Insert(i + 1, new CodeInstruction(OpCodes.Ldsfld, replacementField));

                i++;        // skip the instruction we just inserted
                patched++;
            }

            if (patched == 0)
                UnityEngine.Debug.LogError(
                    "[BepInEx] Failed to find bubbaTokens field in TokenScript.Update!");

            return codes;
        }
    }
}
