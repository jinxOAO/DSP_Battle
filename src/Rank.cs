﻿using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using System.Threading;
using System.IO;
using UnityEngine;
using HarmonyLib;
using System.Reflection;

namespace DSP_Battle
{
    public class Rank
    {
        public static int rank = 0;
        public static int exp = 0;


        public static void InitRank()
        {
            rank = 0;
            exp = 0;
        }

        [HarmonyPostfix]
        [HarmonyPatch(typeof(GameData), "GameTick")]
        public static void RankGameTick(ref GameData __instance, long time)
        {
            //检查升级，因为AddExp有多线程调用，为了避免问题不在每次AddExp后检查升级，而是每帧检查
            if (rank < 10 && exp >= Configs.expToNextRank[rank])
            {
                Promotion();
            }
            //int inc;
            //if(GameMain.mainPlayer.package.TakeItem(8033, 1, out inc)>0)
            //{
            //    AddExp(Configs.expPerAlienMeta);
            //}
            
        }

        public static void AddExp(int num)
        {
            if (rank >= 10) return;
            int realExp = num;
            if (Relic.HaveRelic(3, 17)) // relic 3-17
                realExp = (int)(realExp * 1.25);
            Interlocked.Add(ref exp, realExp);
        }

        private static void Promotion()
        {
            Interlocked.Add(ref exp, -Configs.expToNextRank[rank]);
            rank += 1;
            if (Configs.extraSpeedEnabled) //如果正处在奖励中升级，则刷新一下新增的奖励内容，防止奖励结束时计算出错
            {
                if (rank == 3)
                {
                    GameMain.history.miningCostRate *= 0.8f;
                }
                else if (rank == 5)
                {

                }
                else if (rank == 7)
                {
                    GameMain.history.miningCostRate *= 0.625f;
                }
            }
            if (Relic.HaveRelic(2, 1)) // relic 2-1
                Interlocked.Add(ref Relic.autoConstructMegaStructureCountDown, rank * rank * rank * 60);
            UIRank.ForceRefreshAll();
            UIRank.UIPromotionNotify();
        }

        public static void DownGrade(bool clearExp = true)
        {
            if(clearExp)
                Interlocked.Exchange(ref exp, 0);
            if(rank > 0)
            {
                rank -= 1;
                UIRank.ForceRefreshAll();
            }
        }


        public static void Export(BinaryWriter w)
        {
            w.Write(rank);
            w.Write(exp);
        }

        public static void Import(BinaryReader r)
        {
            if (Configs.versionWhenImporting >= 30220420)
            {
                rank = r.ReadInt32();
                exp = r.ReadInt32();
            }
            else
            {
                InitRank();
            }
            UIRank.InitUI();
        }

        public static void IntoOtherSave()
        {
            rank = 0;
            exp = 0;
            UIRank.InitUI();
        }


        
    }
}
