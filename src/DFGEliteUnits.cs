using HarmonyLib;
using System;
using System.Collections.Concurrent;
using System.Collections.Generic;
using System.Linq;
using System.Reflection.Emit;
using System.Text;
using System.Threading.Tasks;
using UnityEngine;

namespace DSP_Battle
{
    // enemyProto 8128 强袭者 modelProtoId 300
    // enemyProto 8129 游骑兵 modelProtoId 301
    // enemyProto 8130 守卫者 modelProtoId 302
    public class DFGEliteUnits
    {
        public const bool enabled = false;
        public static ConcurrentDictionary<int, int> eliteEnemyCombatStats = new ConcurrentDictionary<int, int>(); // 所有eliteEnemyCombatStat记录在此

        // 激活单位时，有概率将其变为精英单位
        [HarmonyPostfix]
        [HarmonyPatch(typeof(EnemyDFGroundSystem), "ActivateUnit")]
        public static void ActivateEliteByProb(ref EnemyDFGroundSystem __instance, int __result, int baseId, int formId, int portId, long gameTick)
        {
            if(__result != 0) // 成功激活了单位
            {
                int unitId = __result;
                DFGBaseComponent dfgbaseComponent = __instance.bases.buffer[baseId];
                if (dfgbaseComponent == null || dfgbaseComponent.id != baseId)
                {
                    return;
                }
                int enemyId = __instance.units.buffer[unitId].enemyId;
                ref EnemyData enemyData = ref __instance.factory.enemyPool[enemyId];
                int level = dfgbaseComponent.evolve.level;
                if(enemyData.combatStatId > 0)
                {
                    CombatStat[] buffer = GameMain.spaceSector.skillSystem.combatStats.buffer;
                    int combatStatId = enemyData.combatStatId;
                    buffer[combatStatId].hpMax = (SkillSystem.HpMaxByModelIndex[(int)enemyData.modelIndex] + level * SkillSystem.HpUpgradeByModelIndex[(int)enemyData.modelIndex]) * 100;
                    buffer[combatStatId].hp = buffer[combatStatId].hpMax - 10000;
                    buffer[combatStatId].hpRecover = 1;
                    buffer[combatStatId].size = SkillSystem.BarWidthByModelIndex[(int)enemyData.modelIndex]; // 控制血条长度
                    eliteEnemyCombatStats.AddOrUpdate(combatStatId, 1, (x, y) => 1);
                }
                else
                {
                    ref CombatStat combatStat = ref GameMain.spaceSector.skillSystem.combatStats.Add();
                    combatStat.hpMax = (SkillSystem.HpMaxByModelIndex[(int)enemyData.modelIndex] + level * SkillSystem.HpUpgradeByModelIndex[(int)enemyData.modelIndex]) * 100;
                    combatStat.hp = combatStat.hpMax - 100;
                    combatStat.hpRecover = 0;
                    combatStat.astroId = (combatStat.originAstroId = __instance.factory.planetId);
                    combatStat.objectType = (int)ETargetType.Enemy;
                    combatStat.objectId = enemyData.id;
                    combatStat.dynamic = (enemyData.dynamic ? 1 : 0);
                    combatStat.localPos = (enemyData.dynamic ? enemyData.pos : (enemyData.pos + enemyData.pos.normalized * (double)SkillSystem.BarHeightByModelIndex[(int)enemyData.modelIndex]));
                    combatStat.size = SkillSystem.BarWidthByModelIndex[(int)enemyData.modelIndex] * 5;
                    combatStat.lastCaster.type = ETargetType.Enemy;
                    combatStat.lastCaster.id = 0;
                    combatStat.lastCaster.astroId = __instance.factory.planetId;
                    enemyData.combatStatId = combatStat.id;
                    eliteEnemyCombatStats.AddOrUpdate(combatStat.id, 1, (x, y) => 1);
                }
            }
        }


        [HarmonyPostfix]
        [HarmonyPatch(typeof(GameData), "GameTick")]
        public static void HandleIdleEnemyCombatStat()
        {
            foreach (var combatStatId in eliteEnemyCombatStats)
            {
                int id = combatStatId.Key;
                int value = combatStatId.Value;
                ref CombatStat[] buffer = ref GameMain.spaceSector.skillSystem.combatStats.buffer;
                if(id > 0 && buffer.Length > id)
                {
                    int enemyId = buffer[id].objectId;
                    int planetId = buffer[id].astroId;
                    PlanetFactory factory = GameMain.galaxy.PlanetById(planetId)?.factory;
                    if(factory != null)
                    {
                        EnemyData enemyData = factory.enemyPool[enemyId];
                        int unitId = enemyData.unitId;
                        if (factory.enemySystem.units.buffer[unitId].behavior == EEnemyBehavior.KeepForm)
                        {
                            buffer[id].hpRecover = SkillSystem.HpRecoverByModelIndex[(int)enemyData.modelIndex] * 100;
                            eliteEnemyCombatStats.TryRemove(id, out _);
                        }
                    }
                    else
                    {
                        eliteEnemyCombatStats.TryRemove(id, out _);
                    }
                }
                else
                {
                    eliteEnemyCombatStats.TryRemove(id, out _);
                }
            }
        }


        //[HarmonyPostfix]
        //[HarmonyPatch(typeof(EnemyDFGroundSystem), "ActivateUnit")]
        //public static void DFGAU(int formId)
        //{
        //    Debug.Log($"form id is {formId}, protoID is {formId + 8128}");
        //}


        [HarmonyPrefix]
        [HarmonyPatch(typeof(PlanetFactory), "CreateEnemyFinal", new Type[] { typeof(int), typeof(int), typeof(int), typeof(int) })]
        public static bool CreateGroundEnemyFinalPrePatchTest(ref int protoId)
        {
            if (protoId == 8128)
                protoId = 9128;
            return true;
        }


        [HarmonyPrefix]
        [HarmonyPatch(typeof(EnemyDFGroundSystem), "DeactivateUnit")]
        public static void DeactivateUnitPrefix(ref EnemyDFGroundSystem __instance, int unitId)
        {
            int enemyId = __instance.units.buffer[unitId].enemyId;
            if (enemyId == 0)
            {
                return;
            }
            ref EnemyData ptr = ref __instance.factory.enemyPool[enemyId];
            if (ptr.id != 0 && ptr.id == enemyId)
            {
                if (ptr.protoId == 9128)
                    ptr.id = 8128;
            }
        }

        // 不加这个，激活之后显示不出来，加了也不会动
        [HarmonyPostfix]
        [HarmonyPatch(typeof(EnemyData), "Formation", new Type[] { typeof(int), typeof(EnemyData), typeof(float), typeof(VectorLF3), typeof(Quaternion), typeof(Vector3) }, new ArgumentType[] { ArgumentType.Normal, ArgumentType.Ref, ArgumentType.Normal, ArgumentType.Ref, ArgumentType.Ref, ArgumentType.Ref})]
        public static void EnemyDataFormationPostfix(ref EnemyData __instance, int _formTicks, ref EnemyData baseEnemy, float planetRadius, ref VectorLF3 _pos, ref Quaternion _rot, ref Vector3 _vel)
        {
            int num = (int)(__instance.port - 1);
            switch (__instance.protoId)
            {
                case 9128:
                    {
                        int num2 = num / 3;
                        int num3 = num % 3;
                        int num4 = num2 / 6;
                        num2 %= 6;
                        num3 += num4 * 3;
                        float num5 = (float)((num2 % 2 == 0) ? -1 : 1);
                        float num6 = (float)((_formTicks + (int)(__instance.owner * 127)) % 1080) / 1080f;
                        float num7 = num6 * 3.1415927f * 2f * num5;
                        float num8 = num7 + (float)num2 * 3.1415927f / 3f + (float)num3 * 0.06f;
                        float num9 = num8 + 0.02f * num5;
                        float num10 = 1.5f + planetRadius;
                        float num11 = (num2 % 2 == 0) ? (14.5f - 1.5f * Mathf.Cos(3f * num8)) : (20.3f + 2.1f * Mathf.Pow(Mathf.Abs(Mathf.Sin(1.5f * num8)), 30f) + 1.5f * Mathf.Cos(6f * num8));
                        float num12 = (num2 % 2 == 0) ? (14.5f - 1.5f * Mathf.Cos(3f * num9)) : (20.3f + 2.1f * Mathf.Pow(Mathf.Abs(Mathf.Sin(1.5f * num9)), 30f) + 1.5f * Mathf.Cos(6f * num9));
                        Vector3 vector = new Vector3(num11 * Mathf.Cos(num8), num10, num11 * Mathf.Sin(num8));
                        Vector3 normalized = new Vector3(num12 * Mathf.Cos(num9), num10, num12 * Mathf.Sin(num9));
                        Vector3 normalized2 = vector.normalized;
                        vector.x = normalized2.x * num10;
                        vector.y = normalized2.y * num10;
                        vector.z = normalized2.z * num10;
                        normalized = normalized.normalized;
                        normalized.x *= num10;
                        normalized.y *= num10;
                        normalized.z *= num10;
                        Vector3 vector2 = new Vector3((normalized.x - vector.x) * 17.453293f, (normalized.y - vector.y) * 17.453293f, (normalized.z - vector.z) * 17.453293f);
                        Vector3 normalized3 = vector2.normalized;
                        float f = Mathf.Clamp(num6 * 3.1415927f * 16f - (float)(num3 / 3 * 12), 0f, 6.2831855f) / 2f + (float)num3 * 0.1f * Mathf.Cos(num7);
                        float num13 = Mathf.Sin(f);
                        float w = Mathf.Cos(f);
                        Quaternion quaternion = new Quaternion(normalized3.x * num13, normalized3.y * num13, normalized3.z * num13, w);
                        Vector3 vector3;
                        EnemyData.RotateVector(ref normalized2, ref quaternion, out vector3);
                        Quaternion quaternion2;
                        __instance.LookRotation(ref normalized3, ref vector3, out quaternion2);
                        vector.y -= planetRadius;
                        vector.y += Mathf.Sin(num7 * 6f + (float)(num2 * 12 + num3) * 0.35f) * 0.4f;
                        EnemyData.RotateVector(ref vector, ref baseEnemy.rot, out _pos);
                        _pos += baseEnemy.pos;
                        EnemyData.RotateQuaternion(ref quaternion2, ref baseEnemy.rot, out _rot);
                        EnemyData.RotateVector(ref vector2, ref baseEnemy.rot, out _vel);
                        return;
                    }
            }
        }

        [HarmonyPrefix]
        [HarmonyPatch(typeof(EnemyDFGroundSystem), "NotifyUnitKilled")]
        public static bool Ntf(ref EnemyDFGroundSystem __instance, ref EnemyData enemy, DFGBaseComponent @base)
        {
            if (enemy.id != 0 && enemy.unitId != 0)
            {
                int port = (int)enemy.port;
                int num = (int)(enemy.protoId - 8128);
                if (enemy.protoId == 9128)
                    num = 0;
                EnemyFormation enemyFormation = @base.forms[num];
                Assert.True(enemyFormation.units[port] > 1);
                enemyFormation.RemoveUnit(port);
            }
            return false;
        }

        // 没有这个，敌人原地不动
        [HarmonyPostfix]
        [HarmonyPatch(typeof(EnemyUnitComponent), "RunBehavior_Engage_Ground")]
        public static void RBEG(ref EnemyUnitComponent __instance, PlanetFactory factory, ref EnemyData enemy)
        {
            if (__instance.hatred.max.target == 0)
            {
                __instance.RunBehavior_Engage_EmptyHatred(ref enemy);
                return;
            }
            switch (__instance.protoId)
            {
                case 9128:
                    __instance.RunBehavior_Engage_GRaider(factory, ref enemy);
                    return;
                default:
                    return;
            }
        }

        // 没有这个 会数组越界

        //[HarmonyPrefix]
        //[HarmonyPatch(typeof(SpaceSector), "InitPrefabDescArray")]
        //public static bool InitPrefabDescArrayPrefix()
        //{
        //    if (SpaceSector.PrefabDescByModelIndex == null)
        //    {
        //        ModelProto[] dataArray = LDB.models.dataArray;
        //        SpaceSector.PrefabDescByModelIndex = new PrefabDesc[dataArray.Length + 700];
        //        for (int i = 0; i < dataArray.Length; i++)
        //        {
        //            SpaceSector.PrefabDescByModelIndex[dataArray[i].ID] = dataArray[i].prefabDesc;
        //        }
        //    }
        //    return false;
        //}

        [HarmonyPatch(typeof(SpaceSector), nameof(SpaceSector.InitPrefabDescArray))]
        [HarmonyPatch(typeof(PlanetFactory), nameof(PlanetFactory.InitPrefabDescArray))]
        [HarmonyPatch(typeof(ModelProtoSet), nameof(ModelProtoSet.OnAfterDeserialize))]
        [HarmonyTranspiler]
        public static IEnumerable<CodeInstruction> ModelProtoSet_OnAfterDeserialize_Transpiler(IEnumerable<CodeInstruction> instructions)
        {
            var matcher = new CodeMatcher(instructions);

            matcher.MatchForward(false, new CodeMatch(OpCodes.Newarr));

            matcher.InsertAndAdvance(new CodeInstruction(OpCodes.Pop), new CodeInstruction(OpCodes.Ldc_I4, 1024));

            return matcher.InstructionEnumeration();
        }

        [HarmonyPatch(typeof(SkillSystem), MethodType.Constructor, typeof(SpaceSector))]
        [HarmonyTranspiler]
        public static IEnumerable<CodeInstruction> SkillSystem_Constructor_Transpiler(IEnumerable<CodeInstruction> instructions)
        {
            var matcher = new CodeMatcher(instructions);

            matcher.MatchForward(false, new CodeMatch(OpCodes.Newarr));

            do
            {
                matcher.InsertAndAdvance(new CodeInstruction(OpCodes.Pop), new CodeInstruction(OpCodes.Ldc_I4, 1024));
                matcher.Advance(1).MatchForward(false, new CodeMatch(OpCodes.Newarr));
            }
            while (matcher.IsValid);

            return matcher.InstructionEnumeration();
        }

        [HarmonyPatch(typeof(ModelProto), nameof(ModelProto.InitMaxModelIndex))]
        [HarmonyPostfix]
        public static void InitMaxModelIndex()
        {
            ModelProto.maxModelIndex = LDB.models.dataArray.Max(model => model?.ID).GetValueOrDefault();
        }

        // PlanetFactory.KillEnemyFinally还会报错，但是WreckageHandler wreckagePrefab = PlanetFactory.PrefabDescByModelIndex[(modelProto.RuinId != 0) ? modelProto.RuinId : modelProto.ID].wreckagePrefab;游戏里log了一下不是null啊？？！PlanetFactory.PrefabDescByModelIndex[770]不是null啊？！！
    }
}
