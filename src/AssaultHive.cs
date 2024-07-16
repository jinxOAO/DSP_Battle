using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using UnityEngine;

namespace DSP_Battle
{
    public class AssaultHive
    {
        public int listIndex; // index of AssaultController.assaultHives;
        public int starIndex;
        public int oriAstroId; // 1000000+byAstro，也是hiveAstro
        public int orbitIndex; // 0-7, hive's (orbit) index in star system
        public int byAstroIndex; // spaceSector.dfHivesByAstro[] index
        public int strength; // strength

        public EAssaultHiveState state;
        public int time;
        public int timeTillAssault; // totalTime before assault begins
        public int timeTotalInit; // 最初的时间计数
        public int timeDelayedByRelic; // relic 4-7 增加的时间，最大为
        public int level;
        public int oriLevel;

        public int assaultNumTotal; // 初始入侵的舰队数量
        public int inhibitPoints; // 已通过恒星炮压制的点数
        public int inhibitPointsLimit; // 最多可以压制的点数
        public int inhibitPointsTotalInit; // 总强度点
        public bool canFullyStopped; // 是否可以完全阻止
        public bool isSuper; // 超级入侵

        public int enemyKilled;

        public EnemyDFHiveSystem hive;
        public bool isCreated; // 巢穴原本是null或者没有核心，是创建出来的
        public int assembleNum { get { return Math.Min(1440, (int)(assaultNum * 1.5) + 1); } } // 一直制造的舰队数量上限

        public static int timeDelayedMax = 3600 * 20; // 最大推迟的时间


        public int assaultNum
        {
            get
            {
                if (inhibitPoints >= inhibitPointsTotalInit || inhibitPointsTotalInit <= 0)
                    return 0;
                else
                    return Math.Max(1, (int)(assaultNumTotal * (inhibitPointsTotalInit - inhibitPoints) * 1.0 / inhibitPointsTotalInit));
            }
        }

        public AssaultHive(int starIndex, int orbitIndex, int listIndex)
        {
            this.oriAstroId = 1000000 + starIndex * 8 + orbitIndex + 1;
            this.starIndex = starIndex;
            this.orbitIndex = orbitIndex;
            this.listIndex = listIndex;
            byAstroIndex = oriAstroId - 1000000;
            state = EAssaultHiveState.Idle;
            time = 0;
            timeDelayedByRelic = 0;
            inhibitPoints = 0;
            isCreated = false;
            isSuper = false;
            enemyKilled = 0;
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
            if(!hive.realized)
                hive.Realize();
            oriLevel = hive.evolve.level;
        }

        public void LogicTick()
        {
            if (hive == null)
            {
                state = EAssaultHiveState.Remove;
                time = 0;
            }
            else if(!hive.isAlive)
            {
                state = EAssaultHiveState.End;
                time = 10;
            }
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
            timeTillAssault--;
            time--;
            if (time <= 0)
            {
                state = EAssaultHiveState.Expand;
                time = 5400;
            }
        }

        public void LogicTickExpand()
        {
            timeTillAssault--;
            time--;
            QuickBuild(5);

            hive.evolve.threat = 0;
            hive.evolve.waveTicks = 0;
            hive.evolve.waveAsmTicks = 0;

            if (time <= 0)
            {
                state = EAssaultHiveState.Assemble;
                time = timeTillAssault;
                if(listIndex == 0)
                {
                    UIDialogPatch.ShowUIDialog("虚空入侵".Translate(), string.Format("侦测到虚空入侵提示".Translate(), GameMain.galaxy.StarById(starIndex + 1)?.displayName ));
                }
            }

        }
        public void LogicTickAssemble()
        {
            timeTillAssault--;
            time--;
            QuickAssemble();

            hive.evolve.threat = 0;
            hive.evolve.waveTicks = 0;
            hive.evolve.waveAsmTicks = 0;

            if (time <= 0)
            {
                LaunchAssault();
                state = EAssaultHiveState.Assault;
                if (!AssaultController.modifierEnabled && AssaultController.modifier.Sum() > 0)
                    AssaultController.modifierEnabled = true;
                time = 0;
            }
        }
        public void LogicTickAssault()
        {
            timeTillAssault--;
            time--;

            if (hive == null)
            {
                state = EAssaultHiveState.End;
                time = 10;
            }

            if(hive.hasIncomingAssaultingUnit)
            {

            }
            else
            {
                state = EAssaultHiveState.End;
                time = 2;
            }

            if (time == 1)
            {
                state = EAssaultHiveState.End;
                time = 2;
            }
            
        }
        public void LogicTickEnd()
        {
            timeTillAssault--;
            time--;
            if (hive.evolve.level > oriLevel)
            {
                hive.evolve.level--;
                hive.evolve.expf = 0;
                hive.evolve.expp = 0;
            }
            if (time <= 0)
            {
                if (hive.evolve.level > oriLevel)
                {
                    hive.evolve.level = oriLevel;
                }
                state = EAssaultHiveState.Remove;
            }
        }
        public void LogicTickRemove()
        {

        }

        void QuickBuild(int speedFactor)
        {
            if (hive == null)
                return;
            //if (GameMain.instance.timei % 4 != listIndex) // 多个巢穴，间隔进行快速建造
            //    return;

            ref AnimData[] ptr2 = ref hive.sector.enemyAnimPool;
            if (hive.realized)
            {
                int cursor2 = hive.cores.cursor;
                int cursor8 = hive.builders.cursor;
                EnemyBuilderComponent[] buffer8 = hive.builders.buffer;
                DFSCoreComponent[] buffer2 = hive.cores.buffer;
                while (speedFactor > 0)
                {
                    speedFactor--;
                    for (int i = 1; i < cursor8; i++)
                    {
                        ref EnemyBuilderComponent ptr4 = ref buffer8[i];
                        if (ptr4.id == i)
                        {
                            int enemyId = ptr4.enemyId;
                            ptr4.energy = ptr4.maxEnergy;
                            ptr4.matter = ptr4.maxMatter;
                            ptr4.LogicTick();
                            if (ptr4.state >= 3)
                            {
                                ptr4.BuildLogic_Space(hive, buffer8, hive.pbuilders);
                            }
                            if (speedFactor == 0)
                                ptr4.RefreshAnimation_Space(hive.pbuilders, ref ptr2[enemyId]);
                        }
                        else
                        {
                        }
                    }
                }
            }
        }

        void QuickAssemble()
        {
            EnemyFormation enemyFormation = hive.forms[0];
            if (enemyFormation.vacancyCount > 0 && enemyFormation.unitCount < assembleNum)
            {
                int num5 = enemyFormation.AddUnit();
                if (hive.isLocal && num5 > 0)
                {
                    hive.InitiateUnitDeferred(0, num5,  new UnityEngine.Vector3(0,0,0), new UnityEngine.Quaternion(0, 0, 0, 0), new UnityEngine.Vector3(0,0,0), 0);
                }
            }
        }

        void LaunchAssault()
        {
            if (hive != null)
            {
                hive.hatredAstros.Sort();
                ref HatredTarget ptr = ref hive.hatredAstros.max;
                bool flag2 = false;
                int targetAstroId = 0;
                Vector3 tarPos = Vector3.zero;
                Vector3 maxHatredPos = Vector3.zero;
                for (int i = 0; i < 8; i++)
                {
                    switch (i)
                    {
                        case 0:
                            ptr = ref hive.hatredAstros.max;
                            break;
                        case 1:
                            ptr = ref hive.hatredAstros.h1;
                            break;
                        case 2:
                            ptr = ref hive.hatredAstros.h2;
                            break;
                        case 3:
                            ptr = ref hive.hatredAstros.h3;
                            break;
                        case 4:
                            ptr = ref hive.hatredAstros.h4;
                            break;
                        case 5:
                            ptr = ref hive.hatredAstros.h5;
                            break;
                        case 6:
                            ptr = ref hive.hatredAstros.h6;
                            break;
                        case 7:
                            ptr = ref hive.hatredAstros.min;
                            break;
                    }
                    if (!ptr.isNull)
                    {
                        int objectId = ptr.objectId;
                        PlanetData planetData = hive.sector.galaxy.PlanetById(objectId);
                        if (planetData != null && planetData.type != EPlanetType.Gas)
                        {
                            PlanetFactory factory = planetData.factory;
                            if (factory != null)
                            {
                                PowerSystem powerSystem = factory.powerSystem;
                                int consumerCursor = powerSystem.consumerCursor;
                                int nodeCursor = powerSystem.nodeCursor;
                                PowerConsumerComponent[] consumerPool = powerSystem.consumerPool;
                                PowerNodeComponent[] nodePool = powerSystem.nodePool;
                                EntityData[] entityPool = factory.entityPool;
                                TurretComponent[] buffer = factory.defenseSystem.turrets.buffer;
                                double num5 = 0.0;
                                Vector3 vector = Vector3.zero;
                                if (hive._assaultPosByQuadrant == null)
                                {
                                    hive._assaultPosByQuadrant = new Vector3[8];
                                }
                                else
                                {
                                    for (int j = 0; j < 8; j++)
                                    {
                                        hive._assaultPosByQuadrant[j] = Vector3.zero;
                                    }
                                }
                                bool flag3 = false;
                                for (int k = 1; k < consumerCursor; k++)
                                {
                                    ref PowerConsumerComponent ptr2 = ref consumerPool[k];
                                    if (ptr2.id == k)
                                    {
                                        double num6 = 0.01;
                                        int networkId = ptr2.networkId;
                                        PowerNetwork powerNetwork = powerSystem.netPool[networkId];
                                        ref Vector3 ptr3 = ref ptr2.plugPos;
                                        if (powerNetwork != null)
                                        {
                                            long num7 = powerNetwork.energyServed / 4L + powerNetwork.energyCapacity / 80L + (long)((double)ptr2.requiredEnergy * powerNetwork.consumerRatio);
                                            num6 += Math.Sqrt((double)num7 / 500000.0);
                                            int turretId = entityPool[ptr2.entityId].turretId;
                                            if (turretId > 0)
                                            {
                                                ref TurretComponent ptr4 = ref buffer[turretId];
                                                if (ptr4.type == ETurretType.Missile)
                                                {
                                                    num6 *= 10.0;
                                                }
                                                else if (ptr4.type == ETurretType.Plasma)
                                                {
                                                    num6 *= 100.0;
                                                }
                                            }
                                            int num8 = ((ptr3.x >= 0f) ? 1 : 0) + ((ptr3.y >= 0f) ? 2 : 0) + ((ptr3.z >= 0f) ? 4 : 0);
                                            hive._assaultPosByQuadrant[num8] += ptr3 * (float)num6;
                                        }
                                        if (num6 > num5)
                                        {
                                            num5 = num6;
                                            vector = ptr3;
                                            flag3 = true;
                                        }
                                    }
                                }
                                for (int l = 1; l < nodeCursor; l++)
                                {
                                    ref PowerNodeComponent ptr5 = ref nodePool[l];
                                    if (ptr5.id == l)
                                    {
                                        double num9 = 0.01;
                                        int networkId2 = ptr5.networkId;
                                        PowerNetwork powerNetwork2 = powerSystem.netPool[networkId2];
                                        ref Vector3 ptr6 = ref ptr5.powerPoint;
                                        if (powerNetwork2 != null)
                                        {
                                            int powerGenId = entityPool[ptr5.entityId].powerGenId;
                                            long num10 = (powerGenId > 0) ? powerSystem.genPool[powerGenId].generateCurrentTick : 0L;
                                            long num11 = (powerNetwork2.energyServed / 4L + powerNetwork2.energyCapacity / 80L + (long)(ptr5.idleEnergyPerTick / 2) + num10 / 20L) / 2L;
                                            num9 += Math.Sqrt((double)num11 / 500000.0);
                                            int num12 = ((ptr6.x >= 0f) ? 1 : 0) + ((ptr6.y >= 0f) ? 2 : 0) + ((ptr6.z >= 0f) ? 4 : 0);
                                            hive._assaultPosByQuadrant[num12] += ptr5.powerPoint * (float)num9;
                                        }
                                        if (num9 > num5)
                                        {
                                            num5 = num9;
                                            vector = ptr5.powerPoint;
                                            flag3 = true;
                                        }
                                    }
                                }
                                if (flag3)
                                {
                                    flag2 = true;
                                    targetAstroId = ptr.objectId;
                                    float num13 = 0f;
                                    for (int m = 0; m < 8; m++)
                                    {
                                        float magnitude = hive._assaultPosByQuadrant[m].magnitude;
                                        if (magnitude > num13)
                                        {
                                            num13 = magnitude;
                                            tarPos = hive._assaultPosByQuadrant[m];
                                        }
                                    }
                                    maxHatredPos = vector;
                                    break;
                                }
                            }
                        }
                    }
                }
                if (!flag2 && hive.gameData.localPlanet != null && hive.gameData.localPlanet.type != EPlanetType.Gas && hive.gameData.localPlanet.star == hive.starData)
                {
                    flag2 = true;
                    targetAstroId = hive.gameData.localPlanet.astroId;
                    maxHatredPos = (tarPos = hive.sector.skillSystem.playerSkillTargetL);
                }
                if (flag2)
                {
                    int num2 = 5;
                    int num14 = 100 / (num2 * 4 / 5);
                    if (hive.evolve.waves < 3)
                    {
                        num14 = 1;
                    }
                    int num15 = 100 - num14 * (num2);
                    hive.evolve.threat = 0;
                    hive.LaunchLancerAssault(EAggressiveLevel.Normal, tarPos, maxHatredPos, targetAstroId, assaultNum, num14);
                    hive.evolve.threat = 0;
                    hive.evolve.threatshr = 0;
                    hive.evolve.maxThreat = EvolveData.GetSpaceThreatMaxByWaves(hive.evolve.waves, EAggressiveLevel.Normal);
                    hive.lancerAssaultCountBase += hive.GetLancerAssaultCountIncrement(EAggressiveLevel.Normal);
                    return;
                }
            }
        }

        public void Import(BinaryReader r)
        {
            listIndex = r.ReadInt32();
            starIndex = r.ReadInt32();
            oriAstroId = r.ReadInt32();
            orbitIndex = r.ReadInt32();
            byAstroIndex = r.ReadInt32();
            strength = r.ReadInt32();
            state = (EAssaultHiveState)r.ReadInt32();
            time = r.ReadInt32();
            timeTillAssault = r.ReadInt32();
            timeTotalInit = r.ReadInt32();
            timeDelayedByRelic = r.ReadInt32();
            level = r.ReadInt32();
            oriLevel = r.ReadInt32();
            assaultNumTotal = r.ReadInt32();
            inhibitPoints = r.ReadInt32();
            inhibitPointsLimit = r.ReadInt32();
            inhibitPointsTotalInit = r.ReadInt32();
            canFullyStopped = r.ReadBoolean();
            isSuper = r.ReadBoolean();
            enemyKilled = r.ReadInt32();
            isCreated = r.ReadBoolean();
            hive = GameMain.data.spaceSector.GetHiveByAstroId(oriAstroId);
        }

        public void Export(BinaryWriter w)
        {
            w.Write(listIndex);
            w.Write(starIndex);
            w.Write(oriAstroId);
            w.Write(orbitIndex);
            w.Write(byAstroIndex);
            w.Write(strength);
            w.Write((int)state);
            w.Write(time);
            w.Write(timeTillAssault);
            w.Write(timeTotalInit);
            w.Write(timeDelayedByRelic);
            w.Write(level);
            w.Write(oriLevel);
            w.Write(assaultNumTotal);
            w.Write(inhibitPoints);
            w.Write(inhibitPointsLimit);
            w.Write(inhibitPointsTotalInit);
            w.Write(canFullyStopped);
            w.Write(isSuper);
            w.Write(enemyKilled);
            w.Write(isCreated);
        }

        public void IntoOtherSave()
        {
            state = EAssaultHiveState.Remove;
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
