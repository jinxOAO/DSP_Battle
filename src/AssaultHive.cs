using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace DSP_Battle
{
    public class AssaultHive
    {
        public int oriAstroId;
        public int listIndex; // index of AssaultController.assaultHives;
        public int starIndex; 
        public int orbitIndex; // 0-7, hive's (orbit) index in star system
        public int byAstroIndex; // spaceSector.dfHivesByAstro[] index
        public int strength; // strength

        public EAssaultHiveState state;
        public int time;
        public int totalTime; // totalTime before assault begins
        public int maxTime;
        public int level;
        public int oriLevel;

        public int assembleNum; // 一直制造的舰队数量上限
        public int assaultNum; // 初始入侵的舰队数量
        public int inhibitPoints; // 已通过恒星炮压制的点数
        public int inhibitPointsLimit; // 最多可以压制的点数
        public int inhibitPointsTotal; // 总强度点
        public bool canFullyStopped; // 是否可以完全阻止
        public bool isSuper; // 超级入侵

        public EnemyDFHiveSystem hive;
        public bool isCreated; // 巢穴原本是null或者没有核心，是创建出来的

        public AssaultHive(int starIndex, int orbitIndex, int listIndex)
        {
            this.oriAstroId = 1000000 + starIndex * 8 + orbitIndex + 1;
            this.starIndex = starIndex;
            this.orbitIndex = orbitIndex;
            this.listIndex = listIndex;
            byAstroIndex = oriAstroId - 1000000;
            state = EAssaultHiveState.Idle;
            time = 0;
            inhibitPoints = 0;
            isCreated = false;
            CreateOrGetHive();
        }

        public void CreateOrGetHive()
        {
            hive = GameMain.data.spaceSector.dfHivesByAstro[byAstroIndex];
            if(hive == null)
            {
                int count = 8;
                while (hive == null && count > 0)
                {
                    GameMain.spaceSector.TryCreateNewHive(GameMain.galaxy.StarById(starIndex + 1));
                    hive = GameMain.data.spaceSector.dfHivesByAstro[byAstroIndex];
                    count--;
                }
            }
            if (hive != null)
            {
                if (!hive.hasCore())
                {
                    isCreated = true;
                    hive.BuildCore();
                }
            }
            oriLevel = hive.evolve.level;
        }

        public void LogicTick()
        {
            if (hive == null)
                return;
            switch (state)
            {
                case EAssaultHiveState.Idle:
                    LogicTickIdle();
                    break;
                case EAssaultHiveState.Expand:
                    LogicTickExpand();
                    break;
                case EAssaultHiveState.Assemble:
                    LogicTickAssemble();
                    break;
                case EAssaultHiveState.Assault:
                    LogicTickAssault();
                    break;
                case EAssaultHiveState.End:
                    LogicTickEnd();
                    break;
                case EAssaultHiveState.Remove:
                    LogicTickRemove();
                    break;
                default:
                    break;
            }

            // Update invincible and modifier array
            // if not removing or assaulting state, add me to the invincibleHives array
            if (state == EAssaultHiveState.Idle || state == EAssaultHiveState.Expand || state == EAssaultHiveState.Assemble)
                AssaultController.invincibleHives[byAstroIndex] = listIndex;
            else
                AssaultController.invincibleHives[byAstroIndex] = -1;

            // if not removing state, and have modifier, add me to the modifierHives array
            if (state == EAssaultHiveState.Assault && AssaultController.modifierEnabled)
                AssaultController.modifierHives[byAstroIndex] = listIndex;
            else
                AssaultController.modifierHives[byAstroIndex] = -1;

            // if should show special alert monitor, add me to the alertHives array
            if (state == EAssaultHiveState.Assemble || state == EAssaultHiveState.Assault || state == EAssaultHiveState.Expand)
                AssaultController.alertHives[byAstroIndex] = listIndex;
            else
                AssaultController.alertHives[byAstroIndex] = -1;
        }
        public void LogicTickIdle()
        {
            totalTime--;
            time--;
            if (time <= 0)
            {
                state = EAssaultHiveState.Expand;
                time = 3600;
            }
        }

        public void LogicTickExpand()
        {
            totalTime--;
            time--;
            if (time <= 0)
            {
                state = EAssaultHiveState.Assemble;
                time = totalTime;
            }
        }
        public void LogicTickAssemble()
        {
            totalTime--;
            time--;
            if (time <= 0)
            {
                state = EAssaultHiveState.Assault;
                if (AssaultController.modifier.Sum() > 0)
                    AssaultController.modifierEnabled = true;
                time = 3600 * 5;
            }
        }
        public void LogicTickAssault()
        {
            totalTime--;
            time--;
            if (time <= 0)
            {
                state = EAssaultHiveState.End;
                time = 60;
            }
        }
        public void LogicTickEnd()
        {
            totalTime--;
            time--;
            if (time <= 0)
            {
                state = EAssaultHiveState.Remove;
            }
        }
        public void LogicTickRemove()
        {

        }
    }

    public static class AssaultHiveCreator
    {

        public static void SetStrength(this AssaultHive _this, int strength)
        {
            _this.strength = strength;
        }
    }

    public enum EAssaultHiveState
    {
        Idle = 0,
        Expand = 1,
        Assemble = 2,
        Assault = 3,
        End = 4,
        Remove = 99
    }
}
