using System.Reflection;
using HarmonyLib;
using UnityEngine;
using Verse;

namespace ModernExpandMenu
{
    // ═══════════════════════════════════════════════════
    // 模组入口 —— 安装 Harmony 补丁
    // 通过 PatchAll 自动注册本程序集内所有 HarmonyPatch。
    // 设置界面已分离到 SettingsUI.cs（DoSettingsWindowContents 委托给 SettingsUI.DrawSettings）。
    // ═══════════════════════════════════════════════════
    public class ModernExpandMenuMod : Mod
    {
        // 全局设置实例（窗口 / 补丁各处读取）
        public static ModernExpandMenuSettings Settings;

        // 伪翻译开关的上次值（退出设置时检测变化并强制刷新 UI；由 Patch_ModSettingsUI 读写）
        public static bool lastPseudoTranslationSetting;

        // Harmony 唯一标识，与 About.xml 的 packageId 一致
        public const string HarmonyId = "yintx.deepseek.modernexpandmenu";

        public ModernExpandMenuMod(ModContentPack content) : base(content)
        {
            Settings = GetSettings<ModernExpandMenuSettings>();
            var harmony = new Harmony(HarmonyId);
            harmony.PatchAll(Assembly.GetExecutingAssembly());
        }

        /// <summary>游戏内"选项 → Mod 设置"界面（完整绘制逻辑在 SettingsUI.DrawSettings）。</summary>
        public override void DoSettingsWindowContents(Rect inRect)
        {
            SettingsUI.DrawSettings(inRect);
        }

        public override string SettingsCategory()
        {
            return "Modern Expand Menu";
        }
    }
}
