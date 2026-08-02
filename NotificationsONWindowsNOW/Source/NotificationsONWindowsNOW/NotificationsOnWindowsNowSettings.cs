using Verse;

namespace NotificationsOnWindowsNow
{
    /// <summary>模组设置：总开关、推送范围、合并窗口档位、去重开关。</summary>
    public class NotificationsOnWindowsNowSettings : ModSettings
    {
        /// <summary>总开关：一键启用/停用全部推送。</summary>
        public bool enableAllPush = true;

        /// <summary>推送信件（Letter）开关。</summary>
        public bool enableLetterPush = true;

        /// <summary>推送消息（Message）开关。</summary>
        public bool enableMessagePush = true;

        /// <summary>通知合并窗口档位：0=不合并，1=1 秒，2=2 秒，3=5 秒。</summary>
        public int mergeWindowOption = 2;

        /// <summary>短时间消息去重开关（同一时刻 200ms 内相同内容合并为一条，默认关闭）。</summary>
        public bool enableShortTimeMessageDedup = false;

        // ── 信件（Letter）类型细分开关 ──
        /// <summary>信件类型：威胁（袭击/虫灾等 ThreatBig/ThreatSmall）。</summary>
        public bool letterPushThreat = true;
        /// <summary>信件类型：任务/加入（访客/加入者/选择小人等）。</summary>
        public bool letterPushQuest = true;
        /// <summary>信件类型：成长（出生/成年/生日等 Biotech）。</summary>
        public bool letterPushGrowth = true;
        /// <summary>信件类型：其他事件（正面/负面/中性/死亡/Boss 等）。</summary>
        public bool letterPushOther = true;

        // ── 消息（Message）类型细分开关 ──
        /// <summary>消息类型：威胁。</summary>
        public bool messagePushThreat = true;
        /// <summary>消息类型：负面（死亡/健康/负面事件）。</summary>
        public bool messagePushNegative = true;
        /// <summary>消息类型：正面（完成/解决/正面事件）。</summary>
        public bool messagePushPositive = true;
        /// <summary>消息类型：中性/输入类。</summary>
        public bool messagePushNeutral = true;

        public override void ExposeData()
        {
            Scribe_Values.Look(ref enableAllPush, "enableAllPush", true);
            Scribe_Values.Look(ref enableLetterPush, "enableLetterPush", true);
            Scribe_Values.Look(ref enableMessagePush, "enableMessagePush", true);
            Scribe_Values.Look(ref mergeWindowOption, "mergeWindowOption", 2);
            Scribe_Values.Look(ref enableShortTimeMessageDedup, "enableShortTimeMessageDedup", false);
            Scribe_Values.Look(ref letterPushThreat, "letterPushThreat", true);
            Scribe_Values.Look(ref letterPushQuest, "letterPushQuest", true);
            Scribe_Values.Look(ref letterPushGrowth, "letterPushGrowth", true);
            Scribe_Values.Look(ref letterPushOther, "letterPushOther", true);
            Scribe_Values.Look(ref messagePushThreat, "messagePushThreat", true);
            Scribe_Values.Look(ref messagePushNegative, "messagePushNegative", true);
            Scribe_Values.Look(ref messagePushPositive, "messagePushPositive", true);
            Scribe_Values.Look(ref messagePushNeutral, "messagePushNeutral", true);
        }

        /// <summary>合并窗口档位对应的毫秒数（0=不合并，1=1s，2=2s，3=5s）。</summary>
        public int MergeWindowMilliseconds
        {
            get
            {
                switch (mergeWindowOption)
                {
                    case 0: return 0;
                    case 1: return 1000;
                    case 2: return 2000;
                    case 3: return 5000;
                    default: return 2000;
                }
            }
        }
    }
}
