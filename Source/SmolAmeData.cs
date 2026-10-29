using Archipelago.MultiClient.Net;
using System;
using System.Collections.Generic;
using System.Linq;
using UnityEngine;

namespace SmolAmeArchipelagoMod.Source
{
    public enum GameMode
    {
        AllStages = 0,
        Bubba = 1,
        CC = 2
    }

    public class RegionData
    {
        public Dictionary<string, int> Items { get; set; }
        public Dictionary<string, int> Checks { get; set; }

        public RegionData() 
        {
            Items = new Dictionary<string, int>();
            Checks = new Dictionary<string, int>();
        }
    }

    internal static class SmolAmeData
    {
        public static string Server;
        public static int Port;
        public static string Slot;
        public static string Password;

        public static ArchipelagoSession Session;
        public static LoginSuccessful Result;

        public static GameMode Mode = GameMode.AllStages;
        public static bool IncludeCA;

        internal static HashSet<string> MOMButtonsPressed = new HashSet<string>();
        
        internal static int BaseGameBubbaCount = 39;
        internal static int CABubbaCount = 30;
        internal static int BubbaCollected = 0;

        internal static int BaseGameStachQuotaCount = 13;
        internal static int CAStachQuotaCount = 10;
        internal static int StacheQuotaCollected = 0;

        internal static Sprite APCoinSprite;
        internal static Texture2D APCoinTexture;
        internal static GameObject APsEmpty;
        internal static Animator[] APAnims;
        internal static bool[] APCoins = new bool[3];

        internal static readonly HashSet<string> NonLevelRegions =
            new HashSet<string> { "Hub", "Character Select" };


        //internal static Dictionary<string, Dictionary<string, string>> AreaItems;
        internal static Dictionary<string, RegionData> RegionsData;

        internal static Dictionary<string, string> BaseGameNameToSceneName =
            new Dictionary<string, string>()
            {
                { "Hub", "LevelSelect" },
                { "Ame's Office", "Level1" },
                { "Pekoland", "Pekoland" },
                { "What A Joke", "Bettel" },
                { "Ame's Office REVENGEANCE", "OfficeReverse" },
                { "Inascapable Madness", "InascapableMadness" },
                { "To The Moon", "Moon" },
                { "Inumore", "Inumore" },
                { "Mogu Mogu", "MoguMogu" },
                { "Pop On Rocks", "PopOnRocks" },
                { "Here Comes Hope", "HereComesHope" },
                { "Reflect", "Reflect" },
                { "Red Heart", "BigRedHeart" },
                { "NOTHING", "NothingBeatsAGroundPound" },
                { "Character Select", "CharacterSelect" }
            };

        internal static Dictionary<string, string> CANameToSceneName = 
            new Dictionary<string, string>()
            {
                
                { "Sweet Dreams", "SweetDreams" },
                { "Play Dice", "PlayDice" },
                { "Graveyard Groove", "Graveyard Groove" },
                { "BauDol", "BauDol" },
                { "Daydream", "DayDream" },
                { "Say! Fanfare!", "Say! Fanfare!" },
                { "ALiCE&u", "ALiCE&u" },
                { "Mage of Violet", "Mage of Violet" },
                { "HOLOGRAM CIRCUS", "HOLOGRAMCircus" },
                { "Farewell", "Farewell" }
            };

        internal static Dictionary<string, string> CommonNametoSceneName =
            BaseGameNameToSceneName
                .Concat(CANameToSceneName)
                .ToDictionary(pair => pair.Key, pair => pair.Value);

        internal static Dictionary<string, string> SceneNametoCommonName =
            CommonNametoSceneName.ToDictionary(
                pair => pair.Value,
                pair => pair.Key
            );

        internal static Dictionary<string, Dictionary<string, bool>> AreaItemCollected = new Dictionary<string, Dictionary<string, bool>>();
        internal static Dictionary<string, Dictionary<string, bool>> AreaChecksCollected = new Dictionary<string, Dictionary<string, bool>>();
        internal static Dictionary<int, Tuple<string, string>> LocationIDToCheck = new Dictionary<int, Tuple<string, string>>();
    }
}
