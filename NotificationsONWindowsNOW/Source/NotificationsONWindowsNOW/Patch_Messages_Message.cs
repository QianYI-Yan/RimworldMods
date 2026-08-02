using System;
using HarmonyLib;
using Verse;

namespace NotificationsOnWindowsNow
{
    /// <summary>补丁 Messages.Message，捕获所有游戏内消息。</summary>
    [HarmonyPatch(typeof(Messages), nameof(Messages.Message),
        new Type[] { typeof(Message), typeof(bool) })]
    public static class Patch_Messages_Message
    {
        public static void Postfix(Message msg)
        {
            // 重复消息会被 Messages 内部拒绝（不进入存活列表），此时不推送。
            if (!Messages.IsLive(msg))
            {
                return;
            }

            ToastForwarder.ForwardMessage(msg);
        }
    }
}
