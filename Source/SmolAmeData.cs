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
                { "Pekoland", "Pekoland" }
            };

        internal static Dictionary<string, string> GameNametoCommonName =
            CommonNametoGameName.ToDictionary(
                pair => pair.Value,
                pair => pair.Key
            );

        internal static Dictionary<string, int> GameNametoInt =
            new Dictionary<string, int>()
            {
                { "LevelSelect",                0 },
                { "Level1",                     1 },
                { "PopOnRocks",                 2 },
                { "BigRedHeart",                3 },
                { "Pekoland",                   4 },
                { "OfficeReverse",              5 },
                { "Moon",                       6 },
                { "NothingBeatsAGroundPound",   7 },
                { "MoguMogu",                   8 },
                { "Inumore",                    9 },
                { "InascapableMadness",         10 },
                { "HereComesHope",              11 },
                { "Reflect",                    12 },
                { "CharacterSelect",            13 },
                { "Bettel",                     14 },
                { "Pettan",                     15 },
            };

        internal static Dictionary<string, Dictionary<string, bool>> AreaLocationCollected;

        internal static Dictionary<string, int> LocationNameToID;
    }
}
