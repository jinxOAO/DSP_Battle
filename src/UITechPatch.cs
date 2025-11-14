using HarmonyLib;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace DSP_Battle
{
    public class UITechPatch
    {

        [HarmonyPostfix]
        [HarmonyPatch(typeof(UITechNode), "OnStartButtonClick")]
        public static void OnTechNodeStartButtonClickPostfix(ref UITechNode __instance)
        {
            int techId = __instance.techProto.ID;
            if(techId >= BattleProtos.UpgradeTechBegin && techId <= BattleProtos.UpgradeTechBegin + 7)
            {
                UIMessageBox.Show("警告红色gm".Translate(), "研究元驱动挂载点位时警告".Translate(), "明白gm".Translate(), 0);
            }
        }
    }
}
