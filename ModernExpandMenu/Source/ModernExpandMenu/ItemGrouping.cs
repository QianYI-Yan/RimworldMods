using System;
using System.Collections.Generic;
using System.Linq;
using RimWorld;
using Verse;
using Verse.AI;

namespace ModernExpandMenu
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

        public bool isHaulAction;   // 是否为自建的"搬运到储物区"操作（用于同组多项目时标注物品名）

        public float appearTime = -1f;   // 左侧渐入动画的开始时间（realtimeSinceStartup）
    }

    // ═══════════════════════════════════════════════════
    // 一个物品分组 —— 同一种 ThingDef 的物品归为一组
    // ═══════════════════════════════════════════════════
    public class StoredItemGroup
    {
        public string headerLabel;          // 组标题文本（物品名或"其他"）

        public Thing representativeThing;   // 代表物品（"其他"组为 null，不画图标）

        public bool isOtherGroup;           // 是否为"其他"组（用于排序与识别，避免依赖文本比较）

        public int totalCount;              // 物品总数（"其他"组为 0）

        public List<ItemActionEntry> actions = new List<ItemActionEntry>();

        public List<Thing> pendingItems = new List<Thing>();   // 待分帧生成操作的物品实例

        /// <summary>
        /// 将容器内物品按 ThingDef 聚合为分组，并为每个分组生成操作项。
        /// 操作项尽可能复用原版 Provider 生成的选项（含原版全部检查与动作逻辑），
        /// 避免重复实现穿戴判定、负重判定等复杂规则。
        /// </summary>
        public static List<StoredItemGroup> BuildGroups(List<FloatMenuOption> options, List<Thing> clickedItems)
        {
            var groups = new List<StoredItemGroup>();
            var groupByDef = new Dictionary<ThingDef, StoredItemGroup>();
            var otherGroup = new StoredItemGroup { headerLabel = "ModernExpandMenu_OtherGroup".Translate(), isOtherGroup = true };

            // 1) 从原版选项按 iconThing 建立分组（覆盖地面物品堆等原版已生成选项的场景），
            //    并记录"已有原版选项的具体物品实例"
            var thingsWithVanillaOptions = new HashSet<Thing>();
            foreach (FloatMenuOption option in options)
            {
                Thing iconThing = option.iconThing;
                if (iconThing != null && iconThing.def.category == ThingCategory.Item)
                {
                    thingsWithVanillaOptions.Add(iconThing);
                    GetOrCreateGroup(groupByDef, groups, iconThing.def, iconThing).actions.Add(ConvertOption(option, iconThing));
                }
                else
                {
                    otherGroup.actions.Add(ConvertOption(option, iconThing));
                }
            }

            // 2) 对没有原版选项的每个物品实例（如储物容器内的物品），
            //    加入"待生成操作"列表，由悬浮窗在后续帧分帧生成，
            //    避免大量物品时一次性遍历所有 Provider 导致右键卡顿
            foreach (Thing item in clickedItems)
            {
                if (thingsWithVanillaOptions.Contains(item))
                {
                    continue;
                }
                StoredItemGroup group = GetOrCreateGroup(groupByDef, groups, item.def, item);
                group.pendingItems.Add(item);
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
        /// 为单个物品实例生成操作：遍历所有原版 FloatMenu Provider，模拟"右键该物品"，
        /// 得到完整操作集（食用/合并/装备/穿戴/剥光/拾取等）。
        /// 这样容器内所有物品都视为在右键格子上处理，操作与原版右键该物品一致。
        /// </summary>
        public static void CollectBasicActionsForItem(StoredItemGroup group, Thing thing, FloatMenuContext context)
        {
            Pawn pawn = context.FirstSelectedPawn;
            if (thing == null || pawn == null)
            {
                return;
            }

            // 遍历所有原版 Provider（静态缓存复用），单个失败不影响其他
            foreach (FloatMenuOptionProvider provider in CachedProviders)
            {
                try
                {
                    foreach (FloatMenuOption option in provider.GetOptionsFor(thing, context))
                    {
                        group.actions.Add(ConvertOption(option, thing));
                    }
                }
                catch (Exception ex)
                {
                    Log.Warning($"[ModernExpandMenu] Provider {provider.GetType().Name} 对 {thing.LabelShort} 生成选项失败：{ex.Message}");
                }
            }

            // 搬运到储物区（自建，原版没有针对容器内物品的直接搬运选项）
            if (thing.Spawned)
            {
                group.actions.Add(new ItemActionEntry
                {
                    label = "ModernExpandMenu_HaulToStorage".Translate(),
                    isHaulAction = true,
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

        // 排除昂贵且与容器物品无关（或与自建操作重复）的 Provider：
        // - WorkGivers：遍历全部工作类型生成选项，最昂贵，且其"搬运"与我们自建的重复
        // - FromThing / FromZone / FromLord：面向 Thing 泛型 / 存储区 / Lord，对容器物品无内容
        // 注意：必须声明在 CachedProviders 之前——C# 静态字段按声明顺序初始化，
        // 若在 BuildCachedProviders() 执行时本集合仍为 null，会抛 NullReferenceException
        private static readonly HashSet<Type> ExcludedProviderTypes = new HashSet<Type>
        {
            typeof(FloatMenuOptionProvider_WorkGivers),
            typeof(FloatMenuOptionProvider_FromThing),
            typeof(FloatMenuOptionProvider_FromZone),
            typeof(FloatMenuOptionProvider_FromLord),
            // 面向 Pawn / 车队的 Provider：对容器内的物品（Thing）调用会抛异常（刷日志），
            // 且对物品本身无意义，直接排除
            typeof(FloatMenuOptionProvider_CarryingPawn),
            typeof(FloatMenuOptionProvider_LoadCaravan)
        };

        // 静态缓存所有原版 FloatMenu Provider（一次性创建复用，避免每次右键反复实例化）
        private static readonly List<FloatMenuOptionProvider> CachedProviders = BuildCachedProviders();

        private static List<FloatMenuOptionProvider> BuildCachedProviders()
        {
            var list = new List<FloatMenuOptionProvider>();
            foreach (Type type in typeof(FloatMenuOptionProvider).AllSubclassesNonAbstract())
            {
                if (ExcludedProviderTypes.Contains(type))
                {
                    continue;
                }
                // 单个 Provider 实例化失败不影响其他类型（防御：某些 DLC Provider 在特定环境下构造可能抛异常）
                try
                {
                    list.Add((FloatMenuOptionProvider)Activator.CreateInstance(type));
                }
                catch (Exception ex)
                {
                    Log.Warning($"[ModernExpandMenu] 实例化 Provider {type.Name} 失败，已跳过：{ex.Message}");
                }
            }
            return list;
        }

        /// <summary>
        /// 最终化分组：标注搬运物品名、过滤 disabled、移除空组、重新排序。
        /// 在分帧生成全部完成后调用（替代原先 BuildGroups 内的同步处理）。
        /// </summary>
        public static void FinalizeGroups(List<StoredItemGroup> groups)
        {
            // 同一分组内若有多个"搬运到储物区"项目，标注各自搬运的具体物品
            foreach (StoredItemGroup group in groups)
            {
                int haulCount = 0;
                foreach (ItemActionEntry entry in group.actions)
                {
                    if (entry.isHaulAction)
                    {
                        haulCount++;
                    }
                }
                if (haulCount > 1)
                {
                    foreach (ItemActionEntry entry in group.actions)
                    {
                        if (entry.isHaulAction && entry.targetThing != null)
                        {
                            entry.label = "ModernExpandMenu_HaulToStorageLabeled".Translate(entry.targetThing.LabelShortCap);
                        }
                    }
                }
            }

            // 过滤不可执行项，移除空分组
            foreach (StoredItemGroup group in groups)
            {
                group.actions.RemoveAll(entry => entry.disabled);
            }
            groups.RemoveAll(group => group.actions.Count == 0);

            // 重新排序（物品组按名称，"其他"组置于末尾）
            groups.Sort((a, b) =>
            {
                bool aIsOther = a.isOtherGroup;
                bool bIsOther = b.isOtherGroup;
                if (aIsOther != bIsOther)
                {
                    return aIsOther ? 1 : -1;
                }
                return string.Compare(a.headerLabel, b.headerLabel, StringComparison.Ordinal);
            });
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
