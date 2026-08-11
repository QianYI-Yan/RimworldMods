using System;
using System.Reflection;
using HarmonyLib;
using ModernExpandMenu.Theme;
using ModernExpandMenu.UI;
using RimWorld;
using UnityEngine;
using Verse;
using Verse.Sound;

namespace ModernExpandMenu
{
    // ═══════════════════════════════════════════════════
    // 原版展开菜单（FloatMenu / FloatMenuGrid）全部 MD3 化接管：
    //   - FloatMenu（列表式）：如「选择语言」下拉、殖民者菜单、
    //     Mod 设置里的下拉选择、命令浮出菜单等
    //       菜单背景 → MD3 卡片（圆角 + 描边）
    //       每行     → MD3 圆角行（hover 主色高亮）
    //   - FloatMenuGrid（网格式）：如 Ideo 图标 / 发色 / 建筑风格等
    //       背景 MD3 卡片 + 每格 MD3 圆角
    // 由设置 md3StyleFloatMenus 控制（默认开：全部接管）。
    // 说明：本模组自绘的右键分组菜单（MD3FloatMenuWindow）不走原版
    // FloatMenuOption，因此不受此 patch 影响。
    // ═══════════════════════════════════════════════════

    /// <summary>展开菜单接管范围判断（设置 floatMenuTakeoverScope 控制）。</summary>
    internal static class FloatMenuTakeoverHelper
    {
        public static bool ShouldTakeover(bool includeGrid = false)
        {
            ModernExpandMenuSettings.FloatMenuTakeoverScope scope = ModernExpandMenuMod.Settings.floatMenuTakeoverScope;
            if (scope == ModernExpandMenuSettings.FloatMenuTakeoverScope.Off)
            {
                return false;
            }
            if (scope == ModernExpandMenuSettings.FloatMenuTakeoverScope.All)
            {
                return true;
            }
            // DialogDropdowns：仅接管从对话框弹出的选择菜单（窗口栈中存在类名以 Dialog 开头的窗口）
            if (includeGrid)
            {
                return false;   // 网格菜单仅在 All 时接管
            }
            foreach (Window window in Find.WindowStack.Windows)
            {
                if (window is FloatMenu || window is FloatMenuGrid)
                {
                    continue;
                }
                // 原版无统一 Dialog_ 基类（对话框直接继承 Window），按类名约定判断
                if (window.GetType().Name.StartsWith("Dialog", StringComparison.Ordinal))
                {
                    return true;
                }
            }
            return false;
        }
    }

    /// <summary>原版 FloatMenuOption 每行绘制 MD3 化（完全接管，保留原版交互逻辑）。</summary>
    [HarmonyPatch(typeof(FloatMenuOption), nameof(FloatMenuOption.DoGUI))]
    public static class Patch_FloatMenuOptionDoGUI
    {
        // 私有字段反射缓存（一次性读取）
        private static FieldInfo fIconTex;
        private static FieldInfo fShownItem;
        private static FieldInfo fDrawPlaceHolderIcon;
        private static FieldInfo fThingStyle;
        private static FieldInfo fForceBasicStyle;
        private static FieldInfo fGraphicIndexOverride;
        private static FieldInfo fIconJustification;
        private static FieldInfo fSizeMode;

        private static void EnsureFields()
        {
            if (fIconTex != null)
            {
                return;
            }
            Type t = typeof(FloatMenuOption);
            fIconTex = AccessTools.Field(t, "iconTex");
            fShownItem = AccessTools.Field(t, "shownItem");
            fDrawPlaceHolderIcon = AccessTools.Field(t, "drawPlaceHolderIcon");
            fThingStyle = AccessTools.Field(t, "thingStyle");
            fForceBasicStyle = AccessTools.Field(t, "forceBasicStyle");
            fGraphicIndexOverride = AccessTools.Field(t, "graphicIndexOverride");
            fIconJustification = AccessTools.Field(t, "iconJustification");
            fSizeMode = AccessTools.Field(t, "sizeMode");
        }

        public static bool Prefix(FloatMenuOption __instance, Rect rect, bool colonistOrdering, FloatMenu floatMenu, ref bool __result)
        {
            if (!FloatMenuTakeoverHelper.ShouldTakeover())
            {
                return true;   // 未开启 / 范围不覆盖：放行原版绘制
            }
            EnsureFields();
            // 行与卡片边框保持间隔（对齐展开菜单卡片化观感）
            rect = rect.ContractedBy(2f);

            bool disabled = __instance.Disabled;
            string label = __instance.Label;
            Rect clickRect = rect;
            clickRect.height--;
            bool hover = !disabled && Mouse.IsOver(clickRect);

            FloatMenuSizeMode sizeMode = (FloatMenuSizeMode)(fSizeMode.GetValue(__instance) ?? FloatMenuSizeMode.Normal);
            Text.Font = sizeMode != FloatMenuSizeMode.Normal ? GameFont.Tiny : GameFont.Small;

            if (__instance.tooltip.HasValue)
            {
                TooltipHandler.TipRegion(rect, __instance.tooltip.Value);
            }

            // ── 布局（与原版一致：图标区 / 文本区 / extraPart 区）──
            float curIconSize = sizeMode != FloatMenuSizeMode.Tiny ? 27f : 16f;
            Texture2D iconTex = (Texture2D)fIconTex.GetValue(__instance);
            ThingDef shownItem = (ThingDef)fShownItem.GetValue(__instance);
            bool drawPlaceHolderIcon = (bool)(fDrawPlaceHolderIcon.GetValue(__instance) ?? false);
            bool hasIcon = shownItem != null || drawPlaceHolderIcon || (bool)iconTex || __instance.iconThing != null;
            float iconOffset = hasIcon ? curIconSize : 0f;
            float horizontalMargin = sizeMode != FloatMenuSizeMode.Normal ? 3f : 6f;
            HorizontalJustification iconJustification = (HorizontalJustification)(fIconJustification.GetValue(__instance) ?? HorizontalJustification.Left);

            Rect iconRect = rect;
            if (iconJustification == HorizontalJustification.Left)
            {
                iconRect.xMin += 4f;
                iconRect.xMax = rect.x + curIconSize;
                iconRect.yMin += 4f;
                iconRect.yMax = rect.y + curIconSize;
                if (hover)
                {
                    iconRect.x += 4f;
                }
            }

            Rect textRect = rect;
            textRect.xMin += horizontalMargin;
            textRect.xMax -= horizontalMargin;
            textRect.xMax -= 4f;
            textRect.xMax -= __instance.extraPartWidth + iconOffset;
            if (iconJustification == HorizontalJustification.Left)
            {
                textRect.x += iconOffset;
            }
            if (hover)
            {
                textRect.x += 4f;
            }

            float num = Mathf.Min(Text.CalcSize(label).x, textRect.width - 4f);
            float num2 = textRect.xMin + num;
            if (iconJustification == HorizontalJustification.Right)
            {
                iconRect.x = num2 + 4f;
                iconRect.width = curIconSize;
                iconRect.yMin += 4f;
                iconRect.yMax = rect.y + curIconSize;
                num2 += curIconSize;
            }

            bool extraHover = false;
            Rect extraRect = default;
            if (__instance.extraPartWidth != 0f)
            {
                if (__instance.extraPartRightJustified)
                {
                    num2 = rect.xMax - __instance.extraPartWidth;
                }
                extraRect = new Rect(num2, textRect.yMin, __instance.extraPartWidth, 30f);
                extraHover = Mouse.IsOver(extraRect);
            }

            // ── MD3 绘制（与右键菜单 MD3FloatMenuWindow 操作项同款：左侧主色竖条常驻 + hover 圆角）──
            // 左侧主色竖条常驻（右键菜单操作项同款点缀；disabled 行灰化不显示）
            if (!disabled)
            {
                MD3Widgets.DrawRoundedRect(new Rect(rect.x + 1f, rect.y + 4f, 3f, rect.height - 8f), MD3Theme.Primary, 1.5f);
            }
            if (hover && !extraHover)
            {
                MD3Widgets.DrawRoundedRect(rect, MD3Theme.HoverStateLayer, 8f);
            }

            // 图标（原版渲染方式）
            if (shownItem != null || drawPlaceHolderIcon)
            {
                ThingStyleDef thingStyleDef = (ThingStyleDef)fThingStyle.GetValue(__instance);
                if (shownItem == null || Find.World == null)
                {
                    thingStyleDef = null;
                }
                if ((bool)(fForceBasicStyle.GetValue(__instance) ?? false))
                {
                    thingStyleDef = null;
                }
                Color iconValue = Color.white;
                if (shownItem != null)
                {
                    iconValue = shownItem.MadeFromStuff ? shownItem.GetColorForStuff(GenStuff.DefaultStuffFor(shownItem)) : shownItem.uiIconColor;
                }
                Widgets.DefIcon(iconRect, shownItem, null, 1f, thingStyleDef, drawPlaceHolderIcon, iconValue, null, (int?)fGraphicIndexOverride.GetValue(__instance));
            }
            else if ((bool)iconTex)
            {
                Widgets.DrawTextureFitted(iconRect, iconTex, 1f, new Vector2(1f, 1f), __instance.iconTexCoords);
            }
            else if (__instance.iconThing != null)
            {
                Widgets.ThingIcon(iconRect, __instance.iconThing, 1f);
            }

            // 文本（MD3 颜色）
            Text.Anchor = TextAnchor.MiddleLeft;
            GUI.color = disabled ? MD3Theme.DisabledText : MD3Theme.OnSurface;
            Widgets.Label(textRect, label);
            Text.Anchor = TextAnchor.UpperLeft;
            GUI.color = Color.white;

            // extraPart 附加控件
            if (__instance.extraPartOnGUI != null)
            {
                if (__instance.extraPartOnGUI(extraRect))
                {
                    __result = true;
                    return false;
                }
            }

            // hover 附加绘制
            if (hover && __instance.mouseoverGuiAction != null)
            {
                __instance.mouseoverGuiAction(rect);
            }

            // 教程高亮
            if (__instance.tutorTag != null)
            {
                UIHighlighter.HighlightOpportunity(rect, __instance.tutorTag);
            }

            // 点击执行
            if (Widgets.ButtonInvisible(clickRect))
            {
                if (__instance.tutorTag != null && !TutorSystem.AllowAction(__instance.tutorTag))
                {
                    __result = false;
                    return false;
                }
                __instance.Chosen(colonistOrdering, floatMenu);
                if (__instance.tutorTag != null)
                {
                    TutorSystem.Notify_Event(__instance.tutorTag);
                }
                __result = true;
                return false;
            }

            __result = false;
            return false;
        }
    }

    /// <summary>原版 FloatMenu（列表式菜单）背景 MD3 卡片化。</summary>
    [HarmonyPatch(typeof(FloatMenu), nameof(FloatMenu.DoWindowContents))]
    public static class Patch_FloatMenuDoWindowContents
    {
        public static void Prefix(FloatMenu __instance, Rect rect)
        {
            if (!FloatMenuTakeoverHelper.ShouldTakeover())
            {
                return;
            }
            // 整窗 MD3 卡片：表面 + 圆角（内容行随后覆盖其上，间隙透出卡片底）；
            // 边框样式三选：普通描边 / 主色跑马灯 / 彩色流光（与右键菜单 MD3FloatMenuWindow 完全一致）
            MD3Widgets.DrawRoundedRect(rect, MD3Theme.Surface, 10f);
            switch (ModernExpandMenuMod.Settings.menuBorderStyle)
            {
                case ModernExpandMenuSettings.MenuBorderStyle.Marquee:
                    MD3Widgets.DrawMarqueeBorder(rect, 10f, Time.realtimeSinceStartup * 0.6f, 2f);
                    break;
                case ModernExpandMenuSettings.MenuBorderStyle.Rainbow:
                    MD3Widgets.DrawCyberGradientBorder(rect, 10f, Time.realtimeSinceStartup / 3f);
                    break;
                default:
                    MD3Widgets.DrawRoundedRectOutline(rect, MD3Theme.Outline, 10f, 2f, MD3Theme.Surface);
                    break;
            }
        }
    }

    /// <summary>原版 FloatMenuGridOption（网格式菜单项）MD3 化。</summary>
    [HarmonyPatch(typeof(FloatMenuGridOption), nameof(FloatMenuGridOption.OnGUI))]
    public static class Patch_FloatMenuGridOptionOnGUI
    {
        public static bool Prefix(FloatMenuGridOption __instance, Rect rect, ref bool __result)
        {
            if (!FloatMenuTakeoverHelper.ShouldTakeover(includeGrid: true))
            {
                return true;
            }

            bool disabled = __instance.Disabled;
            bool hover = !disabled && Mouse.IsOver(rect);
            if (!disabled)
            {
                MouseoverSounds.DoRegion(rect);
            }
            if (__instance.tooltip.HasValue)
            {
                TooltipHandler.TipRegion(rect, __instance.tooltip.Value);
            }

            // MD3 圆角格：hover 主色半透明 / 普通表面高
            Color background = hover ? MD3Theme.HoverStateLayer : MD3Theme.SurfaceContainerHigh;
            MD3Widgets.DrawRoundedRect(rect, background, 8f);

            Rect iconRect = rect.ContractedBy(2f);
            if (!hover)
            {
                iconRect = iconRect.ContractedBy(2f);
            }
            Material material = disabled ? TexUI.GrayscaleGUI : null;
            Widgets.DrawTextureFitted(iconRect, __instance.texture, 1f, new Vector2(1f, 1f), __instance.iconTexCoords, 0f, material);

            __instance.postDrawAction?.Invoke(iconRect);

            if (Widgets.ButtonInvisible(rect))
            {
                __instance.Chosen();
                __result = true;
                return false;
            }
            __result = false;
            return false;
        }
    }

    /// <summary>原版 FloatMenuGrid（网格式菜单）背景 MD3 卡片化。</summary>
    [HarmonyPatch(typeof(FloatMenuGrid), nameof(FloatMenuGrid.DoWindowContents))]
    public static class Patch_FloatMenuGridDoWindowContents
    {
        public static void Prefix(FloatMenuGrid __instance, Rect rect)
        {
            if (!FloatMenuTakeoverHelper.ShouldTakeover(includeGrid: true))
            {
                return;
            }
            MD3Widgets.DrawRoundedRect(rect, MD3Theme.Surface, 10f);
            switch (ModernExpandMenuMod.Settings.menuBorderStyle)
            {
                case ModernExpandMenuSettings.MenuBorderStyle.Marquee:
                    MD3Widgets.DrawMarqueeBorder(rect, 10f, Time.realtimeSinceStartup * 0.6f, 2f);
                    break;
                case ModernExpandMenuSettings.MenuBorderStyle.Rainbow:
                    MD3Widgets.DrawCyberGradientBorder(rect, 10f, Time.realtimeSinceStartup / 3f);
                    break;
                default:
                    MD3Widgets.DrawRoundedRectOutline(rect, MD3Theme.Outline, 10f, 2f, MD3Theme.Surface);
                    break;
            }
        }
    }
}
