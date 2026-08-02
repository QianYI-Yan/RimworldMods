using System;
using HarmonyLib;
using RimWorld;
using Verse;

namespace NotificationsOnWindowsNow
{
    /// <summary>
    /// 转发器：把游戏内通知（信件、消息）整理后交给 Windows 通知中心，
    /// 并做简单的去重节流，避免刷屏。
    /// </summary>
    public static class ToastForwarder
    {
        /// <summary>去重窗口（tick），相同内容在该时间内只推送一次。</summary>
        private const int DeduplicationWindowTicks = 180;

        /// <summary>上一条推送内容的指纹。</summary>
        private static string lastContentFingerprint = string.Empty;

        /// <summary>上一条推送发生的游戏 tick。</summary>
        private static int lastPushTick = -9999;

        /// <summary>转发一封信件。</summary>
        public static void ForwardLetter(Letter letter)
        {
            try
            {
                if (letter == null)
                {
                    return;
                }

                string title = letter.Label.Resolve().StripTags();
                string body = GetLetterBody(letter).StripTags();
                Push(title, body, isLetter: true);
            }
            catch (Exception exception)
            {
                Log.Warning("[NotificationsONWindowsNOW] 转发信件失败: " + exception);
            }
        }

        /// <summary>转发一条消息。</summary>
        public static void ForwardMessage(Message message)
        {
            try
            {
                if (message == null || string.IsNullOrEmpty(message.text))
                {
                    return;
                }

                // 消息文本可能包含 <color> 等富文本标签，剥离后推送纯文本。
                string title = message.text.StripTags();
                string body = message.def != null ? message.def.label.StripTags() : string.Empty;
                Push(title, body, isLetter: false);
            }
            catch (Exception exception)
            {
                Log.Warning("[NotificationsONWindowsNOW] 转发消息失败: " + exception);
            }
        }

        /// <summary>推送前按设置做去重（默认关闭），然后交给桥梁客户端。</summary>
        private static void Push(string title, string body, bool isLetter)
        {
            // 短时间去重仅在设置开启时生效（默认关闭）。
            if (NotificationsOnWindowsNowMod.Settings != null
                && NotificationsOnWindowsNowMod.Settings.enableShortTimeMessageDedup)
            {
                string fingerprint = title + "\u0001" + body;
                if (fingerprint == lastContentFingerprint
                    && GenTicks.TicksGame - lastPushTick < DeduplicationWindowTicks)
                {
                    return; // 短时间内重复内容，跳过
                }

                lastContentFingerprint = fingerprint;
                lastPushTick = GenTicks.TicksGame;
            }

            ToastBridgeClient.Send(title, body, isLetter);
        }

        /// <summary>获取信件的正文文本（悬停提示内容）。</summary>
        private static string GetLetterBody(Letter letter)
        {
            try
            {
                // GetMouseoverText 是受保护抽象方法，通过 Harmony Traverse 访问。
                string text = Traverse.Create(letter).Method("GetMouseoverText").GetValue<string>();
                return string.IsNullOrEmpty(text) ? string.Empty : text;
            }
            catch
            {
                return string.Empty;
            }
        }
    }
}
