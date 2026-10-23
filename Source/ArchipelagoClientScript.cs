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

                CollectReceivedItems();
                CollectCompletedLocations();
            }
            catch (Exception ex)
            {
                ArchipelagoPlugin.Log.LogError($"[Archipelago Client] Could not connect: {ex.Message}");
            }
        }

        private static void ReceivedItem(ReceivedItemsHelper helper)
        {
            ItemInfo recieved = helper.DequeueItem();
            PutReceivedItemInSmolAme(recieved);
            ArchipelagoPlugin.RandomizerPatches.OnItemReceived();
        }

        static void CollectReceivedItems()
        {
            var items = SmolAmeClient.Session.Items.AllItemsReceived;

            foreach (var item in items)
            {
                PutReceivedItemInSmolAme(item);
            }
        }

        public static void CollectCompletedLocations()
        {
            var locations = SmolAmeClient.Session.Locations.AllLocationsChecked;

            foreach (Int64 location in locations)
            {
                var locationName =  SmolAmeData.LocationIDToName[(int)location];
                PutReceivedLocationInSmolAme(locationName);
            }
        }

        static void PutReceivedItemInSmolAme(ItemInfo item)
        {
            string[] parts = item.ItemName.Split(new[] { '-' }, 2);

            if (parts.Length == 2)
            {
                string regionName = parts[0];
                string itemName = parts[1];

                SmolAmeData.AreaItemCollected[regionName][itemName] = true;
                ArchipelagoPlugin.Log.LogInfo($"Received {item} in {regionName}");
            }
            else
            {
                ArchipelagoPlugin.Log.LogInfo("[Archipelago Client] Item received but cannot parse its region and item name");
            }
        }

        static void PutReceivedLocationInSmolAme(string locationName)
        {
            string[] parts = locationName.Split(new[] { '-' }, 2);

            if (parts.Length == 2)
            {
                string regionName = parts[0];
                string location = parts[1];

                SmolAmeData.AreaLocationCollected[regionName][location] = true;
                ArchipelagoPlugin.Log.LogInfo($"Received {location} in {regionName}");
            }
            else
            {
                ArchipelagoPlugin.Log.LogInfo("[Archipelago Client] Item received but cannot parse its region and item name");
            }
        }
    }
}
