using HarmonyLib;
using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using UnityEngine;
using xiaoye97;
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
        public static int purgeMaxCD = 2700;
        public static int guideMaxCD = 7200;

        public static int moduleFleetProtoId = 5;

        public static float minMechaEnergyCapCoeff = 0.01f; // 初始，每秒消耗机甲最大能量的百分比
        public static float growMechaEnergyCapCoeff = 0.04f; // 成长到最大时，每秒（除了初始之外）额外消耗机甲最大能量的百分比。最终最大值是此值+minMechaEnergyCapCoeff
        public static int energyCapCoeffGrowTime = 3600; // 需要多久，消耗机甲最大能量的百分比值能成长到最大值

        public static int guideBasicEnergyComsumption = 50000; // 初始太阳轰炸每帧耗能
        public static float b = 5000f;
        public static float a = 500f; // ax^2 + bx + guideBasicEnergyComsumption 为引导太阳轰炸的非百分比耗能部分，x为已连续引导太阳轰炸秒数（而非帧数）

        public static bool purgeReady { get { return purgeCoolDown <= 0 && !isPurgeBombing; } }
        public static bool guideReady { get { return guideCoolDown <= 0; } }

        public static void InitWhenLoad()
        {
            isPurgeBombing = false;
            isGuideBombing = false;
            purgeBombingTime = 0;
            guideBombingTime = 0;
            purgeCoolDown = 0;
            guideCoolDown = 0;
        }


        [HarmonyPostfix]
        [HarmonyPatch(typeof(GameData), "GameTick")]
        public static void Update(long time)
        {
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
                    float bombingPhase = purgeBombingTime * 1.0f / maxPurgeBombingTime;
                    float timeToEquator = Mathf.Abs(bombingPhase - 0.5f);
                    bombFactor = (int)((0.5f - timeToEquator) * 12) + 4;
                    for (int i = 0; i < bombFactor; i++)
                    {
                        PurgeBombCircular();
                    }
                    for (int i = 0; i < 3; i++)
                        GuideBomb(false); // 同时还会稀疏地轰炸全星球
                }
                else // 处在倒计时阶段
                {
                    if(purgeBombingTime == - 60)
                        UIRealtimeTip.Popup("<color=#ff2020>1</color>".Translate());
                    else if (purgeBombingTime == -120)
                        UIRealtimeTip.Popup("<color=#ff2020>2</color>".Translate());
                    else if (purgeBombingTime == -180)
                        UIRealtimeTip.Popup("<color=#ff2020>3</color>".Translate());
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
                int timeTillMax1 = guideBombingTime > 3600 ? guideBombingTime : 3600; 
                float maxEnergyCapCoefficient = (0.01f + (0.04f * timeTillMax1/ 3600)) / 60f; // 每秒消耗1%最大能量，逐渐增加直至1分钟时每秒消耗5%最大能量
                energyConsumption += (long)(maxEnergyCapCoefficient * GameMain.mainPlayer.mecha.coreEnergyCap);
                // 然后消耗随时间增加的固定能量
                float tSecond = 1.0f * guideBombingTime / 60;
                energyConsumption += (long)(a * tSecond * tSecond + b * tSecond + guideBasicEnergyComsumption);

                GameMain.mainPlayer.mecha.coreEnergy -= energyConsumption;
                GameMain.mainPlayer.mecha.MarkEnergyChange(22, -energyConsumption);
                if(GameMain.mainPlayer.mecha.coreEnergy <= 0)
                {
                    GameMain.mainPlayer.mecha.coreEnergy = 0;
                    StopGuideBombing();
                }
            }

            if (purgeCoolDown > 0)
                purgeCoolDown--;
            if (guideCoolDown > 0)
                guideCoolDown--;
        }


        public static void LaunchPurgeBombing()
        {
            isPurgeBombing = true;
            purgeBombingTime = -240;
            purgeCoolDown = purgeMaxCD;
        }

        public static void LaunchGuideBombing()
        {
            if (!isGuideBombing)
            {
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
        public static void PurgeBombCircular()
        {
            if (GameMain.localPlanet == null)
            {
                isPurgeBombing = false;
                return;
            }
            Vector3 toCenterDir = GameMain.mainPlayer.uPosition - GameMain.localPlanet.uPosition;

            float planetRadius = GameMain.galaxy.astrosData[GameMain.localPlanet.astroId].uRadius;

            Vector3 targetLPos = Quaternion.AngleAxis(purgeBombingTime * 1.0f / maxPurgeBombingTime * 180 + ((float)Utils.RandDouble() - 0.5f) * 15, new Vector3(1, 0, 0)) * new Vector3(0, 1, 0);
            targetLPos = Quaternion.AngleAxis((float)Utils.RandDouble() * 360, new Vector3(0, 1, 0)) * targetLPos;
            //if(Utils.RandDouble() < 0.4f)
            //    targetLPos = targetLPos.normalized * (planetRadius + 15); // 有40概率往低空打
            //else
                targetLPos = targetLPos.normalized * planetRadius;


            ItemProto itemProto = LDB.items.Select(1606);
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

            ItemProto itemProto = LDB.items.Select(1606);
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
