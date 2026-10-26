
using Archipelago.MultiClient.Net;
using Archipelago.MultiClient.Net.Enums;
using Archipelago.MultiClient.Net.Helpers;
using Archipelago.MultiClient.Net.Models;
using SmolAmeArchipelagoMod.Source;
using System;
using System.Collections.Generic;

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
                InfoHUD.Set(
                    "Archipelago server address is empty.");
                return false;
            }

            if (port < 1 || port > 65535)
            {
                InfoHUD.Set(
                    $"Invalid port: {port}");
                return false;
            }

            if (string.IsNullOrWhiteSpace(user))
            {
                InfoHUD.Set(
                    "Archipelago slot name is empty.");
                return false;
            }

            try
            {
                InfoHUD.Set(
                    $"Connecting to {server}:{port}, slot '{user}'");

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

                InfoHUD.Set(
                    $"[Archipelago] Login result: {result.GetType().Name}");

                if (!result.Successful)
                {
                    LoginFailure failure = (LoginFailure)result;

                    InfoHUD.Set(
                        $"Failed to connect to {server}:{port} " +
                        $"as '{user}'.");

                    foreach (string error in failure.Errors)
                        InfoHUD.Set(error);

                    foreach (ConnectionRefusedError error
                             in failure.ErrorCodes)
                        InfoHUD.Set(error.ToString());

                    SmolAmeData.Session = null;
                    return false;
                }

                InfoHUD.Set(
                    $"Successfully connected to {server}:{port} " +
                    $"as '{user}'.");
                SmolAmeData.Result = (LoginSuccessful)result;
                LoadSlotData(SmolAmeData.Result.SlotData);

                return true;
            }
            catch (Exception e)
            {
                InfoHUD.Set(
                    $"Archipelago connection exception:\n{e}");

                SmolAmeData.Session = null;
                SmolAmeData.Result = null;

                return false;
            }
        }


        private static void OnSocketOpened()
        {
            InfoHUD.Set(
                        "[Archipelago] WebSocket OPENED");
        }

        private static void OnSocketClosed(string reason)
        {
            InfoHUD.Set(
                        $"[Archipelago] WebSocket CLOSED because {reason}");
        }

        private static void OnErrorReceived(Exception exception, string message)
        {   
            InfoHUD.Set(
                        $"[Archipelago] Socket error: {exception} Message:{message}");
        }

        private static void OnPacketReceived(ArchipelagoPacketBase packet)
        {
            InfoHUD.Set(
                         $"[Archipelago] Packet received: {packet.GetType().Name}");
        }

        public static void OnConnectionInvoked()
        {
            InfoHUD.Set("[Archipelago Client] Connection Invoked");
            SmolAmeData.Server = PauseScreenPatch.Server.text;

            if (!int.TryParse(PauseScreenPatch.Port.text, out SmolAmeData.Port))
            {
                InfoHUD.Set("[Archipelago Client] Cannot connect to server, Port should be only numbers");
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
                InfoHUD.Set($"[Archipelago Client] Connection result: {tryConnect}");

                CollectReceivedItems();
                CollectCompletedLocations();
            }
            catch (Exception ex)
            {
                InfoHUD.Set($"[Archipelago Client] Could not connect: {ex.Message}");
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
                var locationName = SmolAmeData.LocationIDToName[(int)location];
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
                InfoHUD.Set($"Received {item} in {regionName}");
            }
            else
            {
                InfoHUD.Set("[Archipelago Client] Item received but cannot parse its region and item name");
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
                InfoHUD.Set($"Received {location} in {regionName}");
            }
            else
            {
                InfoHUD.Set("[Archipelago Client] Item received but cannot parse its region and item name");
            }
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

