using BepInEx;
using BepInEx.Configuration;
using UnityEngine;

namespace Lilly_s_Vore_Mod
{
    [BepInPlugin("d3acf21a-a810-4be1-898a-4bb338e62b9f", "Lilly's Vore Mod", "1.0.1")]
    internal class Bepin : BaseUnityPlugin
    {
        private ConfigEntry<bool> autoAccept;
        private ConfigEntry<bool> VoreLock;

        VoreCore vorecore;
        public void Awake()
        {
            autoAccept = Config.Bind("General", "AutoAccept", false,  "Auto Accept Vore Requests");
            VoreLock = Config.Bind("General", "VoreLock", false,  "Toggles Vore Lock");

            if (VoreCore.VoreInstance != null)
                return;

            GameObject g = GameObject.Instantiate(GameObject.CreatePrimitive(PrimitiveType.Cube));
            g.hideFlags = UnityEngine.HideFlags.HideAndDontSave;
            vorecore = g.AddComponent<VoreCore>();
            vorecore.Logger = logger;
            vorecore.saveConfig = saveSettings;
            var harmony = new HarmonyLib.Harmony("Lilly's Vore Mod");
            harmony.PatchAll();

            vorecore.autoAccept = autoAccept.Value;
            vorecore.VoreLock = VoreLock.Value;
        }
        public bool saveSettings(string _)
        {
            try
            {
                autoAccept.Value = vorecore.autoAccept;
                VoreLock.Value = vorecore.VoreLock;

                Config.Save();
                return true;
            }
            catch (Exception e)
            {
                logger(e.ToString());
                return false;
            }
        }

        public bool logger(string mesg)
        {
            Logger.LogInfo($"{mesg}");
            return true;
        }
    }
}
