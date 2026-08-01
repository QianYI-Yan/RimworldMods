using System.Collections.Generic;
using System.Linq;
using RimWorld;
using UnityEngine;
using Verse;

namespace AstrylsUIZhCN
{
    /// <summary>
    /// 汉化模组设置入口。
    ///
    /// 设计：原版「选项 → Mod 设置」列表中的 16 个 astryl UI 模组条目
    /// 会被 Patcher 隐藏，本模组在列表中只显示一个「astryl UI 模组合集」条目，
    /// 点进来在这里分开打开各 UI 模组的设置界面（Dialog_ModSettings）。
    /// </summary>
    public class AstrylsUIZhCNMod : Mod
    {
        /// <summary>聚合的 16 个 UI 模组 packageId → 中文显示名。</summary>
        public static readonly Dictionary<string, string> AggregatedModNames = new Dictionary<string, string>
        {
            { "astryl.ModernPawnTabs", "现代角色标签页" },
            { "astryl.ModernBioTab", "现代生物标签页" },
            { "astryl.ModernXenotypeTab", "现代异种人标签页" },
            { "astryl.ModernSocialTab", "现代社交标签页" },
            { "astryl.ModernQuestMenu", "现代任务菜单" },
            { "astryl.ModernHistoryMenu", "现代历史菜单" },
            { "astryl.ModernFactionMenu", "现代派系菜单" },
            { "astryl.ModernIdeologyMenu", "现代意识形态菜单" },
            { "astryl.ModernNeedsTab", "现代需求标签页" },
            { "astryl.ModernLearningMenu", "现代学习菜单" },
            { "astryl.ModernNotifications", "现代通知" },
            { "astryl.ModernCC", "现代角色编辑器" },
            { "astryl.ModernColonistBar", "现代殖民者栏" },
            { "astryl.ModernDevTools", "现代开发者工具" },
            { "astryl.PillarPlanner", "屋顶支撑规划" },
            { "astryl.Circinus", "Circinus 性能分析" },
        };

        public AstrylsUIZhCNMod(ModContentPack content) : base(content)
        {
        }

        public override string SettingsCategory() => "astryl UI 模组合集";

        public override void DoSettingsWindowContents(Rect inRect)
        {
            Text.Font = GameFont.Medium;
            Widgets.Label(new Rect(0f, 0f, inRect.width, 30f), "astryl UI 模组合集 — 选择要打开设置的模组：");
            Text.Font = GameFont.Small;

            var listing = new Listing_Standard();
            listing.Begin(new Rect(0f, 35f, inRect.width, inRect.height - 35f));

            bool anyInstalled = false;
            foreach (var mod in LoadedModManager.ModHandles)
            {
                if (mod.Content == null)
                {
                    continue;
                }
                string pid = mod.Content.PackageId;
                if (string.IsNullOrEmpty(pid) || !AggregatedModNames.TryGetValue(pid, out string displayName))
                {
                    continue;
                }
                anyInstalled = true;
                if (listing.ButtonText(displayName + "  ·  " + mod.Content.Name))
                {
                    Find.WindowStack.Add(new Dialog_ModSettings(mod));
                }
                listing.Gap(2f);
            }

            if (!anyInstalled)
            {
                listing.Label("未检测到已安装的 astryl UI 模组。");
            }
            listing.End();
        }

        /// <summary>判断 packageId 是否属于被聚合的 16 个 UI 模组。</summary>
        public static bool IsAggregated(string packageId)
        {
            return !string.IsNullOrEmpty(packageId) && AggregatedModNames.ContainsKey(packageId);
        }
    }
}
