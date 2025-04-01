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
        public static bool UniverseGenTweak = false;
    }

    //"org.soardev.universegentweaks"
    [BepInPlugin("com.ckcz123.TCFVUGTCompat", "TCFVUGTCompat", "0.1.0")]
    [BepInDependency("org.soardev.universegentweaks")]
    public class TCFV_UGTCompat : BaseUnityPlugin
    {
        void Awake()
        {
            CompatManager.UniverseGenTweak = true;
        }
    }
}
