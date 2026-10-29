
using Archipelago.MultiClient.Net;
using Archipelago.MultiClient.Net.Enums;
using Archipelago.MultiClient.Net.Helpers;
using Archipelago.MultiClient.Net.Models;
using SmolAmeArchipelagoMod.Source;
using System;
using System.Collections.Generic;
using UnityEngine;

namespace SmolAmeArchipelago
{
    internal class SmolAmeClient
    {
        public static bool TryConnect(
            string server,
            int port,
            string user,
            string password)
        {
            if (string.IsNullOrWhiteSpace(server))
            {
                HUD.Set(
                    "Archipelago server address is empty.");
                return false;
            }

            if (port < 1 || port > 65535)
            {
                HUD.Set(
                    $"Invalid port: {port}");
                return false;
            }

            if (string.IsNullOrWhiteSpace(user))
            {
                HUD.Set(
                    "Archipelago slot name is empty.");
                return false;
            }

            try
            {
                

                SmolAmeData.Session = ArchipelagoSessionFactory
                    .CreateSession(server, port);

                SmolAmeData.Session.Socket.SocketOpened += OnSocketOpened;
                SmolAmeData.Session.Socket.SocketClosed += OnSocketClosed;
                SmolAmeData.Session.Socket.ErrorReceived += OnErrorReceived;
                SmolAmeData.Session.Socket.PacketReceived += OnPacketReceived;

                var result = SmolAmeData.Session.TryConnectAndLogin(
                    "Smol Ame",
                    user,
                    ItemsHandlingFlags.AllItems,
                    password: password
                );

                if (!result.Successful)
                {
                    LoginFailure failure = (LoginFailure)result;

                    HUD.Set(
                        $"Failed to connect to {server}:{port} " +
                        $"as '{user}'.");

                    foreach (string error in failure.Errors)
                        HUD.Set(error);

                    foreach (ConnectionRefusedError error
                             in failure.ErrorCodes)
                        HUD.Set(error.ToString());

                    SmolAmeData.Session = null;
                    return false;
                }

                HUD.Set(
                    $"Successfully connected to {server}:{port} " +
                    $"as '{user}'.");
                PlayerPrefs.SetString("server", server);
                PlayerPrefs.SetInt("port", port);
                PlayerPrefs.SetString("slot", user);
                SmolAmeData.Result = (LoginSuccessful)result;
                LoadSlotData(SmolAmeData.Result.SlotData);

                return true;
            }
            catch (Exception e)
            {

                SmolAmeData.Session = null;
                SmolAmeData.Result = null;

                return false;
            }
        }


        private static void OnSocketOpened()
        {
        }

        private static void OnSocketClosed(string reason)
        {
            HUD.Set($"Session Disconnected. Moving to Hub and attempting to reconnect...");
            LevelLoader.loader.LoadLevel(0);
            
            TryConnect(SmolAmeData.Server, SmolAmeData.Port, SmolAmeData.Slot, SmolAmeData.Password);

        }

        private static void OnErrorReceived(Exception exception, string message)
        {   
        }

        private static void OnPacketReceived(ArchipelagoPacketBase packet)
        {
        }

        public static void OnConnectionInvoked()
        {
            SmolAmeData.Server = PauseScreenPatch.Server.text;

            if (!int.TryParse(PauseScreenPatch.Port.text, out SmolAmeData.Port))
            {
                HUD.Set("[Archipelago Client] Cannot connect to server, Port should be only numbers");
            }

            SmolAmeData.Slot = PauseScreenPatch.Slot.text;
            var Password = PauseScreenPatch.Password.text;
            try
            {
                bool tryConnect = TryConnect(
                    SmolAmeData.Server,
                    SmolAmeData.Port,
                    SmolAmeData.Slot,
                    Password);
                if (tryConnect)
                {
                    SmolAmeData.Session.Items.ItemReceived += ReceivedItem;
                }
                

                CollectReceivedItems();
                CollectCompletedLocations();
            }
            catch (Exception ex)
            {
                HUD.Set($"Could not connect: {ex.Message}");
            }
        }

        private static void ReceivedItem(ReceivedItemsHelper helper)
        {
            ItemInfo recieved = helper.DequeueItem();
            PutReceivedItemInSmolAme(recieved);
            ArchipelagoPlugin.RegionSync.OnItemReceived();
        }

        static void CollectReceivedItems()
        {
            var items = SmolAmeData.Session.Items.AllItemsReceived;

            foreach (var item in items)
            {
                PutReceivedItemInSmolAme(item);
            }
        }

        public static void CollectCompletedLocations()
        {
            var locations = SmolAmeData.Session.Locations.AllLocationsChecked;

            foreach (Int64 location in locations)
            {
                var locationName = SmolAmeData.LocationIDToCheck[(int)location];
                PutReceivedLocationInSmolAme(locationName.Item1, locationName.Item2);
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

                if (string.Equals(regionName, "General"))
                {
                    HUD.Set($"Received {itemName} from {item.Player.Name}");
                }
                else
                {
                    HUD.Set($"Received {itemName} in {regionName} from {item.Player.Name}");
                }
            }
        }

        static void PutReceivedLocationInSmolAme(string regionName, string checkName)
        {
            SmolAmeData.AreaChecksCollected[regionName][checkName] = true;
        }

        public static void LoadSlotData(Dictionary<string, object> slotData)
        {
            SmolAmeData.Mode = (GameMode)GetIntFromSlotData(slotData, "game_mode", (int)GameMode.AllStages);
            SmolAmeData.IncludeCA = GetIntFromSlotData(slotData, "include_ca", 0) != 0;
        }

        private static int GetIntFromSlotData(Dictionary<string, object> data, string key, int fallback)
        {
            if (data != null && data.TryGetValue(key, out object value) && value != null)
                return Convert.ToInt32(value);

            return fallback;
        }
    }
}

