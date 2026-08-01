using System.Collections.Generic;
using System.Linq;
using HarmonyLib;
using ModernRClickMenu.UI;
using RimWorld;
using Verse;

namespace ModernRClickMenu
{
    // ═══════════════════════════════════════════════════
    // 拦截右键菜单生成流程：
    //   选中小人 + 右键一个堆放多种物品的位置（物品堆）时，
    //   把原版生成的选项按物品分组，弹出 MD3 风格悬浮窗。
    // 分组完全复用原版选项（iconThing 自动关联物品），不丢任何功能。
    // Hook 点 FloatMenuMakerMap.GetOptions —— Selector 在右键时调用，
    // 返回列表非空才创建原版 FloatMenuMap，因此清空即可抑制原版菜单。
    // ═══════════════════════════════════════════════════
    [HarmonyPatch(typeof(FloatMenuMakerMap), nameof(FloatMenuMakerMap.GetOptions))]
    public static class Patch_ItemGroupedFloatMenu
    {
        private static void Postfix(
            List<Pawn> selectedPawns,
            FloatMenuContext context,
            ref List<FloatMenuOption> __result)
        {
            // 仅处理单选小人 + 存在右键上下文
            if (context == null || context.IsMultiselect || selectedPawns.NullOrEmpty())
            {
                return;
            }
            if (context.FirstSelectedPawn == null)
            {
                return;
            }

            // 收集右键目标物品：直接命中的物品 + 命中的储物容器所有占地格上的物品
            List<Thing> clickedItems = CollectClickedItems(context);
            if (clickedItems.Count == 0)
            {
                return;
            }

            // 仅当同一位置堆放多种物品时才接管，单物品 / 单种类保持原版菜单
            int distinctDefCount = clickedItems.Select(thing => thing.def).Distinct().Count();
            if (distinctDefCount < 2)
            {
                return;
            }

            if (__result == null)
            {
                __result = new List<FloatMenuOption>();
            }

            // 按 iconThing 把原版选项分组；容器内物品原版不生成选项，会手动补全操作
            List<StoredItemGroup> groups = StoredItemGroup.BuildGroups(__result, clickedItems, context);
            if (groups.Count == 0)
            {
                return;
            }

            // 抑制原版菜单，弹出 MD3 悬浮窗
            __result.Clear();
            if (Find.WindowStack.Windows.Any(window => window is MD3FloatMenuWindow))
            {
                return;
            }
            Find.WindowStack.Add(new MD3FloatMenuWindow(groups));

            // 静默：仅在开发者模式下输出极简日志，避免每次右键刷屏
            if (Prefs.DevMode)
            {
                Log.Message($"[ModernRClickMenu] 接管：{groups.Count} 组 / {distinctDefCount} 类");
            }
        }

        /// <summary>
        /// 收集右键目标物品：直接命中的物品 + 命中的储物容器所有占地格上的物品。
        /// 这样右键容器任意一格时，菜单会包含容器全部占地格中的物品。
        /// </summary>
        private static List<Thing> CollectClickedItems(FloatMenuContext context)
        {
            var items = new List<Thing>();
            foreach (Thing thing in context.ClickedThings)
            {
                if (thing.def.category == ThingCategory.Item)
                {
                    items.Add(thing);
                }
                else if (thing is Building_Storage storage && storage.Spawned)
                {
                    // 容器：把它所有占地格上的物品都算进来
                    foreach (IntVec3 cell in storage.AllSlotCells())
                    {
                        foreach (Thing cellThing in storage.Map.thingGrid.ThingsListAt(cell))
                        {
                            if (cellThing.def.category == ThingCategory.Item)
                            {
                                items.Add(cellThing);
                            }
                        }
                    }
                }
            }
            return items;
        }
    }
}
