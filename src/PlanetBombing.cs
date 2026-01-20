using HarmonyLib;
using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using UnityEngine;
using xiaoye97;
using static System.Collections.Specialized.BitVector32;
using static UnityEngine.EventSystems.EventTrigger;

namespace DSP_Battle
{
    public class PlanetBombing
    {
        // 存档内容
        public static bool isPurgeBombing;
        public static int purgeBombingTime;
        public static int purgeCoolDown;
        public static bool isGuideBombing;
        public static int guideBombingTime;
        public static int guideCoolDown;

        
        // 不存档
        public static int maxPurgeBombingTime = 2400;
        public static float flyPurgeWaveDelayRatio = 0.2f; // 第二波飞行轰炸的延迟（在maxtime的什么位置开始）
        public static float flyPurgeHeight = 25f;
        public static int purgeMaxCD = 2700;
        public static int guideMaxCD = 7200;

        public static int moduleFleetProtoId = 5;

        public static int fireWorkCountDown;
        public static bool haveGotNewYearGift;

        public static float minMechaEnergyCapCoeff = 0.004f; // 初始，每秒消耗机甲最大能量的百分比
        public static float growMechaEnergyCapCoeff = 0.020f; // 成长到最大时，每秒（除了初始之外）额外消耗机甲最大能量的百分比。最终最大值是此值+minMechaEnergyCapCoeff
        public static int energyCapCoeffGrowTime = 3600; // 需要多久，消耗机甲最大能量的百分比值能成长到最大值

        public static int guideBasicEnergyComsumption = 50000; // 初始太阳轰炸每帧耗能
        public static float b = 5000f;
        public static float a = 500f; // ax^2 + bx + guideBasicEnergyComsumption 为引导太阳轰炸的非百分比耗能部分，x为已连续引导太阳轰炸秒数（而非帧数）

        public static bool LastFrameLocalPlanetNotNull = false;

        public static bool purgeReady { get { return purgeCoolDown <= 0 && !isPurgeBombing; } }
        public static bool guideReady { get { return guideCoolDown <= 0; } }

        public static int bombItemId = 1606;

        public static void InitWhenLoad()
        {
            isPurgeBombing = false;
            isGuideBombing = false;
            purgeBombingTime = 0;
            guideBombingTime = 0;
            purgeCoolDown = 0;
            guideCoolDown = 0;
            fireWorkCountDown = 0;
            haveGotNewYearGift = false;
            if (MoreMegaStructure.MoreMegaStructure.GenesisCompatibility)
                bombItemId = 7613;
        }


        [HarmonyPostfix]
        [HarmonyPatch(typeof(ThreadManager), "ProcessFrame")]
        public static void Update()
        {
            if (GameMain.localPlanet != null)
            {
                DateTime now = DateTime.Now;
                if (now.Month == 12 && now.Day == 31 && now.Hour == 23 && now.Minute == 59 && now.Second >= 58 || now.Month == 1 && now.Day == 1 && now.Hour == 0 && now.Minute == 0 && now.Second <= 59) // 跨年瞬间给礼物
                {
                    fireWorkCountDown = 599;
                    if (!haveGotNewYearGift)
                    {
                        haveGotNewYearGift = true;
                        SkillPoints.totalPoints += 100;
                        UIDialogPatch.ShowUIDialog("新年快乐标题".Translate(), "新年礼物内容".Translate());
                    }
                }
                else if (!LastFrameLocalPlanetNotNull && DateTime.Today.Month == 12 && DateTime.Today.Day == 31 || !LastFrameLocalPlanetNotNull && DateTime.Today.Month == 1 && DateTime.Today.Day == 1) // 1号踏入星球会触发
                {
                    fireWorkCountDown = 900; // 倒数5秒后烟花
                }

                LastFrameLocalPlanetNotNull = true;
            }
            if(GameMain.localPlanet == null)
            {
                fireWorkCountDown = 0;
                LastFrameLocalPlanetNotNull = false;
            }


            if (isPurgeBombing)
            {
                purgeBombingTime++;
                if (purgeBombingTime >= 0)
                {
                    if (purgeBombingTime > maxPurgeBombingTime)
                    {
                        isPurgeBombing = false;
                    }
                    int bombFactor = 2;
                    float bombingPhase = purgeBombingTime * 1.0f /(maxPurgeBombingTime * (1f-flyPurgeWaveDelayRatio));
                    float timeToEquator = Mathf.Abs(bombingPhase - 0.5f);
                    bombFactor = (int)((0.5f - timeToEquator) * 12) + 4;
                    if (bombingPhase <= 1)
                    {
                        for (int i = 0; i < bombFactor; i++)
                        {
                            PurgeBombCircular(bombingPhase);
                        }
                    }
                    // 低空轰炸（对飞行单位）
                    bombingPhase = (purgeBombingTime * 1.0f - maxPurgeBombingTime * flyPurgeWaveDelayRatio) / (maxPurgeBombingTime * (1f - flyPurgeWaveDelayRatio)); 
                    timeToEquator = Mathf.Abs(bombingPhase - 0.5f);
                    bombFactor = (int)((0.5f - timeToEquator) * 12) + 4;
                    if (bombingPhase >= 0)
                    {
                        for (int i = 0; i < bombFactor; i++)
                        {
                            PurgeBombCircular(bombingPhase, flyPurgeHeight);
                        }
                    }
                    for (int i = 0; i < 3; i++)
                        GuideBomb(false); // 同时还会稀疏地轰炸全星球
                }
                else // 处在倒计时阶段
                {
                    if(purgeBombingTime == - 60)
                        UIRealtimeTip.Popup("<color=#ff2020><size=50>1</size></color>".Translate());
                    else if (purgeBombingTime == -120)
                        UIRealtimeTip.Popup("<color=#ff2020><size=50>2</size></color>".Translate());
                    else if (purgeBombingTime == -180)
                        UIRealtimeTip.Popup("<color=#ff2020><size=50>3</size></color>".Translate());
                }
            }
            if(isGuideBombing)
            {
                guideBombingTime++;
                // 描述了范围逐渐增大，轰炸数目逐渐增大
                float range = 10 + 1.0f * guideBombingTime / 8;
                if (range > 80)
                    range = 80;
                float strength = 3f * range * range / 6400;
                int realFactor = (int)strength;
                float over = strength - (int)strength;
                if (Utils.RandDouble() < over)
                    realFactor++;
                for (int i = 0; i < realFactor; i++)
                {
                    GuideBomb(true, 3, range);
                }
                long energyConsumption = 0;
                int timeTillMax1 = guideBombingTime > energyCapCoeffGrowTime ? guideBombingTime : energyCapCoeffGrowTime; 
                float maxEnergyCapCoefficient = (0.01f + (0.04f * timeTillMax1/ energyCapCoeffGrowTime)) / 60f; // 每秒消耗1%最大能量，逐渐增加直至1分钟时每秒消耗5%最大能量
                energyConsumption += (long)(maxEnergyCapCoefficient * GameMain.mainPlayer.mecha.coreEnergyCap);
                // 然后消耗随时间增加的固定能量
                float tSecond = 1.0f * guideBombingTime / 60;
                energyConsumption += (long)(a * tSecond * tSecond + b * tSecond + guideBasicEnergyComsumption);

                GameMain.mainPlayer.mecha.coreEnergy -= energyConsumption;
                GameMain.mainPlayer.mecha.MarkEnergyChange(22, -energyConsumption);
                if(GameMain.mainPlayer.mecha.coreEnergy <= GameMain.mainPlayer.mecha.coreEnergyCap * 0.1) // 如果少于10%能量，则停止
                {
                    StopGuideBombing();
                    if (GameMain.mainPlayer.mecha.coreEnergy < 0)
                        GameMain.mainPlayer.mecha.coreEnergy = 0;
                }
            }
            if (fireWorkCountDown > 0 && fireWorkCountDown <= 600)
                FireworkTest();

            if (purgeCoolDown > 0)
                purgeCoolDown--;
            if (guideCoolDown > 0)
                guideCoolDown--;
            if(fireWorkCountDown > 0)
                fireWorkCountDown--;
        }


        public static void LaunchPurgeBombing()
        {
            UIRealtimeTip.Popup("启动行星清洗警告".Translate());
            isPurgeBombing = true;
            purgeBombingTime = -240;
            purgeCoolDown = purgeMaxCD;
        }

        public static void LaunchGuideBombing()
        {
            if (!isGuideBombing)
            {
                UIRealtimeTip.Popup("启动太阳轰炸警告".Translate());
                isGuideBombing = true;
                guideBombingTime = 0;
                guideCoolDown = 0;
            }
            else
            {
                StopGuideBombing();
            }

        }
       
        public static void StopGuideBombing()
        {
            isGuideBombing = false;
            guideBombingTime = 0;
            guideCoolDown = guideMaxCD;
            UIRealtimeTip.Popup("太阳轰炸已终止".Translate());
        }

        /// <summary>
        /// 以一定宽度的环形从北极到南极，地毯式轰炸整个星球
        /// </summary>
        public static void PurgeBombCircular(float latitudeRatio, float height = 0)
        {
            if (GameMain.localPlanet == null)
            {
                isPurgeBombing = false;
                return;
            }
            Vector3 toCenterDir = GameMain.mainPlayer.uPosition - GameMain.localPlanet.uPosition;

            float planetRadius = GameMain.galaxy.astrosData[GameMain.localPlanet.astroId].uRadius;

            Vector3 targetLPos = Quaternion.AngleAxis(latitudeRatio * 180 + ((float)Utils.RandDouble() - 0.5f) * 15, new Vector3(1, 0, 0)) * new Vector3(0, 1, 0);
            targetLPos = Quaternion.AngleAxis((float)Utils.RandDouble() * 360, new Vector3(0, 1, 0)) * targetLPos;
            //if(Utils.RandDouble() < 0.4f)
            //    targetLPos = targetLPos.normalized * (planetRadius + 15); // 有40概率往低空打
            //else
                targetLPos = targetLPos.normalized * (planetRadius + height);


            ItemProto itemProto = LDB.items.Select(bombItemId);
            ref SkillSystem skillSystem = ref GameMain.spaceSector.skillSystem;
            PrefabDesc pdesc = LDB.items.Select(3003).prefabDesc;
            int astroId = GameMain.localPlanet.astroId;
            ref LocalCannonade ptr = ref skillSystem.mechaLocalCannonades.Add();
            ptr.astroId = astroId;
            ptr.hitIndex = ((itemProto == null) ? 18 : itemProto.prefabDesc.AmmoHitIndex);
            ptr.minorHitIndex = 17;
            ptr.caster.type = ETargetType.Player;
            ptr.caster.id = 1;
            ptr.targetPos = targetLPos;
            ptr.distance = (ptr.targetPos - GameMain.mainPlayer.mecha.skillCastLeftL).magnitude;
            ptr.speed = 8f;
            ptr.damage = 150000;
            ptr.blastRadius0 = ((itemProto == null) ? SkillSystem.localCannonadeBlastRadius0 : itemProto.prefabDesc.AmmoBlastRadius0) * 1.2f;
            ptr.blastRadius1 = ((itemProto == null) ? SkillSystem.localCannonadeBlastRadius1 : itemProto.prefabDesc.AmmoBlastRadius1) * 1.2f;
            ptr.blastFalloff = ((itemProto == null) ? SkillSystem.localCannonadeBlastFalloff : itemProto.prefabDesc.AmmoBlastFalloff) * 1.2f;
            ptr.mask = (ETargetTypeMask.Vegetable | ETargetTypeMask.Enemy);
            ptr.life = 3;
            skillSystem.audio.AddPlayerAudio(314, 2.1f, GameMain.mainPlayer.uPosition);
        }








        /// <summary>
        /// 跟随身边的轰炸或者完全随机轰炸全星球
        /// </summary>
        /// <param name="followMecha">是跟随机甲轰炸还是轰炸全星球</param>
        /// <param name="minRange">轰炸机甲周围的范围的最小半径系数</param>
        /// <param name="maxRange">轰炸机甲周围的范围的最大半径系数，轰炸中心将在二者之间</param>
        public static void GuideBomb(bool followMecha, float minRange = 3, float maxRange = 80)
        {
            if (GameMain.localPlanet == null)
            {
                if (isGuideBombing)
                    StopGuideBombing();
                isPurgeBombing = false;
                isGuideBombing = false;
                return;
            }
            Vector3 toCenterDir = GameMain.mainPlayer.uPosition - GameMain.localPlanet.uPosition;
            Vector3 horizontalDir = MoreMegaStructure.Utils.GetVertical(toCenterDir);
            horizontalDir = (Quaternion.AngleAxis((float)Utils.RandDouble() * 360, toCenterDir) * horizontalDir).normalized;

            //if (toCenterDir.magnitude >= GameMain.galaxy.astrosData[GameMain.localPlanet.astroId].uRadius + 10)
            //{
            //    distance = 20 + (float)Utils.RandDouble() * 40;
            //}
            //else
            float planetRadius = GameMain.galaxy.astrosData[GameMain.localPlanet.astroId].uRadius;


            Vector3 targetLPos;
            if (followMecha) // 轰炸机甲周围
            {
                float width = maxRange - minRange;
                float distance = minRange + (float)Utils.RandDouble() * width * (float)(GameMain.mainPlayer.uPosition - GameMain.localPlanet.uPosition).magnitude / planetRadius;
                VectorLF3 uEnd = horizontalDir * distance;
                Vector3 targetUPos = (uEnd + GameMain.mainPlayer.uPosition - GameMain.localPlanet.uPosition).normalized * GameMain.galaxy.astrosData[GameMain.localPlanet.astroId].uRadius + GameMain.localPlanet.uPosition;


                targetLPos = Quaternion.Inverse(GameMain.galaxy.astrosData[GameMain.localPlanet.astroId].uRot) * (targetUPos - (Vector3)GameMain.localPlanet.uPosition);
            }
            else // 全行星随机轰炸
            {
                targetLPos = Utils.RandPosDelta().normalized * planetRadius;
            }

            ItemProto itemProto = LDB.items.Select(bombItemId);
            ref SkillSystem skillSystem = ref GameMain.spaceSector.skillSystem;
            PrefabDesc pdesc = LDB.items.Select(3003).prefabDesc;
            int astroId = GameMain.localPlanet.astroId;
            ref LocalCannonade ptr = ref skillSystem.mechaLocalCannonades.Add();
            ptr.astroId = astroId;
            ptr.hitIndex = ((itemProto == null) ? 18 : itemProto.prefabDesc.AmmoHitIndex);
            ptr.minorHitIndex = 17;
            ptr.caster.type = ETargetType.Player;
            ptr.caster.id = 1;
            ptr.targetPos = targetLPos;
            ptr.distance = (ptr.targetPos - GameMain.mainPlayer.mecha.skillCastLeftL).magnitude;
            ptr.speed = 8f;
            ptr.damage = 150000;
            ptr.blastRadius0 = ((itemProto == null) ? SkillSystem.localCannonadeBlastRadius0 : itemProto.prefabDesc.AmmoBlastRadius0);
            ptr.blastRadius1 = ((itemProto == null) ? SkillSystem.localCannonadeBlastRadius1 : itemProto.prefabDesc.AmmoBlastRadius1);
            ptr.blastFalloff = ((itemProto == null) ? SkillSystem.localCannonadeBlastFalloff : itemProto.prefabDesc.AmmoBlastFalloff);
            ptr.mask = (ETargetTypeMask.Vegetable | ETargetTypeMask.Enemy);
            ptr.life = 3;
            skillSystem.audio.AddPlayerAudio(314, 2.1f, GameMain.mainPlayer.uPosition);
        }

        public static void FireworkTest(bool followMecha = true, float minRange = 0, float maxRange = 30)
        {
            if (GameMain.localPlanet == null)
            {
                if (isGuideBombing)
                    StopGuideBombing();
                isPurgeBombing = false;
                isGuideBombing = false;
                return;
            }
            if (!DspBattlePlugin.newYearFirework.Value)
            {
                return;
            }

            if (UIRoot.instance.uiMainMenu.active || UIRoot.instance.galaxySelect.active || UIRoot.instance.optionWindow.active || UIRoot.instance.saveGameWindow.active)
            {
                return;
            }
            Vector3 toCenterDir = GameMain.mainPlayer.uPosition - GameMain.localPlanet.uPosition;
            Vector3 horizontalDir = MoreMegaStructure.Utils.GetVertical(toCenterDir);
            horizontalDir = (Quaternion.AngleAxis((float)Utils.RandDouble() * 360, toCenterDir) * horizontalDir).normalized;

            //if (toCenterDir.magnitude >= GameMain.galaxy.astrosData[GameMain.localPlanet.astroId].uRadius + 10)
            //{
            //    distance = 20 + (float)Utils.RandDouble() * 40;
            //}
            //else
            float planetRadius = GameMain.galaxy.astrosData[GameMain.localPlanet.astroId].uRadius;


            Vector3 targetLPos;
            if (true) // 轰炸机甲周围
            {
                float width = maxRange - minRange;
                float distance = minRange + (float)Utils.RandDouble() * width * (float)(GameMain.mainPlayer.uPosition - GameMain.localPlanet.uPosition).magnitude / planetRadius;
                VectorLF3 uEnd = horizontalDir * distance;
                Vector3 targetUPos = (uEnd + GameMain.mainPlayer.uPosition - GameMain.localPlanet.uPosition).normalized * (GameMain.galaxy.astrosData[GameMain.localPlanet.astroId].uRadius + Utils.RandInt(15,40) )+ GameMain.localPlanet.uPosition;


                targetLPos = Quaternion.Inverse(GameMain.galaxy.astrosData[GameMain.localPlanet.astroId].uRot) * (targetUPos - (Vector3)GameMain.localPlanet.uPosition);
            }
            //else // 全行星随机轰炸
            //{
                //targetLPos = Utils.RandPosDelta().normalized * planetRadius;
            //}

            ItemProto itemProto = LDB.items.Select(bombItemId);
            ref SkillSystem skillSystem = ref GameMain.spaceSector.skillSystem;
            PrefabDesc pdesc = LDB.items.Select(3003).prefabDesc;
            int astroId = GameMain.localPlanet.astroId;
            ref LocalCannonade ptr = ref skillSystem.mechaLocalCannonades.Add();
            ptr.astroId = astroId;
            ptr.hitIndex = ((itemProto == null) ? 18 : itemProto.prefabDesc.AmmoHitIndex);
            ptr.minorHitIndex = 17;
            ptr.caster.type = ETargetType.Player;
            ptr.caster.id = 1;
            ptr.targetPos = targetLPos;
            ptr.distance = (ptr.targetPos - GameMain.mainPlayer.mecha.skillCastLeftL).magnitude;
            ptr.speed = 8f;
            ptr.damage = 150000;
            ptr.blastRadius0 = ((itemProto == null) ? SkillSystem.localCannonadeBlastRadius0 : itemProto.prefabDesc.AmmoBlastRadius0);
            ptr.blastRadius1 = ((itemProto == null) ? SkillSystem.localCannonadeBlastRadius1 : itemProto.prefabDesc.AmmoBlastRadius1);
            ptr.blastFalloff = ((itemProto == null) ? SkillSystem.localCannonadeBlastFalloff : itemProto.prefabDesc.AmmoBlastFalloff);
            ptr.mask = (ETargetTypeMask.Vegetable | ETargetTypeMask.Enemy);
            ptr.life = 3;
            skillSystem.audio.AddPlayerAudio(314, 2.1f, GameMain.mainPlayer.uPosition);

            if (Utils.RandDouble() < 0.1)
                UIRealtimeTip.Popup("新年快乐".Translate(), Utils.RandPosDelta() * 520, false);
        }

        /// <summary>
        /// 跟随自身的投掷爆炸晶石的bomb，弃用
        /// </summary>
        public static void BombTest()
        {
            if (GameMain.localPlanet == null)
            {
                isPurgeBombing = false;
                return;
            }
            ItemProto itemProto = LDB.items.Select(1130);
            //if(GameMain.localPlanet != null)
            //{
            //    uVel += (GameMain.spaceSector.astros[GameMain.localPlanet.id].uPosNext) - (GameMain.spaceSector.astros[GameMain.localPlanet.id].uPos) * 60;
            //}
            VectorLF3 castPos = GameMain.mainPlayer.mecha.skillBombingUCenter;
            ref Bomb_Explosive ptr3 = ref GameMain.spaceSector.skillSystem.explosiveUnitBombs.Add();
            ptr3.nearStarId = (GameMain.localStar != null) ? GameMain.localStar.id : 0;
            ptr3.uPos = castPos;// - GameMain.mainPlayer.uVelocity * 0.016666667;
            ptr3.uRot = GameMain.mainPlayer.uRotation;

            Vector3 toCenterDir = GameMain.mainPlayer.uPosition - GameMain.localPlanet.uPosition;
            Vector3 horizontalDir = MoreMegaStructure.Utils.GetVertical(toCenterDir);
            horizontalDir = (Quaternion.AngleAxis((float)Utils.RandDouble() * 360, toCenterDir) * horizontalDir).normalized;

            float velocity;
            if (toCenterDir.magnitude >= GameMain.galaxy.astrosData[GameMain.localPlanet.astroId].uRadius + 10)
            {
                velocity = 20 + (float)Utils.RandDouble() * 40;
            }
            else
            {
                velocity = 40 + (float)Utils.RandDouble() * 80;
            }

            VectorLF3 uVel = horizontalDir * velocity;
            ptr3.uVel = uVel + GameMain.mainPlayer.uVelocity;
            long time = GameMain.instance.timei;
            int num2 = (time > 2147483647L) ? ((int)(time % 2147483647L)) : ((int)time);
            ptr3.uAgl = RandomTable.SphericNormal(ref num2, 0.1);
            ptr3.life = 3600;
            ptr3.mask = (ETargetTypeMask.Vegetable | ETargetTypeMask.Enemy);
            ptr3.abilityValue = (int)((float)itemProto.Ability * GameMain.data.history.blastDamageScale + 0.5f);
            ptr3.caster.id = 1;
            ptr3.caster.type = ETargetType.Player;
            ptr3.protoId = 1129; // 用的是爆破单元的动画而非晶石
            ptr3.ApplyConfigs();
        }


        public static void LaserSweepTest()
        {
            SpaceSector sector = GameMain.spaceSector;
            SkillSystem skillSystem = sector?.skillSystem;
            if (skillSystem == null) return;
            Player player = GameMain.mainPlayer;
            StarData star = GameMain.localStar;
            if (star == null) return;
            PlanetFactory factory = GameMain.localPlanet?.factory;
            if(factory == null) return;
            int num = 0;
            float num2 = float.MaxValue;
            VectorLF3 playerSkillTargetU = skillSystem.playerSkillTargetU;
            AstroData[] galaxyAstros = sector.galaxyAstros;
            if (factory != null)
            {
                num = factory.planet.astroId;
                ref SpaceLaserSweep ptr = ref skillSystem.spaceLaserSweeps.Add();
                ptr.astroId = num;
                ptr.hitIndex = 13;
                float d = galaxyAstros[num].uRadius - 0.5f;
                Vector3 a = skillSystem.playerSkillTargetL.normalized * d;
                Vector3 normalized = (a.normalized + player.forward * 0.5f).normalized;
                a = UnityEngine.Random.insideUnitSphere.normalized * galaxyAstros[num].uRadius * 2;
                ptr.beginPos = a;// + normalized * 2000f;
                ptr.damage = 100000;
                ptr.life = 150;
                ptr.lifemax = 150;
                ptr.damageInterval = 3;
                ptr.mask = (ETargetTypeMask.Vegetable | ETargetTypeMask.Enemy);
                ptr.caster.type = ETargetType.Astro;
                ptr.caster.id = 1;
                ptr.caster.astroId = star.astroId;
                ptr.sweepFrom = (a + UnityEngine.Random.insideUnitSphere * 150f).normalized * d;
                ptr.sweepTo = (ptr.sweepFrom + (a - ptr.sweepFrom).normalized * 150f).normalized * d;
                return;
            }

            // 下面是在localPlanet的factory为null时，从星系里找行星进行激光扫射。
            //for (int i = star.astroId + 1; i <= star.astroId + 8; i++)
            //{
            //    AstroData astroData = galaxyAstros[i];
            //    float uRadius = astroData.uRadius;
            //    if (uRadius >= 1f && uRadius <= 600f)
            //    {
            //        VectorLF3 vectorLF;
            //        vectorLF.x = astroData.uPos.x - playerSkillTargetU.x;
            //        vectorLF.y = astroData.uPos.y - playerSkillTargetU.y;
            //        vectorLF.z = astroData.uPos.z - playerSkillTargetU.z;
            //        float num3 = (float)(vectorLF.x * vectorLF.x + vectorLF.y * vectorLF.y + vectorLF.z * vectorLF.z);
            //        if (num3 < num2)
            //        {
            //            num = i;
            //            num2 = num3;
            //        }
            //    }
            //}
            //if (num2 < 9000000f)
            //{
            //    ref SpaceLaserSweep ptr2 = ref skillSystem.spaceLaserSweeps.Add();
            //    ptr2.astroId = num;
            //    float d2 = galaxyAstros[num].uRadius - 0.5f;
            //    ptr2.hitIndex = 13;
            //    sector.InverseTransformToAstro_ref(num, ref playerSkillTargetU, out playerSkillTargetU);
            //    ptr2.beginPos = playerSkillTargetU;
            //    ptr2.damage = 1;
            //    ptr2.life = 150;
            //    ptr2.lifemax = 150;
            //    ptr2.damageInterval = 3;
            //    ptr2.mask = ETargetTypeMask.NotPlayer;
            //    ptr2.caster.type = ETargetType.Player;
            //    ptr2.caster.id = 1;
            //    ptr2.caster.astroId = star.astroId;
            //    Vector3 a2 = ptr2.beginPos.normalized * d2;
            //    ptr2.sweepFrom = (a2 + UnityEngine.Random.insideUnitSphere * 100f).normalized * d2;
            //    ptr2.sweepTo = (ptr2.sweepFrom + (a2 - ptr2.sweepFrom).normalized * 100f).normalized * d2;
            //}
        }




        [HarmonyPostfix]
        [HarmonyPatch(typeof(SkillSystem), "GetObjectUPose")]
        public static void GetObjectUPosePostPatch(ref SkillSystem __instance, ref SkillTarget obj, ref VectorLF3 upos, ref Quaternion urot, ref bool __result)
        {
            if(obj.type == ETargetType.Astro)
            {
                upos = GameMain.galaxy.astrosData[obj.astroId].uPos;
                urot = GameMain.galaxy.astrosData[obj.astroId].uRot;
                __result = true;
            }
        }


        /// <summary>
        /// 废弃测试
        /// </summary>
        //public static void CreateSpaceCraft()
        //{
        //    if (GameMain.localStar == null)
        //        return;
        //    short modelIndex;
        //    bool dynamic;
        //    bool isSpace;
        //    bool isInvincible;
        //    FleetProto fleetProto = LDB.fleets.Select(moduleFleetProtoId);
        //    if (fleetProto == null)
        //    {
        //        return;
        //    }
        //    VectorLF3 vectorLF;
        //    int starAstroId = GameMain.localStar.astroId;
        //    int astroId = GameMain.localPlanet != null ? GameMain.localPlanet.astroId : starAstroId;
        //    Quaternion quaternion;
        //    GameMain.spaceSector.InverseTransformToAstro_ref(starAstroId, ref GameMain.mainPlayer.uPosition, ref GameMain.mainPlayer.uRotation, out vectorLF, out quaternion);
        //    float d = FleetComponent.CalculateFleetPoseScale(GameMain.spaceSector, ref GameMain.mainPlayer.uPosition);
        //    vectorLF = (VectorLF3)(quaternion * (GameMain.mainPlayer.mecha.spaceCombatModule.moduleFleetPoses[0].position * d)) + vectorLF;

        //    modelIndex = (short)fleetProto.ModelIndex;
        //    dynamic = true;
        //    isSpace = fleetProto.IsSpace;
        //    isInvincible = true;
        //    CraftData craftData = default(CraftData);
        //    craftData.protoId = (short)moduleFleetProtoId;
        //    craftData.modelIndex = modelIndex;
        //    craftData.astroId = astroId;
        //    craftData.owner = -1;
        //    craftData.port = (short)0;
        //    craftData.prototype = ECraftProto.Fleet;
        //    craftData.dynamic = dynamic;
        //    craftData.isSpace = isSpace;
        //    craftData.pos = vectorLF;
        //    craftData.rot = quaternion;
        //    craftData.vel = GameMain.mainPlayer.uVelocity;
        //    craftData.isInvincible = isInvincible;
        //    GameMain.spaceSector.AddCraftDataWithComponents(ref craftData);
        //}


        public static void Export(BinaryWriter w)
        {
            w.Write(isPurgeBombing ? 1 : 0);
            w.Write(purgeBombingTime);
            w.Write(purgeCoolDown);
            w.Write(isGuideBombing ? 1 : 0);
            w.Write(guideBombingTime);
            w.Write(guideCoolDown);
        }

        public static void Import(BinaryReader r)
        {
            InitWhenLoad();
            UIPlanetBombing.InitAll();
            if(Configs.versionWhenImporting >= 30241126)
            {
                isPurgeBombing = r.ReadInt32() > 0;
                purgeBombingTime = r.ReadInt32();
                purgeCoolDown = r.ReadInt32();
                isGuideBombing = r.ReadInt32() > 0;
                guideBombingTime = r.ReadInt32();
                guideCoolDown = r.ReadInt32();
            }
        }

        public static void IntoOtherSave()
        {
            InitWhenLoad();
            UIPlanetBombing.InitAll();
        }

    }
}
