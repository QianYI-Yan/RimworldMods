using Verse;

namespace NotificationsOnWindowsNow
{
    /// <summary>模组设置：去重开关、合并窗口、静默消息过滤。</summary>
    public class NotificationsOnWindowsNowSettings : ModSettings
    {
        /// <summary>短时间消息去重开关（默认关闭，用户可自行开启）。</summary>
        public bool enableShortTimeMessageDedup = false;

        /// <summary>通知合并窗口（秒），0 表示不合并。</summary>
        public float mergeWindowSeconds = 2f;

        public override void ExposeData()
        {
            Scribe_Values.Look(ref enableShortTimeMessageDedup, "enableShortTimeMessageDedup", false);
            Scribe_Values.Look(ref mergeWindowSeconds, "mergeWindowSeconds", 2f);
        }
    }
}
