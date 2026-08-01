using System.Reflection;
using HarmonyLib;
using RimWorld;
using Verse;

namespace ModernRClickMenu
{
    // ═══════════════════════════════════════════════════
    // 模组入口 —— 安装 Harmony 补丁
    // 通过 PatchAll 自动注册本程序集内所有 HarmonyPatch
    // ═══════════════════════════════════════════════════
    public class ModernRClickMenuMod : Mod
    {
        // Harmony 唯一标识，与 About.xml 的 packageId 一致
        public const string HarmonyId = "yintx.deepseek.modernRclickmenu";

        public ModernRClickMenuMod(ModContentPack content) : base(content)
        {
            var harmony = new Harmony(HarmonyId);
            harmony.PatchAll(Assembly.GetExecutingAssembly());
        }
    }
}
