using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using HarmonyLib;
using Steamworks;

namespace DSP_Battle
{
    public class EventSystem
    {
        public static Dictionary<int, EventProto> protos;
        public static List<List<Tuple<int, int>>> alterItems; // 用于上交的物品的id和数量，以等级分（alterItems[level][i])
        public static EventRecorder recorder;
        public static Dictionary<int,List<int>> alterProtos;

        public static void ClearBeforeLoad()
        {
            ClearEvent();
        }

        public static void InitNewEvent(int id = 0)
        {
            if (id == 0)
            {
                if (Relic.GetRelicCount() == 0)
                    SetEvent(1001);
                else
                    SetEvent(1002);
            }
            else
                SetEvent(id);
            UIEventSystem.OnOpen();
        }

        public static void SetEvent(int id)
        {
            recorder = new EventRecorder(id);
            RefreshRequestMeetData();
        }
        
        public static void TransferTo(int id)
        {
            EventRecorder next = new EventRecorder(id, recorder.modifier, recorder.level);
            recorder = next;
            RefreshRequestMeetData();
        }

        public static void PrintData()
        {
            Utils.Log("requestId");
            for (int i = 0; i < recorder.requestId.Length; i++)
            {
                Utils.Log(recorder.requestId[i].ToString());
            }
            Utils.Log("requestCount");
            for (int i = 0; i < recorder.requestCount.Length; i++)
            {
                Utils.Log(recorder.requestCount[i].ToString());
            }
            Utils.Log("requestMeet");
            for (int i = 0; i < recorder.requestMeet.Length; i++)
            {
                Utils.Log(recorder.requestMeet[i].ToString());
            }
        }

        public static void ClearEvent()
        {
            recorder = new EventRecorder(0);
        }

        public static void Decision(int index)
        {
            if (recorder == null || !protos.ContainsKey(recorder.protoId))
            {
                if(recorder != null && recorder.protoId != 0)
                    ClearEvent();
                return;
            }
            EventProto proto = protos[recorder.protoId];
            if(index >= proto.decisionLen)
            {
                return;
            }
            bool satisfied = true;
            for (int i = 0;i < proto.decisionRequestNeed[index].Length; i++)
            {
                int reqIndex = proto.decisionRequestNeed[index][i];
                if (recorder.requestMeet[reqIndex] < recorder.requestCount[reqIndex])
                {
                    satisfied = false;
                    int fullCode = recorder.requestId[reqIndex];
                    if (fullCode >= 40000 && fullCode < 99999)
                    {
                        int baseCode = fullCode / 10000 * 10000;
                        int starIndex = fullCode - baseCode;
                        if(starIndex < GameMain.galaxy.starCount)
                        {
                            PlayerNavigation navigation = GameMain.mainPlayer.navigation;
                            navigation.indicatorAstroId = (starIndex + 1) * 100;
                        }
                    }
                    else if(fullCode > 1000000 && fullCode < 2000000)
                    {
                        PlayerNavigation navigation = GameMain.mainPlayer.navigation;
                        navigation.indicatorAstroId = fullCode;
                    }
                    else if (fullCode > 2000000 && fullCode < 5000000)
                    {
                        int baseCode = fullCode / 1000000 * 1000000;
                        int planetId = fullCode - baseCode;
                        PlayerNavigation navigation = GameMain.mainPlayer.navigation;
                        navigation.indicatorAstroId = planetId;
                    }
                    break;
                }
            }
            if(!satisfied)
            {
                return;
            }
            for (int i = 0; i < proto.decisionRequestNeed[index].Length; i++)
            {
                int reqIndex = proto.decisionRequestNeed[index][i];
                int fullCode = recorder.requestId[reqIndex];
                if(fullCode > 10000 && fullCode < 20000)
                {
                    int itemId = fullCode - 10000;
                    int needCount = recorder.requestCount[reqIndex];
                    int inc;
                    GameMain.data.mainPlayer.package.TakeTailItems(ref itemId, ref needCount, out inc);
                }
            }
            int[] resultIds = proto.decisionResultId[index];
            bool willClearEvent = false;
            for (int i = 0; i < resultIds.Length; i++)
            {
                int code = resultIds[i];
                int amount = proto.decisionResultCount[index][i];
                if (code == -1)
                {
                    willClearEvent = true;
                }
                else if (code == 0)
                {
                    for (int m = 0; i < recorder.modifier.Length; i++)
                    {
                        Relic.modifierByEvent[m] = recorder.modifier[m];
                    }
                    Relic.PrepareNewRelic(recorder.modifier[5]);
                    willClearEvent = true;
                }
                else if (code == 1)
                    Rank.AddExp(amount);
                else if (code == 2)
                {
                    Rank.rank += amount;
                    if (Rank.rank > 10)
                        Rank.rank = 10;
                    if (Rank.rank < 0)
                        Rank.rank = 0;
                }
                else if (code == 3)
                {
                    Relic.autoConstructMegaStructureCountDown += amount / 120;
                }
                else if (code == 4)
                    recorder.modifier[3] += amount;
                else if (code == 5)
                    recorder.modifier[2] += amount;
                else if (code == 6)
                    recorder.modifier[1] += amount;
                else if (code == 7)
                    recorder.modifier[0] += amount;
                else if (code == 8)
                    recorder.modifier[4] += amount;
                else if (code == 9)
                    recorder.modifier[5] += amount;
                else if (code == 10)
                {
                    if (MoreMegaStructure.StarCannon.time < 0)
                    {
                        MoreMegaStructure.StarCannon.time += amount * 60;
                        if (MoreMegaStructure.StarCannon.time > 0)
                            MoreMegaStructure.StarCannon.time = 0;
                    }
                }
                else if (code >= 11 && code <= 19)
                {
                    int nextEKey = (code - 10) * 10 + recorder.level;
                    int minKey = nextEKey / 10 * 10;
                    while (nextEKey > minKey && ((!alterProtos.ContainsKey(nextEKey)) || alterProtos[nextEKey] == null || alterProtos[nextEKey].Count == 0))
                    {
                        nextEKey--;
                    }
                    if ((!alterProtos.ContainsKey(nextEKey)) || alterProtos[nextEKey] == null || alterProtos[nextEKey].Count == 0)
                    {
                        TransferTo(9999);
                    }
                    else
                    {
                        int nextId = alterProtos[nextEKey][Utils.RandInt(0, alterProtos[nextEKey].Count())];
                        TransferTo(nextId);
                    }
                }
                else if (code >= 20 && code < 30)
                {
                    recorder.decodeType = code;
                    recorder.decodeTimeNeed = amount;
                    recorder.decodeTimeSpend = 0;
                }
                else if (code > 10000 && code < 20000)
                {
                    TransferTo(code - 10000);
                }
                else if (code >= 20000 && code <= 30000)
                {
                    GameMain.data.mainPlayer.TryAddItemToPackage(code - 20000, amount, 0, true);
                    Utils.UIItemUp(code - 20000, amount);
                }
                else
                {
                    // 暂未实现
                }
            }
            if(willClearEvent)
            {
                UIEventSystem.OnClose();
                ClearEvent();
            }
            UIEventSystem.RefreshESWindow(true);
        }


        [HarmonyPostfix]
        [HarmonyPatch(typeof(GameData), "GameTick")]
        public static void OnUpdate()
        {
            if(recorder != null && recorder.protoId > 0 && recorder.decodeType > 20)
            {
                if (recorder.decodeTimeSpend < recorder.decodeTimeNeed)
                    recorder.decodeTimeSpend++;
                else
                {
                    recorder.decodeTimeNeed = 0;
                    recorder.decodeTimeSpend = 0;
                    recorder.decodeType = 0;
                }
            }
            RefreshRequestMeetData();
        }


        public static void Exprot(BinaryWriter w)
        {

        }
        public static void Import(BinaryReader r)
        {
            ClearBeforeLoad();
            UIEventSystem.RefreshAll();
        }

        public static void IntoOtherSave()
        {
            ClearBeforeLoad();
            UIEventSystem.RefreshAll();
        }



        /// <summary>
        /// 每秒更新
        /// </summary>
        public static void RefreshRequestMeetData()
        {
            if (recorder != null && recorder.protoId > 0 && recorder.requestLen > 0)
            {

                for (int i = 0; i < recorder.requestLen; i++)
                {
                    int code = recorder.requestId[i];
                    if (code <= 0)
                    {
                        recorder.requestMeet[i] = recorder.requestCount[i];
                    }
                    else if (code == 9995)
                        recorder.requestMeet[i] = Rank.rank;
                    else if (code >= 10000 && code < 20000)
                    {
                        int itemId = code - 10000;
                        recorder.requestMeet[i] = Math.Min(GameMain.mainPlayer.package.GetItemCount(itemId), recorder.requestCount[i]);
                    }
                    else if (code >= 20000 && code < 30000)
                    {

                    }
                    else if (code > 30000 && code < 40000) // 等于30000是任意科技，不需要每秒更新
                    {
                        int techId = code - 30000;
                        recorder.requestMeet[i] = GameMain.data.history.TechState(techId).curLevel;
                    }
                    else if (code >= 40000 && code < 50000)
                    {

                    }
                    else if (code >= 50000 && code < 60000)
                    {
                        if (recorder.requestCount[i] == 0)
                        {
                            int starIndex = code - 50000;
                            EnemyData[] pool = GameMain.data.spaceSector.enemyPool;
                            EnemyDFHiveSystem[] dfHivesByAstro = GameMain.data.spaceSector.dfHivesByAstro;
                            int remaining = 0;
                            for (int j = 0; j < GameMain.data.spaceSector.enemyCursor; j++)
                            {
                                ref EnemyData ptr = ref pool[j];
                                if (ptr.dfTinderId != 0)
                                    continue;
                                if (ptr.id == 0)
                                    continue;
                                EnemyDFHiveSystem enemyDFHiveSystem = dfHivesByAstro[ptr.originAstroId - 1000000];
                                if (enemyDFHiveSystem != null && enemyDFHiveSystem.starData?.index == starIndex)
                                    remaining++;
                            }
                            recorder.requestMeet[i] = -remaining;
                        }
                    }
                    else if (code >= 60000 && code < 70000)
                    {
                        int starIndex = code - 60000;
                        EnemyData[] pool = GameMain.data.spaceSector.enemyPool;
                        EnemyDFHiveSystem[] dfHivesByAstro = GameMain.data.spaceSector.dfHivesByAstro;
                        int lastOriAstroId = -1;
                        for (int j = 0; j < GameMain.data.spaceSector.enemyCursor; j++)
                        {
                            ref EnemyData ptr = ref pool[j];
                            if (ptr.dfTinderId != 0)
                                continue;
                            if (ptr.id == 0)
                                continue;
                            if (ptr.originAstroId == lastOriAstroId)
                                continue;
                            EnemyDFHiveSystem enemyDFHiveSystem = dfHivesByAstro[ptr.originAstroId - 1000000];
                            if (enemyDFHiveSystem != null && enemyDFHiveSystem.starData?.index == starIndex)
                            {
                                int cur = (int)(enemyDFHiveSystem.evolve.threat * 100.0 / enemyDFHiveSystem.evolve.maxThreat);
                                if (cur > recorder.requestMeet[i])
                                    recorder.requestMeet[i] = cur;
                            }
                            lastOriAstroId = ptr.originAstroId;
                        }
                    }
                    else if (code >= 70000 && code < 80000)
                    {
                        int starIndex = code - 70000;
                        int remaining = 0;
                        bool unknown = false;
                        int planetCount = (int)GameMain.galaxy.StarById(starIndex + 1)?.planetCount;
                        for (int j = 0; j < planetCount; j++)
                        {
                            PlanetData planet = GameMain.galaxy.StarById(starIndex + 1)?.planets[j];
                            if (planet != null && planet.type != EPlanetType.Gas)
                            {
                                PlanetFactory factory = planet.factory;
                                if (factory == null) // 尚未落足过的行星无法得知enemy数量？那就无法统计是否已消灭
                                {
                                    unknown = true;
                                    break;
                                }
                                else
                                {
                                    EnemyData[] gPool = factory.enemyPool;
                                    for (int k = 0; k < factory.enemyCursor; k++)
                                    {
                                        ref EnemyData ptr = ref gPool[k];
                                        if (ptr.id > 0)
                                            remaining++;
                                    }
                                }
                            }
                        }
                        if (!unknown)
                        {
                            EnemyData[] pool = GameMain.data.spaceSector.enemyPool;
                            EnemyDFHiveSystem[] dfHivesByAstro = GameMain.data.spaceSector.dfHivesByAstro;
                            for (int j = 0; j < GameMain.data.spaceSector.enemyCursor && !unknown; j++)
                            {
                                ref EnemyData ptr = ref pool[j];
                                if (ptr.dfTinderId != 0)
                                    continue;
                                if (ptr.id == 0)
                                    continue;
                                EnemyDFHiveSystem enemyDFHiveSystem = dfHivesByAstro[ptr.originAstroId - 1000000];
                                if (enemyDFHiveSystem != null && enemyDFHiveSystem.starData?.index == starIndex)
                                    remaining++;

                            }
                        }
                        recorder.requestMeet[i] = unknown ? int.MinValue : -remaining;
                    }
                    else if (code >= 80000 && code < 90000)
                    {
                        int starIndex = code - 80000;
                        int maxLen = GameMain.data.dysonSpheres.Length;
                        if (starIndex != 0 && starIndex < maxLen)
                        {
                            DysonSphere sphere = GameMain.data.dysonSpheres[starIndex];
                            if (sphere != null)
                            {
                                long gen = GameMain.data.dysonSpheres[starIndex].energyGenCurrentTick * 60 / 1000000;
                                if (gen > recorder.requestCount[i])
                                    gen = recorder.requestCount[i];
                                recorder.requestMeet[i] = (int)gen;
                            }
                            else
                                recorder.requestMeet[i] = 0;
                        }
                        else
                        {
                            long maxGen = 0;
                            for (int j = 0; j < maxLen; j++)
                            {
                                DysonSphere sphere = GameMain.data.dysonSpheres[j];
                                if(sphere!=null)
                                {
                                    maxGen = Math.Max(maxGen, sphere.energyGenCurrentTick * 60 / 1000000);
                                }
                            }
                            if (maxGen > int.MaxValue)
                                maxGen = int.MaxValue;
                            recorder.requestMeet[i] = (int)maxGen;
                        }
                    }
                    else if (code >= 90000 && code < 100000)
                    {
                        int starIndex = code - 90000;
                        EnemyData[] pool = GameMain.data.spaceSector.enemyPool;
                        EnemyDFHiveSystem[] dfHivesByAstro = GameMain.data.spaceSector.dfHivesByAstro;
                        int lastOriAstroId = -1;
                        for (int j = 0; j < GameMain.data.spaceSector.enemyCursor; j++)
                        {
                            ref EnemyData ptr = ref pool[j];
                            if (ptr.dfTinderId != 0)
                                continue;
                            if (ptr.id == 0)
                                continue;
                            if (ptr.originAstroId == lastOriAstroId)
                                continue;
                            EnemyDFHiveSystem enemyDFHiveSystem = dfHivesByAstro[ptr.originAstroId - 1000000];
                            if (enemyDFHiveSystem != null && enemyDFHiveSystem.starData?.index == starIndex)
                            {
                                int cur = enemyDFHiveSystem.evolve.level;
                                if (cur > recorder.requestMeet[i])
                                    recorder.requestMeet[i] = cur;
                            }
                            lastOriAstroId = ptr.originAstroId;
                        }
                    }
                    else if (code >= 1000000 && code < 2000000)
                    {
                        EnemyData[] pool = GameMain.data.spaceSector.enemyPool;
                        int remaining = 0;
                        for (int j = 0; j < GameMain.data.spaceSector.enemyCursor; j++)
                        {
                            ref EnemyData ptr = ref pool[j];
                            if (ptr.originAstroId != code)
                                continue;
                            if (ptr.dfTinderId != 0)
                                continue;
                            if (ptr.id == 0)
                                continue;
                            remaining++;
                        }
                        recorder.requestMeet[i] = -remaining;
                    }
                    else if (code >= 2000000 && code < 3000000)
                    {
                        if (recorder.requestCount[i] == 0)
                        {
                            int planetId = code - 2000000;
                            PlanetData planet = GameMain.galaxy.PlanetById(planetId);
                            if(planet != null)
                            {
                                PlanetFactory factory = planet.factory;
                                if (factory == null)
                                    recorder.requestMeet[i] = int.MinValue;
                                else
                                {
                                    int remaining = 0;
                                    EnemyData[] gPool = factory.enemyPool;
                                    for (int j = 0; j < factory.enemyCursor; i++)
                                    {
                                        ref EnemyData ptr = ref gPool[j];
                                        if (ptr.id > 0)
                                            remaining++;
                                    }
                                    recorder.requestMeet[i] = -remaining;
                                }
                            }
                            else
                            {
                                recorder.requestMeet[i] = 0;
                            }
                        }
                    }
                    else if (code >= 3000000 && code < 4000000)
                    {
                        if (recorder.requestCount[i] == 0)
                        {
                            int planetId = code - 3000000;
                            PlanetData planet = GameMain.galaxy.PlanetById(planetId);
                            if (planet != null)
                            {
                                PlanetFactory factory = planet.factory;
                                if (factory == null)
                                    recorder.requestMeet[i] = int.MinValue;
                                else
                                {
                                    int remaining = 0;
                                    EnemyData[] gPool = factory.enemyPool;
                                    for (int j = 0; j < factory.enemyCursor; i++)
                                    {
                                        ref EnemyData ptr = ref gPool[j];
                                        if (ptr.id > 0 && ptr.dfGBaseId > 0)
                                            remaining++;
                                    }
                                    recorder.requestMeet[i] = -remaining;
                                }
                            }
                            else
                            {
                                recorder.requestMeet[i] = 0;
                            }
                        }
                    }
                    else if (code >= 4000000 && code < 5000000)
                    {
                        int planetId = code - 4000000;
                        if(GameMain.data.localPlanet != null && GameMain.data.localPlanet.id == planetId)
                        {
                            recorder.requestMeet[i] = recorder.requestCount[i];
                        }
                    }
                }

            }
        }

        [HarmonyPrefix]
        [HarmonyPatch(typeof(CombatStat), "HandleZeroHp")]
        public static bool ZeroHpInceptor(ref CombatStat __instance, GameData gameData, SkillSystem skillSystem)
        {
            if (recorder != null && recorder.protoId > 0 && recorder.requestId.Length > 0 || true)
            {
                var _this = __instance;
                KillStatistics killStatistics = skillSystem.killStatistics;
                if (_this.originAstroId > 1000000) // 太空
                {
                    if (_this.objectType == 4)
                    {
                        ref EnemyData ptr = ref skillSystem.sector.enemyPool[_this.objectId];
                        if (ptr.id > 0)
                        {
                            for (int i = 0; i < recorder.requestLen; i++)
                            {
                                int code = recorder.requestId[i];
                                if (code == 9998 || code == 9999 && recorder.requestCount[i] > recorder.requestMeet[i])
                                {
                                    recorder.requestMeet[i]++;
                                }
                                else if (code >= 50000 && code < 60000 && recorder.requestCount[i] > recorder.requestMeet[i] && recorder.requestCount[i] > 0)
                                {
                                    int starIndex = code - 50000;
                                    EnemyDFHiveSystem[] dfHivesByAstro = GameMain.data.spaceSector.dfHivesByAstro;
                                    EnemyDFHiveSystem enemyDFHiveSystem = dfHivesByAstro[ptr.originAstroId - 1000000];
                                    int ptrStarIndex = enemyDFHiveSystem?.starData?.index ?? -1;
                                    if(starIndex == ptrStarIndex)
                                        recorder.requestMeet[i]++;
                                }
                            }
                            EnemyDFHiveSystem[] dfHivesByAstro2 = GameMain.data.spaceSector.dfHivesByAstro;
                            EnemyDFHiveSystem enemyDFHiveSystem2 = dfHivesByAstro2[ptr.originAstroId - 1000000];
                            int level = enemyDFHiveSystem2?.evolve.level ?? 0;
                            if (ptr.dfSConnectorId + ptr.dfSGammaId + ptr.dfSNodeId + ptr.dfSReplicatorId + ptr.dfSTurretId + ptr.dfRelayId > 0)
                                Rank.AddExp(30 * (level + 1));
                            else if (ptr.dfSCoreId > 0)
                                Rank.AddExp(50 * (level + 1));
                            else if (ptr.dfTinderId > 0)
                                Rank.AddExp(1000);
                            else
                                Rank.AddExp(5 * (level + 1));

                            if (Relic.HaveRelic(0, 0))
                            {
                                Relic.autoConstructMegaStructurePPoint += 20 * (level / 15 + 1);
                            }
                        }
                    }
                }
                else if (_this.originAstroId > 100 && _this.originAstroId <= 204899 && _this.originAstroId % 100 > 0)
                {
                    PlanetFactory planetFactory = skillSystem.astroFactories[_this.originAstroId];
                    if (planetFactory != null)
                    {
                        if (_this.objectType == 4)
                        {
                            ref EnemyData ptr3 = ref planetFactory.enemyPool[_this.objectId];
                            if (ptr3.id > 0)
                            {
                                for (int i = 0; i < recorder.requestLen; i++)
                                {
                                    int code = recorder.requestId[i];
                                    if (code == 9997 || code == 9999 && recorder.requestCount[i] > recorder.requestMeet[i])
                                    {
                                        recorder.requestMeet[i]++;
                                    }
                                    else if (code >= 40000 && code < 50000 && recorder.requestCount[i] > recorder.requestMeet[i])
                                    {
                                        int starIndex = code - 40000;
                                        if(ptr3.originAstroId/100 - 1 == starIndex)
                                            recorder.requestMeet[i]++;
                                    }
                                    else if(code >= 2000000 && code < 3000000 && recorder.requestCount[i] > recorder.requestMeet[i] && recorder.requestCount[i] > 0)
                                    {
                                        int planetId = code - 2000000;
                                        if(ptr3.originAstroId == planetId)
                                            recorder.requestMeet[i]++;
                                    }
                                }
                                int level = 0;
                                if(ptr3.owner > 0)
                                {
                                    DFGBaseComponent dfgbase = planetFactory.enemySystem.bases[ptr3.owner];
                                    level = dfgbase?.evolve.level ?? 0;
                                }
                                if (ptr3.dfGConnectorId + ptr3.dfGReplicatorId + ptr3.dfGShieldId + ptr3.dfGTurretId > 0)
                                    Rank.AddExp(10 * (level + 1));
                                else if (ptr3.dfGBaseId > 0)
                                    Rank.AddExp(200 * (level + 1));
                                else
                                    Rank.AddExp(level + 1);

                                if (Relic.HaveRelic(0, 0))
                                    Relic.autoConstructMegaStructurePPoint += (level + 10) / 10;
                            }
                        }
                    }
                }
            }
            return true;
        }


        [HarmonyPostfix]
        [HarmonyPatch(typeof(GameHistoryData), "NotifyTechUnlock")]
        public static void UnlockTechHandler()
        {
            if(recorder!=null && recorder.protoId > 0)
            {
                for (int i = 0; i < recorder.requestLen; i++)
                {
                    int code = recorder.requestId[i];
                    if(code == 30000 && recorder.requestCount[i] > recorder.requestMeet[i])
                    {
                        recorder.requestMeet[i]++;
                    }
                }
            }
        }

        public static void TestIfGroudBaseInited()
        {
            if(GameMain.data.localPlanet != null)
            {
                EnemyData[] gPool = GameMain.data.localPlanet.factory?.enemyPool;
                if (gPool != null)
                {
                    for (int i = 0; i < (GameMain.data.localPlanet.factory?.enemyCursor ?? 0); i++)
                    {
                        ref EnemyData ptr = ref gPool[i];

                        //Utils.Log($"oriAstro is {ptr.originAstroId} and astro is {ptr.astroId}");
                    }
                }
            }
            return;
            EnemyData[] pool = GameMain.data.spaceSector.enemyPool;
            for (int i = 0; i < GameMain.data.spaceSector.enemyCursor; i++)
            {
                ref EnemyData ptr = ref pool[i];
                if (ptr.dfRelayId == 0)
                    continue;

                EnemyDFHiveSystem[] dfHivesByAstro = GameMain.data.spaceSector.dfHivesByAstro;
                EnemyDFHiveSystem hive = dfHivesByAstro[ptr.originAstroId - 1000000];
                
                Utils.Log($"oriAstro is {ptr.originAstroId} and hive astro is {hive.hiveAstroId}");
            }
        }
    }
}
