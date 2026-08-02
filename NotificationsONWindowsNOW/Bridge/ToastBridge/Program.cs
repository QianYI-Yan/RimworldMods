using System;
using System.Collections.Generic;
using System.IO;
using System.IO.Pipes;
using System.Reflection;
using System.Text;
using System.Threading;

namespace ToastBridge
{
    /// <summary>
    /// 桥梁进程：常驻后台，通过命名管道接收 RimWorld 模组发来的通知内容，
    /// 调用 Windows 通知中心（Toast）API 展示。
    ///
    /// 因为 RimWorld 运行在 Unity Mono 上无法直接调用 WinRT，
    /// 所以由本进程（.NET Framework CLR）代为发送 Toast 通知。
    /// </summary>
    internal static class Program
    {
        /// <summary>命名管道名称，与模组侧约定一致。</summary>
        private const string PipeName = "RimWorldToastBridge";

        /// <summary>Toast 的应用标识（AUMID），用于通知中心归组显示。</summary>
        private const string AppId = "RimWorld.NotificationsONWindowsNOW";

        /// <summary>等待客户端连接的空闲超时；超过则自动退出以释放资源。</summary>
        private static readonly TimeSpan IdleTimeout = TimeSpan.FromMinutes(5);

        /// <summary>标题与正文的分隔符，与模组侧约定一致。</summary>
        private const char TitleBodySeparator = '\u0001';

        /// <summary>控制行前缀，与普通通知行区分。</summary>
        private const string ControlLinePrefix = "\u0002";

        /// <summary>默认通知合并窗口（毫秒）：窗口内的多条通知合并为一条 Toast。</summary>
        private const int DefaultMergeWindowMilliseconds = 2000;

        /// <summary>合并后正文最多保留的行数，超出丢弃。</summary>
        private const int MaxMergedBodyLines = 8;

        /// <summary>当前合并窗口（毫秒），可由模组的设置控制行更新；0 表示不合并。</summary>
        private static int currentMergeWindowMilliseconds = DefaultMergeWindowMilliseconds;

        // 合并缓冲（跨线程访问，统一加锁）
        private static readonly object MergeSyncRoot = new object();
        private static string pendingTitle = string.Empty;
        private static readonly List<string> pendingBodyLines = new List<string>();
        private static Timer mergeTimer;

        // WinRT 反射句柄
        private static Type toastManagerType;
        private static Type xmlDocumentType;
        private static Type toastNotificationType;
        private static object toastNotifier;

        private static int Main()
        {
            // 注册 AUMID（首次运行自动创建开始菜单快捷方式并写入 AppUserModelID），
            // 否则 Win10 会静默丢弃无打包应用的 Toast 通知。
            string executablePath = System.Reflection.Assembly.GetEntryAssembly().Location;
            AumidRegistration.EnsureRegistered(AppId, executablePath);

            if (!TryInitializeWinRt())
            {
                Console.Error.WriteLine("[ToastBridge] Windows 通知 API 初始化失败，进程退出。");
                return 1;
            }

            RunPipeServer();
            return 0;
        }

        /// <summary>
        /// 通过反射初始化 WinRT Toast 相关类型与通知器。
        /// 采用反射而非直接引用 Windows.winmd，保证项目不依赖 Windows SDK 路径、跨机器可编译。
        /// </summary>
        private static bool TryInitializeWinRt()
        {
            try
            {
                const string windowsAssembly =
                    "Windows, Version=255.255.255.255, Culture=neutral, PublicKeyToken=null, ContentType=WindowsRuntime";

                toastManagerType = Type.GetType("Windows.UI.Notifications.ToastNotificationManager, " + windowsAssembly);
                xmlDocumentType = Type.GetType("Windows.Data.Xml.Dom.XmlDocument, " + windowsAssembly);
                toastNotificationType = Type.GetType("Windows.UI.Notifications.ToastNotification, " + windowsAssembly);

                if (toastManagerType == null || xmlDocumentType == null || toastNotificationType == null)
                {
                    return false;
                }

                toastNotifier = toastManagerType
                    .GetMethod("CreateToastNotifier", new[] { typeof(string) })
                    ?.Invoke(null, new object[] { AppId });

                return toastNotifier != null;
            }
            catch
            {
                return false;
            }
        }

        /// <summary>循环监听命名管道，处理客户端（模组）发来的每一行通知。</summary>
        private static void RunPipeServer()
        {
            while (true)
            {
                using (var server = new NamedPipeServerStream(
                    PipeName, PipeDirection.In, 1, PipeTransmissionMode.Byte, PipeOptions.Asynchronous))
                {
                    var cancellationSource = new CancellationTokenSource(IdleTimeout);
                    try
                    {
                        server.WaitForConnectionAsync(cancellationSource.Token).GetAwaiter().GetResult();
                    }
                    catch (OperationCanceledException)
                    {
                        return; // 长时间无客户端连接，自动退出
                    }
                    catch (Exception)
                    {
                        return; // 管道被占用（已有实例）或其它异常，退出
                    }

                    using (var reader = new StreamReader(server, Encoding.UTF8, false, 4096, true))
                    {
                        string line;
                        while ((line = reader.ReadLine()) != null)
                        {
                            HandleNotificationLine(line);
                        }
                    }

                    server.Disconnect();
                }
            }
        }

        /// <summary>
        /// 解析一行负载：控制行（\u0002 开头）直接处理，普通通知行按类型分流。
        /// 信件（L）作为事件锚点合并伴随消息；独立消息（M）单独推送，不与无关事件合并。
        /// </summary>
        private static void HandleNotificationLine(string line)
        {
            if (string.IsNullOrEmpty(line))
            {
                return;
            }

            // 控制行（以 \u0002 开头）直接处理，不进入合并缓冲。
            if (line.StartsWith(ControlLinePrefix))
            {
                HandleControlLine(line.Substring(ControlLinePrefix.Length));
                return;
            }

            string[] parts = line.Split(TitleBodySeparator);
            string title = parts.Length > 0 ? parts[0] : string.Empty;
            string body = parts.Length > 1 ? parts[1] : string.Empty;
            // 第三段为类型标记：L=信件（事件锚点），M=消息。
            bool isLetter = parts.Length > 2 && parts[2] == "L";

            lock (MergeSyncRoot)
            {
                // 合并窗口为 0（或负数）时，不合并，立即发送。
                if (currentMergeWindowMilliseconds <= 0)
                {
                    ShowToast(title, body);
                    return;
                }

                if (isLetter)
                {
                    // 新信件作为事件锚点：若缓冲还有未发送内容，先发出，避免两个事件混在一起。
                    if (pendingBodyLines.Count > 0)
                    {
                        FlushMergedNotification(null);
                    }

                    pendingTitle = title;
                    pendingBodyLines.Clear();
                    string firstLine = !string.IsNullOrEmpty(body) ? body : title;
                    if (!string.IsNullOrEmpty(firstLine))
                    {
                        pendingBodyLines.Add(firstLine);
                    }
                    ResetMergeTimer();
                }
                else
                {
                    if (pendingBodyLines.Count > 0)
                    {
                        // 有未发送的信件锚点：该消息属于同一事件，合并进缓冲。
                        string content = !string.IsNullOrEmpty(body) ? body : title;
                        if (!string.IsNullOrEmpty(content) && pendingBodyLines.Count < MaxMergedBodyLines)
                        {
                            pendingBodyLines.Add(content);
                        }
                        ResetMergeTimer();
                    }
                    else
                    {
                        // 无锚点：独立消息（如存档、研究完成）立即单独推送，不合并。
                        ShowToast(title, body);
                    }
                }
            }
        }

        /// <summary>重置合并窗口计时器。</summary>
        private static void ResetMergeTimer()
        {
            if (mergeTimer == null)
            {
                mergeTimer = new Timer(FlushMergedNotification, null, currentMergeWindowMilliseconds, Timeout.Infinite);
            }
            else
            {
                mergeTimer.Change(currentMergeWindowMilliseconds, Timeout.Infinite);
            }
        }

        /// <summary>处理设置控制行（如 SET\u0001merge=2000），更新合并窗口等参数。</summary>
        private static void HandleControlLine(string command)
        {
            string[] parts = command.Split(TitleBodySeparator);
            if (parts.Length < 2 || parts[0] != "SET")
            {
                return;
            }

            for (int index = 1; index < parts.Length; index++)
            {
                string[] keyValue = parts[index].Split('=');
                if (keyValue.Length != 2 || keyValue[0] != "merge")
                {
                    continue;
                }

                int mergeMilliseconds;
                if (int.TryParse(keyValue[1], out mergeMilliseconds))
                {
                    lock (MergeSyncRoot)
                    {
                        currentMergeWindowMilliseconds = mergeMilliseconds;
                    }
                }
            }
        }

        /// <summary>合并窗口到期，把缓冲中的通知合并成一条 Toast 发送。</summary>
        private static void FlushMergedNotification(object state)
        {
            lock (MergeSyncRoot)
            {
                if (pendingBodyLines.Count == 0)
                {
                    return;
                }

                string title = pendingTitle;
                string body = string.Join("\n", pendingBodyLines);
                pendingTitle = string.Empty;
                pendingBodyLines.Clear();

                ShowToast(title, body);
            }
        }

        /// <summary>以 Toast 形式展示一条通知。</summary>
        private static void ShowToast(string title, string body)
        {
            try
            {
                string xml =
                    "<toast>" +
                    "<visual><binding template='ToastGeneric'>" +
                    "<text>" + XmlEscape(Truncate(title, 120)) + "</text>" +
                    "<text>" + XmlEscape(Truncate(body, 300)) + "</text>" +
                    "</binding></visual>" +
                    "</toast>";

                object document = Activator.CreateInstance(xmlDocumentType);
                // LoadXml 存在两个重载，需按参数类型精确匹配。
                xmlDocumentType.GetMethod("LoadXml", new[] { typeof(string) }).Invoke(document, new object[] { xml });

                object toast = Activator.CreateInstance(toastNotificationType, new object[] { document });
                // Show 存在多个重载，需按参数类型精确匹配。
                toastNotifier.GetType().GetMethod("Show", new[] { toastNotificationType }).Invoke(toastNotifier, new object[] { toast });
            }
            catch (Exception exception)
            {
                Console.Error.WriteLine("[ToastBridge] Toast 发送失败: " + exception);
            }
        }

        /// <summary>XML 特殊字符转义，防止破坏 Toast 内容结构。</summary>
        private static string XmlEscape(string text)
        {
            if (string.IsNullOrEmpty(text))
            {
                return string.Empty;
            }

            return text
                .Replace("&", "&amp;")
                .Replace("<", "&lt;")
                .Replace(">", "&gt;")
                .Replace("\"", "&quot;")
                .Replace("'", "&apos;");
        }

        /// <summary>按字符数截断文本，避免 Toast 内容过长。</summary>
        private static string Truncate(string text, int maxLength)
        {
            if (string.IsNullOrEmpty(text) || text.Length <= maxLength)
            {
                return text ?? string.Empty;
            }

            return text.Substring(0, maxLength) + "…";
        }
    }
}
