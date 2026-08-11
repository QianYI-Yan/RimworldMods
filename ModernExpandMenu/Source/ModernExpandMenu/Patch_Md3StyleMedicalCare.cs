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
    // 医药设置选择器（MedicalCareSetter）MD3 化：
    //   - 应用位置：「默认医药设置」对话框（Dialog_MedicalDefaults）、
    //     健康 tab 的医药选择（ITab_Pawn_Visitor）等所有调用处
    //   - 5 个图标格（无医疗/无药/草药/工业/超凡）：MD3 圆角格，
    //     hover 主色底、选中主色描边
    //   - 保留原版交互：拖动连续涂色（medicalCarePainting）、tooltip、音效
    // 由设置 md3StyleMedicalCare 控制（默认关）。
    // ═══════════════════════════════════════════════════
    [HarmonyPatch(typeof(MedicalCareUtility), nameof(MedicalCareUtility.MedicalCareSetter))]
    public static class Patch_MedicalCareSetter
    {
        // 私有字段反射缓存
        private static FieldInfo fCareTextures;
        private static FieldInfo fPainting;

        private static void EnsureFields()
        {
            if (fCareTextures != null)
            {
                return;
            }
            fCareTextures = AccessTools.Field(typeof(MedicalCareUtility), "careTextures");
            fPainting = AccessTools.Field(typeof(MedicalCareUtility), "medicalCarePainting");
        }

        public static bool Prefix(Rect rect, ref MedicalCareCategory medCare)
        {
            if (!ModernExpandMenuMod.Settings.md3StyleMedicalCare)
            {
                return true;   // 未开启：放行原版绘制
            }
            EnsureFields();

            Texture2D[] careTextures = (Texture2D[])fCareTextures.GetValue(null);
            bool painting = (bool)(fPainting.GetValue(null) ?? false);
            float slotWidth = rect.width / 5f;
            for (int i = 0; i < 5; i++)
            {
                MedicalCareCategory mc = (MedicalCareCategory)i;
                var slotRect = new Rect(rect.x + slotWidth * i, rect.y, slotWidth, rect.height);
                bool hover = Mouse.IsOver(slotRect);
                bool selected = medCare == mc;

                // MD3 圆角格：选中主色半透明底 + 主色描边；hover 主色底
                if (selected)
                {
                    MD3Widgets.DrawRoundedRect(slotRect, MD3Theme.HoverStateLayer, 6f);
                    MD3Widgets.DrawRoundedRectOutline(slotRect, MD3Theme.Primary, 6f, 2f, MD3Theme.HoverStateLayer);
                }
                else if (hover)
                {
                    MD3Widgets.DrawRoundedRect(slotRect, MD3Theme.HoverStateLayer, 6f);
                }

                // 图标（原版医疗图标纹理；careTextures 可能尚未加载完成）
                if (careTextures != null && i < careTextures.Length && careTextures[i] != null)
                {
                    GUI.color = Color.white;
                    Widgets.DrawTextureFitted(slotRect.ContractedBy(4f), careTextures[i], 1f);
                    GUI.color = Color.white;
                }

                // tooltip
                if (hover)
                {
                    TooltipHandler.TipRegion(slotRect, () => mc.GetLabel().CapitalizeFirst(), 632165 + i * 17);
                }

                // 交互（与原版一致：点击选中 / 拖动连续涂色）
                MouseoverSounds.DoRegion(slotRect);
                Widgets.DraggableResult draggableResult = Widgets.ButtonInvisibleDraggable(slotRect);
                if (draggableResult == Widgets.DraggableResult.Dragged)
                {
                    painting = true;
                }
                // 与原版 AnyPressed 扩展等价（该扩展为 internal，模组不可直接调用）
                bool anyPressed = draggableResult == Widgets.DraggableResult.Pressed || draggableResult == Widgets.DraggableResult.DraggedThenPressed;
                if ((painting && Mouse.IsOver(slotRect) && medCare != mc) || anyPressed)
                {
                    medCare = mc;
                    SoundDefOf.Tick_High.PlayOneShotOnCamera();
                }
            }
            if (!Input.GetMouseButton(0))
            {
                painting = false;
            }
            fPainting.SetValue(null, painting);
            return false;   // 完全接管
        }
    }
}
