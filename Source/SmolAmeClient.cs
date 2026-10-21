
using Archipelago.MultiClient.Net;
using Archipelago.MultiClient.Net.Enums;
using System;
using System.Net.Sockets;
using static System.Collections.Specialized.BitVector32;

namespace SmolAmeArchipelago
{
    internal static class SmolAmeClient
    {
        public static ArchipelagoSession Session;
        public static LoginResult result;

        public static bool Connect(
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

                Session = ArchipelagoSessionFactory
                    .CreateSession(server, port);

                Session.Socket.SocketOpened += OnSocketOpened;
                Session.Socket.SocketClosed += OnSocketClosed;
                Session.Socket.ErrorReceived += OnErrorReceived;
                Session.Socket.PacketReceived += OnPacketReceived;

                result = Session.TryConnectAndLogin(
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

                    Session = null;
                    return false;
                }

                ArchipelagoPlugin.Log.LogInfo(
                    $"Successfully connected to {server}:{port} " +
                    $"as '{user}'.");

                return true;
            }
            catch (Exception e)
            {
                ArchipelagoPlugin.Log.LogError(
                    $"Archipelago connection exception:\n{e}");

                Session = null;
                result = new LoginFailure(
                    e.GetBaseException().Message);

                return false;
            }
        }

        public static void ReceiveItemsList()
        {
            
            
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

    }
}
