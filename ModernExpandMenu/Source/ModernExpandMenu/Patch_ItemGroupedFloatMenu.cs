using System.Collections.Generic;
using System.Linq;
using HarmonyLib;
using ModernExpandMenu.UI;
using RimWorld;
using Verse;

namespace ModernExpandMenu
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
        // 接管日志只输出一次（避免 DevMode 下每次右键刷屏）
        private static bool loggedTakeover;

        private static void Postfix(
            List<Pawn> selectedPawns,
            FloatMenuContext context,
            ref List<FloatMenuOption> __result)
        {
            // 设置中"完全关闭模组"时直接放行原版菜单
            if (!ModernExpandMenuMod.Settings.modEnabled)
            {
                return;
            }

            // 仅处理单选小人 + 存在右键上下文
            if (context == null || context.IsMultiselect || selectedPawns.NullOrEmpty())
            {
                return;
            }
            if (context.FirstSelectedPawn == null)
            {
                return;
            }

            // 右键目标包含殖民者（Pawn）时不接管：
            // 直接右键殖民者头像/殖民者本身 = 原版针对 Pawn 的菜单（选择、交谈、约会等），不是物品分组场景
            if (context.ClickedThings.Any(thing => thing is Pawn))
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

            // 按 iconThing 把原版选项分组；容器内物品放入待生成列表，由窗口分帧补全操作
            List<StoredItemGroup> groups = StoredItemGroup.BuildGroups(__result, clickedItems);
            if (groups.Count == 0)
            {
                return;
            }

            // 抑制原版菜单，弹出 MD3 悬浮窗（传入右键的容器用于高亮显示，不改变选中）
            __result.Clear();
            if (Find.WindowStack.Windows.Any(window => window is MD3FloatMenuWindow))
            {
                return;
            }
            Building_Storage clickedStorage = FindClickedStorage(context);
            // 传入右键命中的物品实例：窗口持续高亮这些物品（原版右键目标白框效果）
            Find.WindowStack.Add(new MD3FloatMenuWindow(groups, context, clickedStorage, clickedItems));

            // 静默：仅首次在开发者模式下输出极简日志，避免每次右键刷屏
            if (Prefs.DevMode && !loggedTakeover)
            {
                loggedTakeover = true;
                Log.Message($"[ModernExpandMenu] 接管：{groups.Count} 组 / {distinctDefCount} 类");
            }
        }

        /// <summary>查找右键命中的储物容器（用于高亮显示，不改变选中状态）。</summary>
        private static Building_Storage FindClickedStorage(FloatMenuContext context)
        {
            foreach (Thing thing in context.ClickedThings)
            {
                if (thing is Building_Storage storage && storage.Spawned)
                {
                    return storage;
                }
            }
            return null;
        }

        /// <summary>
        /// 收集右键目标物品：直接命中的物品 + 命中的储物容器所有占地格上的物品。
        /// 这样右键容器任意一格时，菜单会包含容器全部占地格中的物品。
        /// </summary>
        private static List<Thing> CollectClickedItems(FloatMenuContext context)
        {
            // 用 HashSet 去重：右键命中的物品可能与容器占地格收集的物品重复，
            // 否则会导致数量翻倍、操作项重复
            var items = new List<Thing>();
            var seen = new HashSet<Thing>();
            foreach (Thing thing in context.ClickedThings)
            {
                if (thing.def.category == ThingCategory.Item)
                {
                    if (seen.Add(thing))
                    {
                        items.Add(thing);
                    }
                }
                else if (thing is Building_Storage storage && storage.Spawned)
                {
                    // 若容器属于容器组（StorageGroup），收集组内所有容器的物品；
                    // 否则只收集该容器所有占地格上的物品
                    if (storage.storageGroup != null)
                    {
                        foreach (Thing heldThing in storage.storageGroup.HeldThings)
                        {
                            if (heldThing.def.category == ThingCategory.Item && seen.Add(heldThing))
                            {
                                items.Add(heldThing);
                            }
                        }
                    }
                    else
                    {
                        foreach (IntVec3 cell in storage.AllSlotCells())
                        {
                            foreach (Thing cellThing in storage.Map.thingGrid.ThingsListAt(cell))
                            {
                                if (cellThing.def.category == ThingCategory.Item && seen.Add(cellThing))
                                {
                                    items.Add(cellThing);
                                }
                            }
                        }
                    }
                }
            }
            return items;
        }
    }
}
