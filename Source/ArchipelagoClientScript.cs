using Archipelago.MultiClient.Net.Helpers;
using Archipelago.MultiClient.Net.Models;
using SmolAmeArchipelago;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using UnityEngine;

namespace SmolAmeArchipelagoMod.Source
{
    internal class ArchipelagoClientScript : MonoBehaviour
    {
        
        public static void OnConnectionInvoked()
        {
            ArchipelagoPlugin.Log.LogInfo("[Archipelago Client] Connection Invoked");
            string server = PauseScreenPatch.Server.text;
            int port = int.Parse(PauseScreenPatch.Port.text);
            string slot = PauseScreenPatch.Slot.text;
            try
            {
                bool tryConnect = SmolAmeClient.Connect(server, port, slot, string.Empty);
                if (tryConnect)
                {
                    SmolAmeClient.Session.Items.ItemReceived += ReceivedItem;
                }
                ArchipelagoPlugin.Log.LogInfo($"[Archipelago Client] Connection result: {tryConnect}");
            }
            catch (Exception ex)
            {
                ArchipelagoPlugin.Log.LogError($"[Archipelago Client] Could not connect: {ex.Message}");
            }
        }

        private static void ReceivedItem(ReceivedItemsHelper helper)
        {
            ItemInfo recieved = helper.DequeueItem();

            string[] parts = recieved.LocationName.Split(new[] { '-' }, 2);

            if (parts.Length == 2)
            {
                string regionName = parts[0];
                string location = parts[1];

                SmolAmeData.AreaLocationCollected[regionName][location] = true;
            }
            else
            {
                ArchipelagoPlugin.Log.LogInfo("[Archipelago Client] Item received but cannot parse its region and item name");
            }
        }
    }
}
