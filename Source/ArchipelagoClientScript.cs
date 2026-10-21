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
                ArchipelagoPlugin.Log.LogInfo($"[Archipelago Client] Connection result: {tryConnect}");
            }
            catch (Exception ex)
            {
                ArchipelagoPlugin.Log.LogError($"[Archipelago Client] Could not connect: {ex.Message}");
            }
        }
    }
}
