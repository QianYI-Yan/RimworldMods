using System;
using HarmonyLib;
using Verse;

namespace NotificationsOnWindowsNow
{
    /// <summary>补丁 LetterStack.ReceiveLetter，捕获所有进入信件栏的信件。</summary>
    [HarmonyPatch(typeof(LetterStack), nameof(LetterStack.ReceiveLetter),
        new Type[] { typeof(Letter), typeof(string), typeof(int), typeof(bool) })]
    public static class Patch_LetterStack_ReceiveLetter
    {
        public static void Postfix(Letter let)
        {
            ToastForwarder.ForwardLetter(let);
        }
    }
}
