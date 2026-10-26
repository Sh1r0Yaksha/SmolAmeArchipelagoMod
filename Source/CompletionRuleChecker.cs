using System;
using System.Collections.Generic;
using System.Linq;
using UnityEngine;

namespace SmolAmeArchipelagoMod.Source
{
    internal class CompletionRuleChecker : MonoBehaviour
    {
        static int RequiredBubbaCount => 
            SmolAmeData.BaseGameBubbaCount + (SmolAmeData.IncludeCA ?       SmolAmeData.CABubbaCount : 0);

        static int RequiredStacheQuotaCount =>
            SmolAmeData.BaseGameStachQuotaCount + (SmolAmeData.IncludeCA ? SmolAmeData.CAStachQuotaCount: 0);

        void Update()
        {
            if (SmolAmeData.Mode == GameMode.CC)
            {
                if (!IsAllStacheQuotaCollected())
                    return;
            }
            if (SmolAmeData.Mode == GameMode.Bubba)
            {
                if (!IsAllBubbaCollected())
                    return;
            }
            if (IsAllStagesGoalMet())
            {
                SmolAmeData.Session.SetGoalAchieved();
            }
        }

        
        internal static bool IsAllStagesGoalMet()
        {
            IEnumerable<string> required = SmolAmeData.BaseGameNameToSceneName.Keys
                .Where(name => !SmolAmeData.NonLevelRegions.Contains(name));

            if (SmolAmeData.IncludeCA)
                required = required.Concat(SmolAmeData.CANameToSceneName.Keys);

            return required.All(SmolAmeData.MOMButtonsPressed.Contains);
        }

        internal static bool IsAllStacheQuotaCollected()
        {
            return SmolAmeData.StacheQuotaCollected >= RequiredStacheQuotaCount;
        }

        internal static bool IsAllBubbaCollected()
        {
            return SmolAmeData.BubbaCollected >= RequiredBubbaCount;
        }
    }
}
