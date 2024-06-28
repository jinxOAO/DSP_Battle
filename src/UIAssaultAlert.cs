using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using HarmonyLib;

namespace DSP_Battle
{
    public class UIAssaultAlert
    {
        // UIDarkfogMonitor OrganizeTargetList可能是用于排序黑雾威胁程度用来显示的
        // color
        // frame 和 frameTint 是主条的框色，可以理解为frame为框色的底层色，frameTint从左往右在上加盖颜色，且从左到右覆盖率逐渐降低，逐渐过渡为很偏frame本身的颜色
        // icon 和 iconTint 为左侧小图标主题色，对于地面黑雾基地的图标
        // threatBar 紫色候选 0.689 0.34 1 0.585 、 0.773 0.330 0.836 0.305
        // 一个候选的对应frame 和 frameTint的颜色 frame 0.601 0.018 0.018 0.809   frameTint 0.321 0.088 0.82 0.609



        [HarmonyPrefix]
        [HarmonyPatch(typeof(UIDarkFogMonitor), "UrgentValue")]
        public static bool UrgentValuePostfix(object obj, ref float __result)
        {
            __result = -1f;
            DFGBaseComponent dfgbaseComponent = obj as DFGBaseComponent;
            EnemyDFHiveSystem enemyDFHiveSystem = obj as EnemyDFHiveSystem;
            if (dfgbaseComponent != null && dfgbaseComponent.id != 0)
            { 
                return true;
            }
            else
            {
                int byAstroIndex = enemyDFHiveSystem.hiveAstroId - 1000000;
                if (byAstroIndex >= 0 && byAstroIndex < AssaultController.alertHives.Length)
                {
                    if (AssaultController.alertHives[byAstroIndex] >= 0)
                    {
                        int hiveListIndex = AssaultController.alertHives[byAstroIndex];
                        if (hiveListIndex >= 0 && hiveListIndex < AssaultController.assaultHives.Count)
                        {
                            __result = 100 + Math.Max(AssaultController.assaultHives[hiveListIndex].totalTime, 0);
                            return false;
                        }
                    }
                }
                else if (byAstroIndex >= 0 && byAstroIndex >= AssaultController.alertHives.Length && byAstroIndex < GameMain.spaceSector.maxHiveCount)
                {
                    DspBattlePlugin.logger.LogWarning("Sbnormal byAstroIndex in UIAssaultAlert.UrgentValuePostfix");
                }
            }
            return true;
        }

        [HarmonyPostfix]
        [HarmonyPatch(typeof(UIDarkFogMonitorEntry), "RefreshValuesComplete")]
        public static void DFMonitorEntryRefreshPostPatch(ref UIDarkFogMonitorEntry __instance)
        {
            if(__instance.targetHive != null)
            {
                int byAstroIndex = __instance.targetHive.hiveAstroId - 1000000;
                if(byAstroIndex >= 0 && byAstroIndex < GameMain.spaceSector.maxHiveCount && AssaultController.alertHives[byAstroIndex] >= 0)
                {
                    int listIndex = AssaultController.alertHives[byAstroIndex];
                    if (listIndex >= 0 && listIndex < AssaultController.assaultHives.Count)
                    {
                        AssaultHive ah = AssaultController.assaultHives[listIndex];
                        __instance.expBar.fillAmount = 0;
                        EAssaultHiveState state = ah.state;
                    }
                }
            }
        }




        [HarmonyPostfix]
        [HarmonyPatch(typeof(UIDarkFogMonitor), "OrganizeTargetList")]
        public static void OrganizeTargetListPostfix(ref UIDarkFogMonitor __instance)
        {
            
        }

        //[HarmonyPrefix]
        //[HarmonyPatch(typeof(UIDarkFogMonitorEntry), "SetColorTheme0")]
        //public static bool SetColor0Test(ref UIDarkFogMonitorEntry __instance, bool showThreatBar)
        //{
        //    __instance.threatBar.gameObject.SetActive(showThreatBar);
        //    __instance.threatBarFull.gameObject.SetActive(false);
        //    __instance.iconFrameImage.gameObject.SetActive(false);
        //    return false;
        //    if (__instance.bgImage.color != __instance.monitor.bgColor0)
        //    {
        //        __instance.bgImage.color = new UnityEngine.Color(0.8f,0.8f,0f);
        //        __instance.frameImage.color = new UnityEngine.Color(0.8f, 0f, 0f);
        //        __instance.frameTintImage.color = new UnityEngine.Color(0.0f, 0f, 0.9f);
        //        __instance.threatBar.color = new UnityEngine.Color(0.8f, 0.0f, 0.4f);
        //        __instance.iconImage.color = new UnityEngine.Color(0f, 0f, 0f);
        //        __instance.iconTintImage.color = __instance.monitor.icontColor0;
        //    }
        //    return false;
        //}

        //[HarmonyPrefix]
        //[HarmonyPatch(typeof(UIDarkFogMonitorEntry), "SetColorTheme1")]
        //public static bool SetColor1Test(ref UIDarkFogMonitorEntry __instance)
        //{
        //    __instance.threatBar.gameObject.SetActive(true);
        //    __instance.threatBarFull.gameObject.SetActive(false);
        //    __instance.iconFrameImage.gameObject.SetActive(true);
        //    if (__instance.bgImage.color != __instance.monitor.bgColor0)
        //    {
        //        __instance.bgImage.color = __instance.monitor.bgColor0;
        //        __instance.frameImage.color = __instance.monitor.frameColor0;
        //        __instance.frameTintImage.color = __instance.monitor.frametColor0;
        //        __instance.threatBar.color = __instance.monitor.tbarColor0;
        //        __instance.iconImage.color = __instance.monitor.iconColor0;
        //        __instance.iconTintImage.color = __instance.monitor.icontColor0;
        //    }
        //    return false;
        //}

        //[HarmonyPrefix]
        //[HarmonyPatch(typeof(UIDarkFogMonitorEntry), "SetColorTheme2")]
        //public static bool SetColor2Test(ref UIDarkFogMonitorEntry __instance)
        //{
        //    __instance.threatBar.gameObject.SetActive(true);
        //    __instance.threatBarFull.gameObject.SetActive(false);
        //    __instance.iconFrameImage.gameObject.SetActive(true);
        //    if (__instance.bgImage.color != __instance.monitor.bgColor0)
        //    {
        //        __instance.bgImage.color = __instance.monitor.bgColor0;
        //        __instance.frameImage.color = __instance.monitor.frameColor0;
        //        __instance.frameTintImage.color = __instance.monitor.frametColor0;
        //        __instance.threatBar.color = __instance.monitor.tbarColor0;
        //        __instance.iconImage.color = __instance.monitor.iconColor0;
        //        __instance.iconTintImage.color = __instance.monitor.icontColor0;
        //    }
        //    return false;
        //}
    }
}
