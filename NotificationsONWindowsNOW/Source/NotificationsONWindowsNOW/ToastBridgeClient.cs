using System;
using System.Diagnostics;
using System.IO;
using System.IO.Pipes;
using System.Text;
using Verse;

namespace NotificationsOnWindowsNow
{
    /// <summary>
    /// 与 ToastBridge 桥梁进程通信的客户端。
    /// 负责确保桥梁进程运行，并通过命名管道把通知内容发送给它。
    /// </summary>
    public static class ToastBridgeClient
    {
        /// <summary>命名管道名称，与桥梁进程约定一致。</summary>
        private const string PipeName = "RimWorldToastBridge";

        /// <summary>桥梁进程可执行文件名称（不含扩展名，用于进程探测）。</summary>
        private const string BridgeProcessName = "ToastBridge";

        /// <summary>桥梁进程所在子目录（相对模组根目录）。</summary>
        private const string BridgeSubDirectory = "Bridge";

        /// <summary>管道连接超时（毫秒）。</summary>
        private const int ConnectTimeoutMilliseconds = 1500;

        /// <summary>标题与正文的分隔符，与桥梁进程约定一致。</summary>
        private const char TitleBodySeparator = '\u0001';

        /// <summary>控制行前缀，与普通通知行区分。</summary>
        private const string ControlLinePrefix = "\u0002";

        /// <summary>已同步到桥梁的设置指纹，避免重复发送。</summary>
        private static string lastSyncedSettingsHash = string.Empty;

        /// <summary>同步锁，避免多线程同时发送。</summary>
        private static readonly object SyncRoot = new object();

        /// <summary>发送一条通知到 Windows 通知中心。</summary>
        /// <param name="isLetter">是否为信件（事件锚点）：true 时桥梁侧会把伴随消息合并进它，false 的独立消息单独推送。</param>
        public static void Send(string title, string body, bool isLetter)
        {
            if (string.IsNullOrEmpty(title) && string.IsNullOrEmpty(body))
            {
                return;
            }

            lock (SyncRoot)
            {
                // 设置变化时，在同一连接内先发一条设置控制行，再发通知行。
                string settingsControlLine = BuildSettingsControlLineIfChanged();
                string notifyLine = Sanitize(title) + TitleBodySeparator + Sanitize(body)
                    + TitleBodySeparator + (isLetter ? "L" : "M");
                string payload = settingsControlLine != null
                    ? settingsControlLine + "\n" + notifyLine
                    : notifyLine;

                if (TrySendOnce(payload))
                {
                    return;
                }

                // 首次失败：可能是桥梁进程未启动，尝试拉起后重试一次。
                StartBridgeProcessIfNeeded();
                TrySendOnce(payload);
            }
        }

        /// <summary>
        /// 若设置自上次同步后发生变化，返回要发送的设置控制行；否则返回 null。
        /// 目前仅同步合并窗口（merge），去重与过滤都在模组侧完成，无需下发。
        /// </summary>
        private static string BuildSettingsControlLineIfChanged()
        {
            NotificationsOnWindowsNowSettings settings = NotificationsOnWindowsNowMod.Settings;
            if (settings == null)
            {
                return null;
            }

            int mergeMilliseconds = (int)(settings.mergeWindowSeconds * 1000f);
            string settingsHash = mergeMilliseconds.ToString();
            if (settingsHash == lastSyncedSettingsHash)
            {
                return null;
            }

            lastSyncedSettingsHash = settingsHash;
            return ControlLinePrefix + "SET" + TitleBodySeparator + "merge=" + mergeMilliseconds;
        }

        /// <summary>尝试通过命名管道发送一次负载。</summary>
        private static bool TrySendOnce(string payload)
        {
            try
            {
                using (var client = new NamedPipeClientStream(".", PipeName, PipeDirection.Out))
                {
                    client.Connect(ConnectTimeoutMilliseconds);
                    byte[] bytes = Encoding.UTF8.GetBytes(payload + "\n");
                    client.Write(bytes, 0, bytes.Length);
                    client.Flush();
                }
                return true;
            }
            catch (Exception exception)
            {
                Log.Warning("[NotificationsONWindowsNOW] 管道发送失败: " + exception.Message);
                return false;
            }
        }

        /// <summary>确保桥梁进程正在运行（未运行则启动）。</summary>
        private static void StartBridgeProcessIfNeeded()
        {
            try
            {
                if (Process.GetProcessesByName(BridgeProcessName).Length > 0)
                {
                    return; // 进程已在运行，等待其管道就绪即可
                }

                string bridgeExecutablePath = FindBridgeExecutablePath();
                if (bridgeExecutablePath == null)
                {
                    Log.Warning("[NotificationsONWindowsNOW] 未找到桥梁进程 ToastBridge.exe，无法推送通知。");
                    return;
                }

                var startInfo = new ProcessStartInfo
                {
                    FileName = bridgeExecutablePath,
                    UseShellExecute = false,
                    CreateNoWindow = true,
                    WindowStyle = ProcessWindowStyle.Hidden
                };
                Process.Start(startInfo);
            }
            catch (Exception exception)
            {
                Log.Warning("[NotificationsONWindowsNOW] 启动桥梁进程失败: " + exception.Message);
            }
        }

        /// <summary>定位桥梁进程可执行文件的绝对路径。</summary>
        private static string FindBridgeExecutablePath()
        {
            if (string.IsNullOrEmpty(NotificationsOnWindowsNowMod.ModRootDirectory))
            {
                return null;
            }

            string path = Path.Combine(NotificationsOnWindowsNowMod.ModRootDirectory, BridgeSubDirectory, BridgeProcessName + ".exe");
            return File.Exists(path) ? path : null;
        }

        /// <summary>清理可能破坏管道协议的控制字符与多余空白。</summary>
        private static string Sanitize(string text)
        {
            if (string.IsNullOrEmpty(text))
            {
                return string.Empty;
            }

            var builder = new StringBuilder(text.Length);
            foreach (char character in text)
            {
                // 保留可打印字符与换行，剔除其余控制字符及分隔符本身。
                if ((character == '\n' || character == '\r' || character >= ' ') && character != TitleBodySeparator)
                {
                    builder.Append(character);
                }
            }
            return builder.ToString();
        }
    }
}
