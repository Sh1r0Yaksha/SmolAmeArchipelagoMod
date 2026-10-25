using System.Collections.Generic;
using System.Linq;

namespace SmolAmeArchipelagoMod.Source
{
    internal static class SmolAmeData
    {
        internal static int MOMButtonsPressed = 0;
        internal static int TotalMOMButtons => CommonNametoGameName.Count - 1;

        internal static Dictionary<string, Dictionary<string, string>> AreaItems;

        internal static Dictionary<string, string> CommonNametoGameName = 
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
                { "Character Select", "CharacterSelect" },
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

        internal static Dictionary<string, string> GameNametoCommonName =
            CommonNametoGameName.ToDictionary(
                pair => pair.Value,
                pair => pair.Key
            );

        internal static Dictionary<string, Dictionary<string, bool>> AreaItemCollected;
        internal static Dictionary<string, Dictionary<string, bool>> AreaLocationCollected;

        internal static Dictionary<string, int> LocationNameToID;
        internal static Dictionary<int, string> LocationIDToName;
    }
}
