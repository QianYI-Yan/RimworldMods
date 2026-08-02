using System.Collections.Generic;
using System.Linq;
using AstrylsUIZhCN.Theme;
using AstrylsUIZhCN.UI;
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
        /// <summary>聚合的 16 个 UI 模组 packageId（RimWorld 内部存小写）→ 中文显示名。</summary>
        public static readonly Dictionary<string, string> AggregatedModNames = new Dictionary<string, string>
        {
            { "astryl.modernpawntabs", "现代角色标签页" },
            { "astryl.modernbiotab", "现代生物标签页" },
            { "astryl.modernxenotypetab", "现代异种人标签页" },
            { "astryl.modernsocialtab", "现代社交标签页" },
            { "astryl.modernquestmenu", "现代任务菜单" },
            { "astryl.modernhistorymenu", "现代历史菜单" },
            { "astryl.modernfactionmenu", "现代派系菜单" },
            { "astryl.modernideologymenu", "现代意识形态菜单" },
            { "astryl.modernneedstab", "现代需求标签页" },
            { "astryl.modernlearningmenu", "现代学习菜单" },
            { "astryl.modernnotifications", "现代通知" },
            { "astryl.moderncc", "现代角色编辑器" },
            { "astryl.moderncolonistbar", "现代殖民者栏" },
            { "astryl.moderndevtools", "现代开发者工具" },
            { "astryl.pillarplanner", "屋顶支撑规划" },
            { "astryl.circinus", "Circinus 性能分析" },
            { "astryl.truerpginventory", "True RPG 背包系统" },
            { "astryl.truerpgbackpacks", "True RPG 背包" },
        };

        /// <summary>聚合列表滚动位置。</summary>
        private Vector2 scrollPosition;

        public AstrylsUIZhCNMod(ModContentPack content) : base(content)
        {
        }

        public override string SettingsCategory() => "astryl UI 模组合集";

        public override void DoSettingsWindowContents(Rect inRect)
        {
            // MD3 卡片背景
            MD3Widgets.DrawCard(inRect, MD3Theme.Surface, MD3Theme.CardCornerRadius);

            // 标题
            GUI.color = MD3Theme.OnSurface;
            Text.Font = GameFont.Medium;
            Text.Anchor = TextAnchor.MiddleLeft;
            Text.WordWrap = false;
            Widgets.Label(new Rect(inRect.x + 20f, inRect.y + 12f, inRect.width - 40f, 30f), "astryl UI 模组合集");
            Text.Font = GameFont.Small;
            Text.Anchor = TextAnchor.UpperLeft;
            Text.WordWrap = true;
            GUI.color = Color.white;

            // 收集已安装的聚合模组（packageId 小写匹配）
            var installed = new List<(string displayName, Mod mod)>();
            foreach (var mod in LoadedModManager.ModHandles)
            {
                if (mod.Content == null)
                {
                    continue;
                }
                string pid = mod.Content.PackageId;
                if (string.IsNullOrEmpty(pid))
                {
                    continue;
                }
                if (!AggregatedModNames.TryGetValue(pid.ToLowerInvariant(), out string displayName))
                {
                    continue;
                }
                installed.Add((displayName, mod));
            }

            // 提示
            GUI.color = MD3Theme.OnSurfaceVariant;
            Text.Font = GameFont.Small;
            Text.Anchor = TextAnchor.UpperLeft;
            Text.WordWrap = false;
            Widgets.Label(new Rect(inRect.x + 20f, inRect.y + 42f, inRect.width - 40f, 22f),
                "点击卡片打开对应模组的设置；关闭设置后返回本聚合界面。");
            Text.Anchor = TextAnchor.UpperLeft;
            Text.WordWrap = true;
            GUI.color = Color.white;

            float listTop = inRect.y + 70f;
            float listHeight = inRect.height - 70f - 14f;
            if (installed.Count == 0)
            {
                // 空状态提示
                GUI.color = MD3Theme.OnSurfaceVariant;
                Text.Font = GameFont.Small;
                Text.Anchor = TextAnchor.MiddleCenter;
                Text.WordWrap = true;
                Widgets.Label(new Rect(inRect.x + 30f, listTop, inRect.width - 60f, listHeight), "未检测到已安装的 astryl UI 模组。");
                Text.Anchor = TextAnchor.UpperLeft;
                Text.WordWrap = true;
                GUI.color = Color.white;
                return;
            }

            // MD3 卡片块网格：每卡片一个模组，点击叠加打开设置（关闭后返回本聚合界面）
            // 3 列动态宽度：保证中文名（最长 8 字 Medium）不截断
            const float cardHeight = 80f;
            const float cardGapX = 14f;
            const float cardGapY = 14f;
            const int columns = 3;
            float cardWidth = (inRect.width - 32f - (columns - 1) * cardGapX) / columns;
            int rows = Mathf.CeilToInt(installed.Count / (float)columns);
            float contentHeight = rows * cardHeight + (rows - 1) * cardGapY;
            float contentWidth = columns * cardWidth + (columns - 1) * cardGapX;
            var contentRect = new Rect(0f, 0f, Mathf.Max(contentWidth, inRect.width - 48f), contentHeight);
            var scrollRect = new Rect(inRect.x + 16f, listTop, inRect.width - 32f, listHeight);
            MD3Widgets.MD3BeginScrollView(scrollRect, ref scrollPosition, contentRect);
            for (int i = 0; i < installed.Count; i++)
            {
                int col = i % columns;
                int row = i / columns;
                var cardRect = new Rect(col * (cardWidth + cardGapX), row * (cardHeight + cardGapY), cardWidth, cardHeight);
                if (DrawModCard(cardRect, installed[i]))
                {
                    // 叠加打开子设置；关闭后回到本聚合界面
                    Find.WindowStack.Add(new Dialog_ModSettings(installed[i].mod));
                }
            }
            MD3Widgets.MD3EndScrollView(scrollRect, ref scrollPosition, contentHeight, 3001, MD3Theme.CardCornerRadius);
        }

        /// <summary>
        /// 绘制单个模组卡片（MD3 卡片块）：中文名 + 英文原名，hover 高亮，点击返回 true。
        /// </summary>
        private static bool DrawModCard(Rect rect, (string displayName, Mod mod) item)
        {
            bool hover = Mouse.IsOver(rect);
            MD3Widgets.DrawCard(rect, hover ? MD3Theme.SurfaceContainerHigh : MD3Theme.SurfaceContainer, MD3Theme.CardCornerRadius);

            // 中文名
            GUI.color = MD3Theme.OnSurface;
            Text.Font = GameFont.Medium;
            Text.Anchor = TextAnchor.UpperLeft;
            Text.WordWrap = false;
            Widgets.Label(new Rect(rect.x + 12f, rect.y + 12f, rect.width - 24f, 24f), item.displayName);
            // 英文原名
            GUI.color = MD3Theme.OnSurfaceVariant;
            Text.Font = GameFont.Small;
            Text.WordWrap = false;
            Widgets.Label(new Rect(rect.x + 12f, rect.y + 40f, rect.width - 24f, 20f), item.mod.Content.Name);
            // 还原
            Text.Font = GameFont.Small;
            Text.Anchor = TextAnchor.UpperLeft;
            Text.WordWrap = true;
            GUI.color = Color.white;

            return Widgets.ButtonInvisible(rect);
        }

        /// <summary>判断 packageId 是否属于被聚合的 16 个 UI 模组（大小写不敏感）。</summary>
        public static bool IsAggregated(string packageId)
        {
            return !string.IsNullOrEmpty(packageId) && AggregatedModNames.ContainsKey(packageId.ToLowerInvariant());
        }
    }
}
