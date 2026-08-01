using System.Collections.Generic;
using System.Reflection;
using System.Reflection.Emit;
using HarmonyLib;
using Verse;

namespace AstrylsUIZhCN
{
    /// <summary>
    /// 硬编码字符串翻译核心。
    ///
    /// 设计（方便维护）：
    /// - 翻译文本存放在 RimWorld 标准语言文件 Languages/ChineseSimplified/Keyed/HardcodedZhCN.xml
    /// - 本类只维护「英文原文 → Keyed 键名」的稳定映射（只有原模组字符串变化时才需更新）
    /// - Transpiler 把 ldstr 指令替换为 Translator.Translate(键名) 调用，运行时查表
    ///
    /// 后续维护：只改 Keyed XML 即可更新翻译，无需重新编译 DLL。
    /// 缺失翻译键时 RimWorld 会回退显示键名（配合存在性检查，绝不报错）。
    /// </summary>
    public static class HardcodedStringReplacer
    {
        /// <summary>
        /// 稳定映射：原模组英文硬编码字符串 → 本汉化的 Keyed 键名。
        /// </summary>
        public static readonly Dictionary<string, string> KeyForString = new Dictionary<string, string>
        {
            // ================= Modern Social Tab =================
            // 设置项
            { "Relation list", "HardcodedZhCN.SocialTab.RelationList" },
            { "Default sort", "HardcodedZhCN.SocialTab.DefaultSort" },
            { "Default filter", "HardcodedZhCN.SocialTab.DefaultFilter" },
            { "Social stats", "HardcodedZhCN.SocialTab.SocialStats" },
            { "Record standing history", "HardcodedZhCN.SocialTab.RecordStandingHistory" },
            { "Reset to defaults", "HardcodedZhCN.SocialTab.ResetToDefaults" },
            { "Sets the Social tab's width (60% to 100% of its base width); the height is always full. The tab also auto-fits to your resolution and UI scale, so it never runs off-screen.", "HardcodedZhCN.SocialTab.WidthDesc" },
            { "How relation cards are ordered. Pinned pawns always stay on top; off-map pawns sink to the bottom.", "HardcodedZhCN.SocialTab.SortDesc" },
            { "Which relationships to show: everyone, blood family, lovers (lover/fiancé/spouse), or rivals (negative opinion either way).", "HardcodedZhCN.SocialTab.FilterDesc" },
            { "Records each colonist's social standing every few in-game hours so the graph can show history. Turn this off to remove all per-tick work from this mod; the graph then shows only the live value.", "HardcodedZhCN.SocialTab.HistoryDesc" },
            { "How often social standing is sampled. Longer intervals cost less performance in large colonies.", "HardcodedZhCN.SocialTab.SampleDesc" },
            { "How many days of standing history to keep. Shorter windows use less memory and save space.", "HardcodedZhCN.SocialTab.RetentionDesc" },
            { "How often the relationship list refreshes while the tab is open, in frames. Higher is lighter on frame rate; lower updates opinions and relationships more responsively.", "HardcodedZhCN.SocialTab.RefreshDesc" },
            { "Restore window scale, sort/filter, and the displayed social stats to defaults.", "HardcodedZhCN.SocialTab.RestoreDesc" },
            // 标签页内
            { "Switch to Modern Social Tab", "HardcodedZhCN.SocialTab.SwitchToModern" },
            { "Switch to vanilla Social tab", "HardcodedZhCN.SocialTab.SwitchToVanilla" },
            { "Assign ideoligion role", "HardcodedZhCN.SocialTab.AssignIdeoRole" },
            { "Opinion", "HardcodedZhCN.SocialTab.Opinion" },
            { "Relation", "HardcodedZhCN.SocialTab.Relation" },
            { "Standing", "HardcodedZhCN.SocialTab.Standing" },
            { "Relations", "HardcodedZhCN.SocialTab.Relations" },
            { "Not possible", "HardcodedZhCN.SocialTab.NotPossible" },
            { "Factors", "HardcodedZhCN.SocialTab.Factors" },
            { "How this is calculated", "HardcodedZhCN.SocialTab.HowCalculated" },
            { "How well-liked ", "HardcodedZhCN.SocialTab.WellLiked" },
            { "Current contributors", "HardcodedZhCN.SocialTab.CurrentContributors" },
            { "Recent interactions", "HardcodedZhCN.SocialTab.RecentInteractions" },
            { "No recent interactions.", "HardcodedZhCN.SocialTab.NoRecentInteractions" },
            { "Choose which social stats to display", "HardcodedZhCN.SocialTab.ChooseStats" },
            { "Your opinion of them", "HardcodedZhCN.SocialTab.YourOpinionOfThem" },
            { "Their opinion of you", "HardcodedZhCN.SocialTab.TheirOpinionOfYou" },
            { "Unpin (return to normal sort order)", "HardcodedZhCN.SocialTab.Unpin" },
            { "Pin to the top of the list", "HardcodedZhCN.SocialTab.Pin" },
            { "Break up with ", "HardcodedZhCN.SocialTab.BreakUp" },
            { "Propose marriage to ", "HardcodedZhCN.SocialTab.ProposeMarriage" },
            { "on the current map", "HardcodedZhCN.SocialTab.OnCurrentMap" },
            { "travelling in a caravan", "HardcodedZhCN.SocialTab.InCaravan" },
            { "travelling in a caravan (", "HardcodedZhCN.SocialTab.InCaravanParen" },
            { "elsewhere in the world", "HardcodedZhCN.SocialTab.Elsewhere" },
            { "unknown", "HardcodedZhCN.SocialTab.Unknown" },
            { "deceased", "HardcodedZhCN.SocialTab.Deceased" },
            { "No known relationships.", "HardcodedZhCN.SocialTab.NoKnownRelationships" },
            { "Ideoligion certainty: ", "HardcodedZhCN.SocialTab.IdeoCertainty" },
            { "Thoughts on ", "HardcodedZhCN.SocialTab.ThoughtsOn" },
            { "Nothing in particular.", "HardcodedZhCN.SocialTab.NothingInParticular" },
            { "No body observations yet.", "HardcodedZhCN.SocialTab.NoBodyObservations" },
            { "Permanently disabled for this pawn.", "HardcodedZhCN.SocialTab.PermanentlyDisabled" },
            { "Recording opinion…\nthe graph fills in over time.", "HardcodedZhCN.SocialTab.RecordingOpinion" },
            { "How {0} currently feels about {1}: {2}{3}.", "HardcodedZhCN.SocialTab.HowFeelsAbout" },

            // ================= Modern Learning Menu =================
            // 面板标题
            { "Colony Skills", "HardcodedZhCN.LearningMenu.ColonySkills" },
            { "Child Development", "HardcodedZhCN.LearningMenu.ChildDevelopment" },
            { "Growth Moments", "HardcodedZhCN.LearningMenu.GrowthMoments" },
            { "Learning Summary", "HardcodedZhCN.LearningMenu.LearningSummary" },
            { "VSE Expertise", "HardcodedZhCN.LearningMenu.VseExpertise" },
            { "Active Training", "HardcodedZhCN.LearningMenu.ActiveTraining" },
            // 面板说明
            { "Every colonist's skill levels at a glance.\n\nColumns are colonists, rows are the 12 skills. Each cell shows the level, an XP progress bar, and a passion icon (fire = vanilla, custom icons from Vanilla Skills Expanded). Hover any cell for XP detail. Scrolls horizontally for large colonies.", "HardcodedZhCN.LearningMenu.SkillsDesc" },
            { "Child development tracking (requires Biotech).\n\nOne card per child colonist: growth tier (0–8) shown as diamonds, growth-point progress to the next tier, the Learning need bar, active learning desires, and — with Children School & Learning — their school schedule. A ⚡ badge appears when a growth moment is ready.", "HardcodedZhCN.LearningMenu.ChildDesc" },
            { "Formal education systems.\n\nRim Education: each colonist's education level + progress to the next.\nProgression: Education: scheduled classes with teacher, students, and progress.\nChildren School & Learning: each child's school hours and current status.", "HardcodedZhCN.LearningMenu.EducationDesc" },
            { "A colony-wide skills & passions summary.\n\nPassion breakdown across all colonists, skill milestone counts (maxed / expert / skilled), and how many colonists are actively learning right now. Derived entirely from your colonists' skills — not the vanilla tutorial Learning Helper.", "HardcodedZhCN.LearningMenu.SummaryDesc" },
            { "Vanilla Skills Expanded — Expertise (requires VSE).\n\nExpertise is a separate progression system from the 12 normal skills: specialized knowledge tracks, each with its own level (0–20) and XP. Pick a colonist from the pill row to see their expertise records.", "HardcodedZhCN.LearningMenu.ExpertiseDesc" },
            { "Active skill-training in progress.\n\nGrowth-vat learning (Biotech / Enhanced Vat Learning): vat occupants and their progress toward the next skill XP award.\nSimple Learning: who is at a learning desk and what they're studying.\nMisc. Training: who is using training equipment and the skill they're practising.", "HardcodedZhCN.LearningMenu.TrainingDesc" },
            // 界面与提示
            { "Layout locked — left-click to unlock (enables drag / resize / minimise)", "HardcodedZhCN.LearningMenu.LayoutLocked" },
            { "Layout unlocked — left-click to lock (prevents accidental changes)", "HardcodedZhCN.LearningMenu.LayoutUnlocked" },
            { "Progression Dashboard layout reset to defaults.", "HardcodedZhCN.LearningMenu.LayoutReset" },
            { "No colonists on map.", "HardcodedZhCN.LearningMenu.NoColonists" },
            { "Requires Biotech DLC.", "HardcodedZhCN.LearningMenu.RequiresBiotech" },
            { "No child colonists on map.", "HardcodedZhCN.LearningMenu.NoChildColonists" },
            { "Growth points: {0:F1} / {1:F0}", "HardcodedZhCN.LearningMenu.GrowthPoints" },
            { "No school scheduled", "HardcodedZhCN.LearningMenu.NoSchoolScheduled" },
            { "No child colonists — no growth moments to track.", "HardcodedZhCN.LearningMenu.NoGrowthMoments" },
            { "Next milestone at age {0} ({1} years away)", "HardcodedZhCN.LearningMenu.NextMilestone" },
            { "All milestones passed", "HardcodedZhCN.LearningMenu.AllMilestonesPassed" },
            { "Next: +{0} trait choices", "HardcodedZhCN.LearningMenu.NextTraitChoices" },
            { "No education mods detected.\nLoad Progression: Education, Rim Education, or CSL.", "HardcodedZhCN.LearningMenu.NoEducationMods" },
            { "No classes set up. Use the Progression: Education tab to create classes.", "HardcodedZhCN.LearningMenu.NoClasses" },
            { "Not scheduled", "HardcodedZhCN.LearningMenu.NotScheduled" },
            { "No teacher", "HardcodedZhCN.LearningMenu.NoTeacher" },
            { "No training mods detected.\nLoad Biotech, Simple Learning, or Misc. Training.", "HardcodedZhCN.LearningMenu.NoTrainingMods" },
            { "EVL Enhanced", "HardcodedZhCN.LearningMenu.EvlEnhanced" },
            { "No colonists currently in growth vats.", "HardcodedZhCN.LearningMenu.NoVatOccupants" },
            { "Awards 8000 XP to a random skill when full.\nVat: ", "HardcodedZhCN.LearningMenu.VatAward" },
            { "No colonists training right now.", "HardcodedZhCN.LearningMenu.NoTrainingNow" },
            { "Minor passions", "HardcodedZhCN.LearningMenu.MinorPassions" },
            { "Major passions", "HardcodedZhCN.LearningMenu.MajorPassions" },
            { "VSE passions", "HardcodedZhCN.LearningMenu.VsePassions" },
            { "Requires Vanilla Skills Expanded.", "HardcodedZhCN.LearningMenu.RequiresVse" },
            { "No colonists.", "HardcodedZhCN.LearningMenu.NoColonists2" },
            { "Shrinks the whole dashboard uniformly. 100% = auto-fit to screen.", "HardcodedZhCN.LearningMenu.DashboardScaleDesc" },
            { "Lock layout", "HardcodedZhCN.LearningMenu.LockLayout" },
            { "Prevent dragging, resizing, and minimising panels.", "HardcodedZhCN.LearningMenu.LockLayoutDesc" },
            { "Reset layout to defaults", "HardcodedZhCN.LearningMenu.ResetLayout" },
        };

        /// <summary>
        /// 翻译包装方法：Transpiler 生成的 IL 调用此方法（而非直接调用 Translator.Translate）。
        ///
        /// 原因：Verse.Translator.Translate 是扩展方法，返回 TaggedString（struct），
        /// 直接替换 ldstr 会让 IL 栈上类型从 string 变成 TaggedString，导致
        /// InvalidProgramException（Invalid IL code）。包装方法返回 string，
        /// 与原 ldstr 的栈类型保持一致，生成的 IL 必然合法。
        /// 转发到 Translator.Translate 后再 ToString 取最终文本；键缺失时 RimWorld 回退显示键名。
        /// </summary>
        public static string Translate(string key)
        {
            return Translator.Translate(key).ToString();
        }

        /// <summary>本类 Translate 包装方法的反射句柄（编译期已确定，必非 null）。</summary>
        static readonly MethodInfo TranslateMethod =
            AccessTools.Method(typeof(HardcodedStringReplacer), nameof(Translate));

        /// <summary>
        /// 通用 Transpiler：把命中映射表的 ldstr 替换为 Translator.Translate(键名) 调用。
        /// </summary>
        public static IEnumerable<CodeInstruction> Transpiler(IEnumerable<CodeInstruction> instructions)
        {
            foreach (var code in instructions)
            {
                if (code.opcode == OpCodes.Ldstr
                    && code.operand is string str
                    && KeyForString.TryGetValue(str, out var key))
                {
                    // ldstr "键名"; call Verse.Translator::Translate(string)
                    yield return new CodeInstruction(OpCodes.Ldstr, key);
                    yield return new CodeInstruction(OpCodes.Call, TranslateMethod);
                }
                else
                {
                    yield return code;
                }
            }
        }
    }
}
