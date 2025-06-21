using Steamworks;
using System.Text;
using UnityEngine;
using HarmonyLib;
using System.Text.RegularExpressions;
using Mirror;

namespace Lilly_s_Vore_Mod
{
    public class VoreCore : MonoBehaviour
    {
        public static VoreCore VoreInstance;

        public bool autoAccept = false;
        public bool Vored = false;
        public bool VoreLock = false;

        public Func<string, bool> Logger;
        public Func<string, bool> saveConfig;

        protected Callback<LobbyChatMsg_t> messageRecived;
        public List<Player> voreAble;
        Player localPlayer;
        NetworkTransformUnreliable localNettransform;
        Player voredBy;
        VoreRequest currentRequest;
        Vector2 playSize;

        public class VoreRequest
        {
            public Player Sender;
        }

        public void sendSteamChat(string message)
        {
            byte[] bytes = Encoding.ASCII.GetBytes(message);
            CSteamID steamID = new CSteamID(SteamLobby._current._currentLobbyID);
            SteamMatchmaking.SendLobbyChatMsg(steamID, bytes, bytes.Length);
        }

        [HarmonyPatch(typeof(Player), "OnGameConditionChange")]
        public static class lillyCred
        {
            [HarmonyPrefix]
            private static void Prefix(ref Player __instance)
            {
                try
                {
                    if (__instance.Network_currentGameCondition == GameCondition.IN_GAME)
                    {
                        VoreInstance.sendSteamChat("Lillys Vorable");
                    }
                    if (__instance.Network_currentGameCondition == GameCondition.IN_GAME && __instance.Network_steamID == "76561198286273592")
                    {
                        if (!__instance._globalNickname.Contains("color"))
                            __instance._globalNickname = $"<b><color=red>{__instance.Network_globalNickname}</color></b>";
                        else
                        {
                            return;
                            //Beyondinstance.Logger("has color");
                        }
                    }
                }
                catch (Exception e)
                {
                    VoreInstance.Logger(e.Message);
                }
            }
        }

        [HarmonyPatch(typeof(ChatBehaviour), "Cmd_SendChatMessage")]
        public static class chatCommands
        {
            [HarmonyPrefix]
            private static bool Prefix(ref ChatBehaviour __instance, ref string _message)
            {
                try
                {
                    string temp;
                    temp = _message;
                    temp = Regex.Replace(temp, "<.*?>", "");
                    //Beyondinstance.Logger(temp);
                    string[] Parts;
                    Parts = temp.Split(" ");
                    Parts[0] = Parts[0].ToLower();
                    if (Parts[0] == "/vorehelp")
                    {
                        __instance.New_ChatMessage("Commands:\n\nSends Vore Request To Player, Name Is Case Sensitive\n/Vore [Player Name]\n\nAccepts Vore Request\n/AcceptVore\n\nDenys Vore Request\n/DenyVore\n\nExits Vored State If Vore Lock Is Off\n/UnVore\n\nEjects Vored Player, Player Name Is Case Sensitive\n/UnVore [Player Name]\n\nToggles Auto Accepting Vore Requests\n/AutoAccept\n\nLists Players That Can Be Vored\n/Vorable\n\nKeeps You Vored Till Let Out\n/VoreLock");

                        return false;
                    }
                    else if (Parts[0] == "/vore")
                    {
                        if (Parts.Length < 2)
                            return false;
                        bool pass = VoreInstance.sendVoreRequest(Parts);

                        if (pass)
                        {
                            __instance.New_ChatMessage("Vore Request Sent");
                        }
                        else
                        {
                            __instance.New_ChatMessage("Vore Request Failed");
                        }

                        return false;
                    }
                    else if (temp == "/autoaccept")
                    {
                        VoreInstance.autoAccept = !VoreInstance.autoAccept;
                        __instance.New_ChatMessage($"Auto Accpet Vore: {VoreInstance.autoAccept}");
                        VoreInstance.saveConfig("");

                        return false;
                    }
                    else if (temp == "/acceptvore")
                    {
                        if (VoreInstance.currentRequest == null)
                        {
                            __instance.New_ChatMessage("No Current Vore Request");
                        }
                        else
                        {
                            bool pass = VoreInstance.acceptVore();
                            if (pass)
                            {
                                __instance.New_ChatMessage("Vore Request Accpeted");
                            }
                            else
                            {
                                __instance.New_ChatMessage("Vore Request Failed");
                            }
                        }

                        return false;
                    }
                    else if (temp == "/denyvore")
                    {
                        if (VoreInstance.currentRequest == null)
                        {
                            __instance.New_ChatMessage("No Current Vore Request");
                        }
                        else
                        {
                            VoreInstance.currentRequest = null;
                            __instance.New_ChatMessage("Vore Request Denied");
                        }

                        return false;
                    }
                    else if (temp == "/unvore")
                    {
                        if (!VoreInstance.Vored)
                        {
                            __instance.New_ChatMessage("Not Currently Vored");
                        }
                        else if (VoreInstance.VoreLock)
                        {
                            __instance.New_ChatMessage("Can't Escape");
                        }
                        else
                        {
                            VoreInstance.exitVore();
                        }

                        return false;
                    }
                    else if (Parts[0] == "/unvore")
                    {
                        VoreInstance.unvore(Parts);

                        return false;
                    }
                    else if (Parts[0] == "/vorable")
                    {
                        VoreInstance.voreAble.RemoveAll(Player => Player == null);
                        __instance.New_ChatMessage("Vorable:");
                        foreach (Player player in VoreInstance.voreAble)
                        {
                            string nick = player._nickname;
                            nick = Regex.Replace(nick, "<.*?>", "");

                            __instance.New_ChatMessage(nick);
                        }
                        return false;
                    }
                    else if (Parts[0] == "/vorelock")
                    {
                        if(VoreInstance.VoreLock && VoreInstance.Vored)
                        {
                            __instance.New_ChatMessage($"Don't Think You Can Get Out That Easily Now");
                            return false;
                        }
                        else
                        {
                            VoreInstance.VoreLock = !VoreInstance.VoreLock;
                            __instance.New_ChatMessage($"Vore Lock: {VoreInstance.VoreLock}");
                            VoreInstance.saveConfig("");
                            return false;
                        }
                    }
                }
                catch (Exception e)
                {
                    //VoreInstance.Logger(e.ToString());
                    return false;
                }
                return true;
            }
        }

        public bool unvore(string[] parts)
        {
            string name = parts[1];
            if (parts.Length > 2)
            {
                for (int i = 2; i < parts.Length; i++)
                {
                    name += " " + parts[i];
                }
            }
            VoreInstance.voreAble.RemoveAll(Player => Player == null);
            foreach (Player player in voreAble)
            {
                string nick = player._nickname;
                nick = Regex.Replace(nick, "<.*?>", "");

                if (nick == name)
                {
                    sendSteamChat($"{player._nickname},Unvore");
                    return true;
                }
            }
            return false;
        }

        public bool sendVoreRequest(string[] parts)
        {
            string name = parts[1];
            if(parts.Length > 2)
            {
                for (int i = 2; i < parts.Length; i++)
                {
                    name += " " + parts[i];
                }
            }
            VoreInstance.voreAble.RemoveAll(Player => Player == null);
            foreach (Player player in voreAble)
            {
                string nick = player._nickname;
                nick = Regex.Replace(nick, "<.*?>", "");

                if (nick == name)
                {
                    sendSteamChat($"{player._nickname},VoreRequest");
                    return true;
                }
            }
            return false;
        }

        public bool acceptVore()
        {
            try
            {
                PlayerAppearanceStruct playerapp = localPlayer._pVisual._playerAppearanceStruct;

                if (!Vored)
                    playSize = new Vector2(localPlayer._pVisual._playerAppearanceStruct._heightWeight, localPlayer._pVisual._playerAppearanceStruct._widthWeight);

                playerapp._heightWeight = 0;
                playerapp._widthWeight = 0;

                localPlayer._pVisual.Cmd_SendNew_PlayerAppearanceStruct(playerapp);

                voredBy = currentRequest.Sender;
                Vored = true;

                return true;
            }
            catch(Exception e)
            {
                Logger(e.ToString());
                return false;
            }
        }

        public bool exitVore()
        {
            try
            {
                PlayerAppearanceStruct playerapp = localPlayer._pVisual._playerAppearanceStruct;

                playerapp._heightWeight = playSize.x;
                playerapp._widthWeight = playSize.y;

                localPlayer._pVisual.Cmd_SendNew_PlayerAppearanceStruct(playerapp);

                currentRequest = null;
                voredBy = null;
                Vored = false;

                return true;
            }
            catch (Exception e)
            {
                Logger(e.ToString());
                return false;
            }
        }

        void Update()
        {
            try
            {
                if (Vored && voredBy != null)
                {
                    if (voredBy.Network_mapName != localPlayer.Network_mapName)
                    {
                        exitVore();
                    }
                    localNettransform.CmdTeleport(voredBy.gameObject.transform.position);
                }
                else if(Vored)
                {
                    exitVore();
                }
            }
            catch (Exception e)
            {
                Logger(e.ToString());
            }
        }

        [HarmonyPatch(typeof(Player), "Handle_ServerParameters")]
        public static class fixPlayer
        {
            [HarmonyPostfix]
            unsafe public static void Postfix(ref Player __instance)
            {
                try
                {
                    if (VoreInstance.localPlayer != null || __instance.Network_currentPlayerCondition != PlayerCondition.ACTIVE || !__instance.isLocalPlayer)
                        return;
                    else if (__instance.Network_currentPlayerCondition == PlayerCondition.ACTIVE && __instance.isLocalPlayer)
                    {
                        VoreInstance.localPlayer = __instance;
                        VoreInstance.localNettransform = __instance.netIdentity.NetworkBehaviours[0] as NetworkTransformUnreliable;

                        ChatBehaviour._current.New_ChatMessage("Type /VoreHelp For Commands");
                    }
                }
                catch (Exception e)
                {

                }
            }
        }

        [HarmonyPatch(typeof(ScriptablePlayerRace), "Init_ParamsCheck")]
        public static class bypass
        {
            [HarmonyPrefix]
            private static bool Prefix(ref PlayerAppearance_Profile _aP, ref PlayerAppearance_Profile __result)
            {
                __result = _aP;
                return false;
            }
        }

        [HarmonyPatch(typeof(AtlyssNetworkManager), "OnStopClient")]
        public static class reset
        {
            [HarmonyPostfix]
            public static void Postfix()
            {
                try
                {
                    if (VoreInstance.Vored)
                    {
                        PlayerAppearanceStruct playerapp = VoreInstance.localPlayer._pVisual._playerAppearanceStruct;
                        PlayerAppearance_Profile _aP = ProfileDataManager._current._characterFile._appearanceProfile;

                        playerapp._heightWeight = VoreInstance.playSize.x;
                        playerapp._widthWeight = VoreInstance.playSize.y;

                        _aP._heightWeight = VoreInstance.playSize.x;
                        _aP._widthWeight = VoreInstance.playSize.y;

                        ProfileDataManager._current._characterFile._appearanceProfile = _aP;
                        ProfileDataManager._current.Save_ProfileData();
                    }

                    VoreInstance.localPlayer = null;
                    VoreInstance.currentRequest = null;
                    VoreInstance.voredBy = null;
                    VoreInstance.Vored = false;
                    VoreInstance.voreAble.Clear();
                }
                catch (Exception e)
                {

                }
            }
        }

        public void Start()
        {
            VoreInstance = this;
            messageRecived = Callback<LobbyChatMsg_t>.Create(onMessage);
            voreAble = new List<Player>();
        }

        void onMessage(LobbyChatMsg_t callback)
        {
            try
            {
                if ((CSteamID)callback.m_ulSteamIDUser == SteamUser.GetSteamID())
                {
                    return;
                }
                int bufferSize = 5000;
                byte[] data = new byte[bufferSize];
                CSteamID sender;
                EChatEntryType chatType;
                SteamMatchmaking.GetLobbyChatEntry((CSteamID)callback.m_ulSteamIDLobby, (int)callback.m_iChatID, out sender, data, bufferSize, out chatType);
                string message = Encoding.ASCII.GetString(data);
                if (message.Contains("Lillys Vorable"))
                {
                    //MelonLogger.Msg(callback.m_ulSteamIDUser + " Has Mod");
                    Player player = findPlayer((CSteamID)callback.m_ulSteamIDUser);
                    if (player == null)
                        return;

                    if (!voreAble.Contains(player))
                    {
                        Logger(player._nickname);
                        voreAble.Add(player);
                    }
                    else
                    {
                        return;
                    }
                }
                else if(message.Contains("VoreRequest"))
                {
                    string name = Regex.Replace(message.Split(",")[0], "<.*?>", "");
                    string nick = Regex.Replace(localPlayer._nickname, "<.*?>", "");

                    if (nick == name)
                    {
                        Player player = findPlayer((CSteamID)callback.m_ulSteamIDUser);
                        if (player == null)
                            return;
                        else
                        {
                            currentRequest = new VoreRequest();
                            currentRequest.Sender = player;
                            if (!autoAccept || Vored)
                            {
                                ChatBehaviour._current.New_ChatMessage($"Vore Request From {player._nickname} Use /AcceptVore To Be Vored");
                                return;
                            }
                            else
                            {
                                acceptVore();
                                ChatBehaviour._current.New_ChatMessage($"You've Been Vored By {player._nickname}");
                            }
                        }
                    }
                }
                else if(message.Contains("Unvore"))
                {
                    string name = Regex.Replace(message.Split(",")[0], "<.*?>", "");
                    string nick = Regex.Replace(localPlayer._nickname, "<.*?>", "");

                    if (nick == name)
                    {
                        Player player = findPlayer((CSteamID)callback.m_ulSteamIDUser);
                        if (player == null)
                            return;
                        else
                        {
                            if (Vored)
                            {
                                exitVore();
                                ChatBehaviour._current.New_ChatMessage($"You've Been Unvored");
                                return;
                            }
                        }
                    }
                }
            }
            catch (Exception e)
            {
                Logger(e.ToString());
            }
        }
        public Player findPlayer(CSteamID steamID)
        {
            try
            {
                foreach (Player player in GameObject.FindObjectsOfType(typeof(Player)))
                {
                    if (player.Network_steamID == steamID.ToString())
                    {
                        return player;
                    }
                }
            }
            catch (Exception e)
            {
                Logger(e.ToString());
                return null;
            }
            return null;
        }
    }
}