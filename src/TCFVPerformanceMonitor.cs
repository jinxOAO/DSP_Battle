using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace DSP_Battle
{
    public class TCFVPerformanceMonitor
    {
        public static int MainLogic;
        public static int Assault;
        public static int MetaDrive;
        //public static int Factory;
        public static int Damage;
        public static int Kill;
        public static int Ballistic;
        public static int Droplet;
        public static int EventSys;
        public static int DrawCall;

        //public static int Droplet1;
        //public static int Droplet2;
        //public static int Droplet3;

        public static void Awake()
        {
            // 使用 MoreMegaStructure 新版性能统计 API 注册本 mod 的各项 CPU 采样点。
            // RegisterEntry(localizationKey, level)：入参为本地化 key，性能面板中会经
            // 游戏 Translate() 显示，对应中英文翻译在 BattleProtos.AddTranslate 中注册。
            // level：0 顶层、1 一级、2 二级；父子关系由 level + 注册顺序自动推导，
            // 因此必须严格“父项在前、子项紧随其后”。返回的内部索引与 BeginSample/EndSample 配对使用。
            // 顶层：深空来敌各项逻辑合计。
            MainLogic = MoreMegaStructure.MMSCPU.RegisterEntry("PF深空来敌", 0);

            // 入侵逻辑。
            Assault = MoreMegaStructure.MMSCPU.RegisterEntry("PF入侵逻辑", 1);

            // 元驱动及其子项：伤害、击杀、弹道重写。
            MetaDrive = MoreMegaStructure.MMSCPU.RegisterEntry("PF元驱动", 1);
            ////Factory = MoreMegaStructure.MMSCPU.RegisterEntry("PF工厂重写", 2); // 工厂重写暂未启用
            Damage = MoreMegaStructure.MMSCPU.RegisterEntry("PF伤害逻辑", 2);
            Kill = MoreMegaStructure.MMSCPU.RegisterEntry("PF击杀逻辑", 2);
            Ballistic = MoreMegaStructure.MMSCPU.RegisterEntry("PF弹道重写", 2);

            // 其余一级子项。
            Droplet = MoreMegaStructure.MMSCPU.RegisterEntry("PF水滴", 1);
            EventSys = MoreMegaStructure.MMSCPU.RegisterEntry("PF事件链", 1);
            DrawCall = MoreMegaStructure.MMSCPU.RegisterEntry("PF深空绘制调用", 1);

        }
    }
}
