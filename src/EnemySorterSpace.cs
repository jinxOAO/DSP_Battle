using System;
using System.Collections;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using UnityEngine;

namespace DSP_Battle
{
    public class EnemySorterSpace
    {
        public static int enemyPoolMaxLen = 50; // 敌人池子最大长度 

        public int starIndex; // 机甲所在星系
        public EEnemySearchMode searchMode;
        public int sortInterval; // 每多少帧重新寻敌
        public int unitPriorityFactorSquared; // 单位优先系数，对于以距离作为寻敌标准时，优先的单位会在比较距离时视作距离成倍减小
        public bool ignoreAssimilatingHive; // 寻敌时忽略同化中的建筑

        public List<SpaceEnemySortInfo> sortedPool;
        public List<SpaceEnemySortInfo> sortedPoolNear;

        public EnemySorterSpace(int starIndex, EEnemySearchMode searchMode, int unitPriorityFactor = 10)
        {
            this.starIndex = starIndex;
            this.searchMode = searchMode;
            this.unitPriorityFactorSquared = unitPriorityFactor * unitPriorityFactor;
            this.ignoreAssimilatingHive = true;
            this.sortInterval = 60;

            sortedPool = new List<SpaceEnemySortInfo>();
            sortedPoolNear = new List<SpaceEnemySortInfo>();

            if (GameMain.localStar != null)
                this.starIndex = GameMain.localStar.index;
        }

        public void GameTick(long time, bool CheckPoolPreFrame)
        {
            if (searchMode == EEnemySearchMode.None)
            {
                return;
            }
            else if (searchMode == EEnemySearchMode.NearMechaCurStar)
            {
                if (GameMain.localStar != null)
                    starIndex = GameMain.localStar.index;
                else
                {
                    starIndex = -1;
                }
                if(time % sortInterval == 47)
                {
                    SearchAndSort();
                }
                else if (CheckPoolPreFrame)
                {
                    CheckSortedPool();
                }
            }
        }

        public void SearchAndSort()
        {
            if(searchMode == EEnemySearchMode.NearMechaCurStar )
            {
                sortedPool.Clear();
                sortedPoolNear.Clear();
                if (starIndex >= 0)
                {
                    SpaceSector sector = GameMain.data.spaceSector;
                    EnemyData[] enemyPool = sector.enemyPool;
                    int enemyCursor = sector.enemyCursor;
                    EnemyDFHiveSystem[] dfHivesByAstro = sector.dfHivesByAstro;
                    Vector3 currentUPos = GameMain.mainPlayer.uPosition;
                    Vector3 mechaUPos = GameMain.mainPlayer.uPosition;
                    float defaultCheckDistance = 60000f * 60000f;

                    VectorLF3 zero = VectorLF3.zero;
                    for (int m = 0; m < enemyCursor; m++)
                    {
                        ref EnemyData ptr = ref enemyPool[m];
                        if (ptr.id != 0 && (ptr.dfRelayId == 0 || (bool)GameMain.data.mainPlayer?.mecha?.spaceCombatModule?.attackRelay))
                        {
                            if (ptr.dfTinderId != 0) // 水滴不攻击火种
                            {
                                continue;
                            }
                            else
                            {
                                if (GameMain.localStar == null)
                                    continue;
                                int byAstroIndex = ptr.originAstroId - 1000000;
                                EnemyDFHiveSystem enemyDFHiveSystem = dfHivesByAstro[byAstroIndex];
                                if (enemyDFHiveSystem == null)
                                    continue;
                                else if (enemyDFHiveSystem.starData.index != starIndex)
                                    continue;
                                else if (byAstroIndex>=0 && byAstroIndex < AssaultController.invincibleHives.Length && AssaultController.invincibleHives[byAstroIndex] >= 0 && !ptr.isAssaultingUnit) // 同化中的不会被选
                                    continue;
                            }
                            sector.TransformFromAstro_ref(ptr.astroId, out zero, ref ptr.pos);
                            float distanceX = (float)(zero.x - mechaUPos.x);
                            float distanceY = (float)(zero.y - mechaUPos.y);
                            float distanceZ = (float)(zero.z - mechaUPos.z);
                            float distanceSquared = distanceX * distanceX + distanceY * distanceY + distanceZ * distanceZ;
                            float distanceSquaredCalc = distanceSquared;
                            if (ptr.unitId > 0)
                                distanceSquaredCalc = distanceSquared / unitPriorityFactorSquared;
                            SpaceEnemySortInfo enemySortInfo = new SpaceEnemySortInfo();
                            enemySortInfo.enemyId = ptr.id;
                            enemySortInfo.distance = distanceSquaredCalc;
                            enemySortInfo.isUnit = ptr.unitId > 0;
                            BinaryInsert(ref sortedPool, ref enemySortInfo, enemyPoolMaxLen);
                            if (distanceSquared < defaultCheckDistance)
                                BinaryInsert(ref sortedPoolNear, ref enemySortInfo, enemyPoolMaxLen);
                        }
                    }
                }
            }
        }

        public void CheckSortedPool()
        {
            int oriPoolLen = sortedPool.Count;
            int oriNearPoolLen = sortedPoolNear.Count;
            SpaceSector sector = GameMain.data.spaceSector;
            EnemyData[] enemyPool = sector.enemyPool;
            EnemyDFHiveSystem[] dfHivesByAstro = sector.dfHivesByAstro;
            List<SpaceEnemySortInfo> oldPool = sortedPool;
            List<SpaceEnemySortInfo> oldPoolNear = sortedPoolNear;
            sortedPool = new List<SpaceEnemySortInfo>();
            sortedPoolNear = new List<SpaceEnemySortInfo>();
            for (int i = 0; i < oldPool.Count; i++)
            {
                ref EnemyData ptr = ref enemyPool[oldPool[i].enemyId];
                if (ptr.id > 0)
                {
                    int byAstroIndex = ptr.originAstroId - 1000000;
                    EnemyDFHiveSystem enemyDFHiveSystem = dfHivesByAstro[byAstroIndex];
                    if (enemyDFHiveSystem == null)
                        continue;
                    else if (enemyDFHiveSystem.starData.index != starIndex)
                        continue;
                    else if (byAstroIndex >= 0 && byAstroIndex < AssaultController.invincibleHives.Length && AssaultController.invincibleHives[byAstroIndex] >= 0 && !ptr.isAssaultingUnit) // 同化中的无敌单位不会被选为目标
                        continue;
                    sortedPool.Add(oldPool[i]); 
                }
            }
            for (int i = 0; i < oldPoolNear.Count; i++)
            {
                ref EnemyData ptr = ref enemyPool[oldPoolNear[i].enemyId];
                if (ptr.id > 0)
                {
                    int byAstroIndex = ptr.originAstroId - 1000000;
                    EnemyDFHiveSystem enemyDFHiveSystem = dfHivesByAstro[byAstroIndex];
                    if (enemyDFHiveSystem == null)
                        continue;
                    else if (enemyDFHiveSystem.starData.index != starIndex)
                        continue;
                    else if (byAstroIndex >= 0 && byAstroIndex < AssaultController.invincibleHives.Length && AssaultController.invincibleHives[byAstroIndex] >= 0 && !ptr.isAssaultingUnit) // 同化中的无敌单位不会被选为目标
                        continue;
                    sortedPoolNear.Add(oldPoolNear[i]);
                }
            }
            if(oriPoolLen != 0 && sortedPool.Count == 0 || oriNearPoolLen != 0 && sortedPoolNear.Count == 0)
            {
                SearchAndSort();
            }
        }

        public void RemoveEnemyFromPoolById(int enemyId)
        {

        }

        public void BinaryInsert(ref List<SpaceEnemySortInfo> list, ref SpaceEnemySortInfo data, int maxLen)
        {
            if(list.Count >= maxLen)
                list.RemoveAt(list.Count - 1);

            int i = 0;
            int j = list.Count - 1;
            while(i <= j)
            {
                int center = (i + j) / 2;
                if (data.distance < list[center].distance)
                {
                    j = center - 1;
                }
                else if (data.distance > list[center].distance)
                {
                    i = center + 1;
                }
                else
                {
                    list.Insert(center, data);
                    return;
                }
            }
            list.Insert(i, data);
        }
    }

    public struct SpaceEnemySortInfo
    {
        public int enemyId;
        public float distance;
        public bool isUnit;
    }

    public enum EEnemySearchMode
    {
        None,
        NearMechaCurStar,
    }
}
