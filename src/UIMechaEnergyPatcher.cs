using HarmonyLib;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Runtime.CompilerServices;
using System.Text;
using System.Threading.Tasks;

namespace DSP_Battle
{
    public class UIMechaEnergyPatcher
    {
        public const int droplet = 21;
        public const int guideBombing = 22;
        [HarmonyPostfix]
        [HarmonyPatch(typeof(UIMechaEnergy), "EnergyChangeString")]
        public static void EnergyChangeStringPostPatch(ref UIMechaEnergy __instance, Mecha mecha, int func, ref string __result)
        {
            if (func == 21 || func == 22)
            {
                double num = (func < 32) ? (mecha.energyChanges[func] * 60.0) : (mecha.totalEnergyChange * 60.0);
                bool flag = num < 0.0;
                long num2 = (long)(num + (flag ? -0.5 : 0.5));
                if (num2 == 0L)
                {
                    return;
                }
                double num3 = mecha.reactorPowerConsRatio - 1.0;
                string text;
                if (num3 > 0.001)
                {
                    text = string.Format(" (+{0:0.0}%)", num3 * 100.0);
                }
                else if (num3 < -0.001)
                {
                    text = string.Format(" ({0:0.0}%)", num3 * 100.0);
                }
                else
                {
                    text = "";
                }
                StringBuilder stringBuilder = (func < 32) ? __instance.textBuilderW : __instance.textBuilderW2;
                StringBuilderUtility.WriteKMG(stringBuilder, 8, num2, true);
                string text2 = stringBuilder.ToString();
                if (flag)
                {
                    switch (func)
                    {
                        case 21:
                            __result = "水滴耗能".Translate() + "<color=\"#FD965EC0\">" + text2 + "\r\n";
                            return;
                        case 22:
                            __result = "引导太阳轰炸耗能".Translate() + "<color=\"#FD965EC0\">" + text2 + "\r\n";
                            return;
                    }
                }
                else
                {
                    switch (func)
                    {
                        case 21:
                            __result = "水滴耗能".Translate() + "<color=\"#61D8FFC0\">" + text2 + "\r\n";
                            return;
                        case 22:
                            __result = "引导太阳轰炸耗能".Translate() + "<color=\"#61D8FFC0\">" + text2 + "\r\n";
                            return;
                    }
                }
            }
        }
    }
}
