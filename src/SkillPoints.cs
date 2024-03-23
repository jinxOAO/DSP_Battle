using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace DSP_Battle
{
    public class SkillPoints
    {
        // 需要存档的数据
        public static int totalPoints = 0;
        public static int[] skillLevelL = new int[30]; // 左列已分配的点数
        public static int[] skillLevelR = new int[30];

        // 参数
        public static int skillCountL = 10;
        public static int skillCountR = 10;
        public static List<int> spMinByRank = new List<int> { 0, 1, 2, 4, 6, 10, 14, 20, 26, 34, 44, 44, 44 }; // 满级前，sp不会低于
        public static int spGainFullLevel = 5; // 满级后每次升级给的点数
        public static List<int> skillMaxLevelL = new List<int> { 200, 200, 200, 200, 200, 200, 200, 200, 200, 200, 200, 200, 200, 200 };
        public static List<int> skillMaxLevelR = new List<int> { 200, 200, 200, 200, 200, 200, 200, 200, 200, 200, 200, 200, 200, 200 };
        public static List<float> LSkillValues = new List<float> { 0.1f, 0.1f, 0.1f, 0.1f, 0.1f, 0.1f, 0.1f, 0.1f, 0.1f, 0.1f, 0.1f, 0.1f };
        public static List<float> RSkillValues = new List<float> { 0.1f, 0.1f, 0.1f, 0.1f, 0.1f, 0.1f, 0.1f, 0.1f, 0.1f, 0.1f, 0.1f, 0.1f };
        public static List<string> LSkillSuffixes = new List<string> { "%", "%", "%", "%", "%", "%", "%", "%", "%", "%", "%" }; // 文本后缀
        public static List<string> RSkillSuffixes = new List<string> { "-", "-", "-", "-", "-", "-", "%", "%", "%", "%", "%" };

        // 0.94 ** (tech[3606].curLevel-1) 是游戏默认的采矿消耗率，如果3601-3605均已解锁，否则，按照解锁的最后一个计算

        public static void InitBeforeLoad()
        {
            UISkillPointsWindow.InitAll();
            totalPoints = 0;
            skillLevelL = new int[skillCountL];
            for (int i = 0; i < skillLevelL.Length; i++)
            {
                skillLevelL[i] = 0;
            }
            skillLevelR = new int[skillCountR];
            for (int i = 0; i < skillLevelR.Length; i++)
            {
                skillLevelR[i] = 0;
            }
            UISkillPointsWindow.ClearTempLevelAdded();
        }

        public static void InitAfterLoad()
        {
            SkillPoints.totalPoints = Math.Max(SkillPoints.totalPoints, SkillPoints.spMinByRank[Rank.rank]);
        }

        public static int UnusedPoints()
        {
            return totalPoints - skillLevelL.Sum() - skillLevelR.Sum();
        }

        public static void Export(BinaryWriter w)
        {
            w.Write(totalPoints);
            w.Write(skillLevelL.Length);
            for (int i = 0; i < skillLevelL.Length; i++)
            {
                w.Write(skillLevelL[i]);
            }
            w.Write(skillLevelR.Length);
            for (int i = 0; i < skillLevelR.Length; i++)
            {
                w.Write(skillLevelR[i]);
            }
        }

        public static void Import(BinaryReader r)
        {
            InitBeforeLoad();
            if (Configs.versionWhenImporting >= 30240320)
            {
                totalPoints = r.ReadInt32();
                int countL = r.ReadInt32();
                skillLevelL = new int[skillCountL];
                for (int i = 0; i < countL; i++)
                {
                    if (i < skillCountL)
                        skillLevelL[i] = r.ReadInt32();
                    else
                        _ = r.ReadInt32();
                }
                int countR = r.ReadInt32();
                skillLevelR = new int[skillCountR];
                for (int i = 0; i < countR; i++)
                {
                    if(i < skillCountR)
                        skillLevelR[i] = r.ReadInt32();
                    else
                        _ = r.ReadInt32();
                }
            }
            InitAfterLoad();
        }

        public static void IntoOtherSave()
        {
            InitBeforeLoad();
        }
    }
}
