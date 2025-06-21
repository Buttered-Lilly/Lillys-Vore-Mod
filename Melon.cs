using MelonLoader;
using UnityEngine;

[assembly: MelonInfo(typeof(Lilly_s_Vore_Mod.MelonLoad), "Lilly's Vore Mod", "1.0.0", "ButteredLilly", null)]
[assembly: MelonGame("KisSoft", "ATLYSS")]
[assembly: MelonOptionalDependencies("BepInEx")]

namespace Lilly_s_Vore_Mod
{
    internal class MelonLoad : MelonMod
    {
        private MelonPreferences_Category general;
        private MelonPreferences_Entry<bool> autoAccept;

        VoreCore vorecore;

        public override void OnInitializeMelon()
        {
            general = MelonPreferences.CreateCategory("General");
            autoAccept = general.CreateEntry<bool>("AutoAccept", false, "AutoAccept", "Auto Accept Vore Requests");
            if (VoreCore.VoreInstance != null)
                return;

            GameObject g = GameObject.Instantiate(GameObject.CreatePrimitive(PrimitiveType.Cube));
            g.name = "VoreCore";
            g.hideFlags = UnityEngine.HideFlags.HideAndDontSave;
            vorecore = g.AddComponent<VoreCore>();
            vorecore.Logger = logger;
            vorecore.saveConfig = saveSettings;

            vorecore.autoAccept = autoAccept.Value;
        }

        public bool saveSettings(string _)
        {
            try
            {
                autoAccept.Value = vorecore.autoAccept;

                MelonPreferences.Save();
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
            MelonLogger.Msg(mesg);
            return true;
        }
    }
}
