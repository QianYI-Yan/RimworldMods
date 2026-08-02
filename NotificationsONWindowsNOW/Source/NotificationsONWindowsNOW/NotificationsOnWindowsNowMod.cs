using System;
using HarmonyLib;
using UnityEngine;
using Verse;

namespace NotificationsOnWindowsNow
{
    /// <summary>
    /// 模组主类：负责记录全局状态、应用 Harmony 补丁，并提供设置页。
    /// </summary>
    public class NotificationsOnWindowsNowMod : Mod
    {
        /// <summary>当前模组实例，供全局访问。</summary>
        public static NotificationsOnWindowsNowMod Instance;

        /// <summary>模组设置实例。</summary>
        public static NotificationsOnWindowsNowSettings Settings;

        /// <summary>模组根目录（绝对路径），用于定位桥梁进程。</summary>
        public static string ModRootDirectory;

        public NotificationsOnWindowsNowMod(ModContentPack content) : base(content)
        {
            Instance = this;
            Settings = GetSettings<NotificationsOnWindowsNowSettings>();
            ModRootDirectory = content.RootDir;
            ApplyHarmonyPatches();
        }

        /// <summary>绘制设置页。</summary>
        public override void DoSettingsWindowContents(Rect inRect)
        {
            Listing_Standard listing = new Listing_Standard();
            listing.Begin(inRect);

            listing.CheckboxLabeled(
                "NOW.EnableDedup".Translate(),
                ref Settings.enableShortTimeMessageDedup,
                "NOW.EnableDedupDesc".Translate());
            listing.Gap(8f);

            // SliderLabeled 不显示当前值，把数值拼进标签（如「通知合并窗口（秒）: 2.0s」）。
            Settings.mergeWindowSeconds = listing.SliderLabeled(
                "NOW.MergeWindow".Translate() + ": " + Settings.mergeWindowSeconds.ToString("0.0") + "s",
                Settings.mergeWindowSeconds,
                0f, 10f,
                0.5f,
                "NOW.MergeWindowDesc".Translate());

            listing.End();
            base.DoSettingsWindowContents(inRect);
        }

        /// <summary>设置页在选项列表中的分类名。</summary>
        public override string SettingsCategory()
        {
            return "NOW.SettingsCategory".Translate();
        }

        /// <summary>应用本程序集内所有 Harmony 补丁。</summary>
        private static void ApplyHarmonyPatches()
        {
            try
            {
                var harmony = new Harmony("yintx.deepseek.NotificationsONWindowsNOW");
                harmony.PatchAll();
            }
            catch (Exception exception)
            {
                Log.Error("[NotificationsONWindowsNOW] Harmony 补丁应用失败: " + exception);
            }
        }
    }
}
