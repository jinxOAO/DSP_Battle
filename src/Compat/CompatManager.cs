using BepInEx;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace DSP_Battle.src.Compat
{
    public static class CompatManager
    {
        public const string UniverseGenTweak_GUID = "org.soardev.universegentweaks";
        public const string GB_GUID = "org.LoShin.GenesisBook";

        public static bool UniverseGenTweak = false;
        public static bool GB = false;

        public static void Init()
        {
            if (BepInEx.Bootstrap.Chainloader.PluginInfos.ContainsKey(GB_GUID))
                GB = true;
            if (BepInEx.Bootstrap.Chainloader.PluginInfos.ContainsKey(UniverseGenTweak_GUID))
                UniverseGenTweak = true;
        }
    }

    ////"org.soardev.universegentweaks"
    //[BepInPlugin("com.ckcz123.TCFVUGTCompat", "TCFVUGTCompat", "0.1.0")]
    //[BepInDependency("org.soardev.universegentweaks")]
    //public class TCFV_UGTCompat : BaseUnityPlugin
    //{
    //    void Awake()
    //    {
    //        CompatManager.UniverseGenTweak = true;
    //    }
    //}
}
