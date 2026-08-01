using System;
using System.Collections.Generic;
using System.Linq;
using RimWorld;
using Verse;
using Verse.AI;

namespace ModernRClickMenu
{
    // ═══════════════════════════════════════════════════
    // 单条操作项 —— 悬浮窗子菜单的一行
    // ═══════════════════════════════════════════════════
    public class ItemActionEntry
    {
        public string label;

        public Action action;

        public bool disabled;

        public Thing targetThing;
    }

    // ═══════════════════════════════════════════════════
    // 一个物品分组 —— 同一种 ThingDef 的物品归为一组
    // ═══════════════════════════════════════════════════
    public class StoredItemGroup
    {
        public string headerLabel;          // 组标题文本（物品名或"其他"）

        public Thing representativeThing;   // 代表物品（"其他"组为 null，不画图标）

        public int totalCount;              // 物品总数（"其他"组为 0）

        public List<ItemActionEntry> actions = new List<ItemActionEntry>();

        /// <summary>
        /// 将容器内物品按 ThingDef 聚合为分组，并为每个分组生成操作项。
        /// 操作项尽可能复用原版 Provider 生成的选项（含原版全部检查与动作逻辑），
        /// 避免重复实现穿戴判定、负重判定等复杂规则。
        /// </summary>
        public static List<StoredItemGroup> BuildGroups(List<FloatMenuOption> options, List<Thing> clickedItems, FloatMenuContext context)
        {
            var groups = new List<StoredItemGroup>();
            var groupByDef = new Dictionary<ThingDef, StoredItemGroup>();
            var otherGroup = new StoredItemGroup { headerLabel = "其他" };

            // 1) 从原版选项按 iconThing 建立分组（覆盖地面物品堆等原版已生成选项的场景）
            foreach (FloatMenuOption option in options)
            {
                Thing iconThing = option.iconThing;
                if (iconThing != null && iconThing.def.category == ThingCategory.Item)
                {
                    GetOrCreateGroup(groupByDef, groups, iconThing.def, iconThing).actions.Add(ConvertOption(option, iconThing));
                }
                else
                {
                    otherGroup.actions.Add(ConvertOption(option, iconThing));
                }
            }

            // 2) 对收集到的物品：若原版未为其生成任何选项（如储物容器内的物品），
            //    建立分组并手动生成基础操作（穿戴 / 拾取 / 搬运）
            foreach (Thing item in clickedItems)
            {
                if (!groupByDef.ContainsKey(item.def))
                {
                    StoredItemGroup group = GetOrCreateGroup(groupByDef, groups, item.def, item);
                    CollectBasicActions(group, context);
                }
            }

            // 3) 从右键命中的物品统计每组总数量
            foreach (StoredItemGroup group in groups)
            {
                group.totalCount = clickedItems
                    .Where(thing => thing.def == group.representativeThing.def)
                    .Sum(thing => thing.stackCount);
            }

            // 物品组按名称排序，"其他"组置于末尾
            groups.Sort((a, b) => string.Compare(a.headerLabel, b.headerLabel, StringComparison.Ordinal));
            if (otherGroup.actions.Count > 0)
            {
                groups.Add(otherGroup);
            }
            return groups;
        }

        /// <summary>按 ThingDef 获取或创建物品分组。</summary>
        private static StoredItemGroup GetOrCreateGroup(
            Dictionary<ThingDef, StoredItemGroup> groupByDef,
            List<StoredItemGroup> groups,
            ThingDef def,
            Thing representativeThing)
        {
            if (!groupByDef.TryGetValue(def, out StoredItemGroup group))
            {
                group = new StoredItemGroup
                {
                    headerLabel = def.label,
                    representativeThing = representativeThing
                };
                groupByDef[def] = group;
                groups.Add(group);
            }
            return group;
        }

        /// <summary>
        /// 手动生成物品的基础操作（容器内物品原版不生成选项，需要补全）：
        /// 衣物穿戴 / 拾取 / 搬运到储物区。
        /// </summary>
        private static void CollectBasicActions(StoredItemGroup group, FloatMenuContext context)
        {
            Thing thing = group.representativeThing;
            Pawn pawn = context.FirstSelectedPawn;
            if (thing == null || pawn == null)
            {
                return;
            }

            // 衣物类操作（复用原版 Provider，含全部穿戴检查）
            if (thing is Apparel)
            {
                var wearProvider = new FloatMenuOptionProvider_Wear();
                foreach (FloatMenuOption option in wearProvider.GetOptionsFor(thing, context))
                {
                    group.actions.Add(ConvertOption(option, thing));
                }
            }

            // 拾取类操作
            var pickUpProvider = new FloatMenuOptionProvider_PickUpItem();
            foreach (FloatMenuOption option in pickUpProvider.GetOptionsFor(thing, context))
            {
                group.actions.Add(ConvertOption(option, thing));
            }

            // 搬运到储物区（自建）
            if (thing.Spawned)
            {
                group.actions.Add(new ItemActionEntry
                {
                    // TODO: 国际化 —— 后续改为 Keyed 翻译键
                    label = "搬运到储物区",
                    action = delegate
                    {
                        thing.SetForbidden(false, warnOnFail: false);
                        Job job = HaulAIUtility.HaulToStorageJob(pawn, thing, forced: true);
                        if (job != null)
                        {
                            job.playerForced = true;
                            pawn.jobs.TryTakeOrderedJob(job, JobTag.Misc);
                        }
                    },
                    targetThing = thing
                });
            }
        }

        /// <summary>
        /// 将原版 FloatMenuOption 转换为通用操作项。
        /// action 为 null 时视为不可执行（原版不可执行的选项 label 已带原因说明）。
        /// </summary>
        private static ItemActionEntry ConvertOption(FloatMenuOption option, Thing thing)
        {
            return new ItemActionEntry
            {
                label = option.Label,
                action = option.action,
                disabled = option.action == null,
                targetThing = thing
            };
        }
    }
}
