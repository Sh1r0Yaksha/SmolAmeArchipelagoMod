
using Archipelago.MultiClient.Net;
using Archipelago.MultiClient.Net.Enums;
using Archipelago.MultiClient.Net.Helpers;
using Archipelago.MultiClient.Net.Models;
using SmolAmeArchipelagoMod.Source;
using System;
using System.Net.Sockets;
using UnityEngine;
using static System.Collections.Specialized.BitVector32;

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
                ArchipelagoPlugin.Log.LogError(
                    "Archipelago server address is empty.");
                return false;
            }

            if (port < 1 || port > 65535)
            {
                ArchipelagoPlugin.Log.LogError(
                    $"Invalid port: {port}");
                return false;
            }

            if (string.IsNullOrWhiteSpace(user))
            {
                ArchipelagoPlugin.Log.LogError(
                    "Archipelago slot name is empty.");
                return false;
            }

            try
            {
                ArchipelagoPlugin.Log.LogInfo(
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

                ArchipelagoPlugin.Log.LogInfo(
                    $"[Archipelago] Login result: {result.GetType().Name}");

                if (!result.Successful)
                {
                    LoginFailure failure = (LoginFailure)result;

                    ArchipelagoPlugin.Log.LogError(
                        $"Failed to connect to {server}:{port} " +
                        $"as '{user}'.");

                    foreach (string error in failure.Errors)
                        ArchipelagoPlugin.Log.LogError(error);

                    foreach (ConnectionRefusedError error
                             in failure.ErrorCodes)
                        ArchipelagoPlugin.Log.LogError(error.ToString());

                    SmolAmeData.Session = null;
                    return false;
                }

                ArchipelagoPlugin.Log.LogInfo(
                    $"Successfully connected to {server}:{port} " +
                    $"as '{user}'.");
                SmolAmeData.Result = (LoginSuccessful)result;

                return true;
            }
            catch (Exception e)
            {
                ArchipelagoPlugin.Log.LogError(
                    $"Archipelago connection exception:\n{e}");

                SmolAmeData.Session = null;
                SmolAmeData.Result = null;

                return false;
            }
        }


        private static void OnSocketOpened()
        {
            ArchipelagoPlugin.Log.LogInfo(
                        "[Archipelago] WebSocket OPENED");
        }

        private static void OnSocketClosed(string reason)
        {
            ArchipelagoPlugin.Log.LogWarning(
                        $"[Archipelago] WebSocket CLOSED because {reason}");
        }

        private static void OnErrorReceived(Exception exception, string message)
        {
            ArchipelagoPlugin.Log.LogWarning(
                        $"[Archipelago] Socket error: {exception} Message:{message}");
        }

        private static void OnPacketReceived(ArchipelagoPacketBase packet)
        {
            ArchipelagoPlugin.Log.LogWarning(
                         $"[Archipelago] Packet received: {packet.GetType().Name}");
        }

        public static void OnConnectionInvoked()
        {
            ArchipelagoPlugin.Log.LogInfo("[Archipelago Client] Connection Invoked");
            SmolAmeData.Server = PauseScreenPatch.Server.text;

            if (!int.TryParse(PauseScreenPatch.Port.text, out SmolAmeData.Port))
            {
                ArchipelagoPlugin.Log.LogError("[Archipelago Client] Cannot connect to server, Port should be only numbers");
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

