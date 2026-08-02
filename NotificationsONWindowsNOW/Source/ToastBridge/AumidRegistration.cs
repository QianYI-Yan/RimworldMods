using System;
using System.IO;
using System.Runtime.InteropServices;
using System.Text;

namespace ToastBridge
{
    /// <summary>
    /// 为无打包桌面应用注册 AUMID（AppUserModelID）。
    /// Windows 10 上未注册 AUMID 的应用发送的 Toast 会被静默丢弃，
    /// 因此需要在开始菜单创建快捷方式并写入 AUMID 属性。
    ///
    /// 注意：必须用 IShellLink COM 接口创建快捷方式，而不能用 WScript.Shell——
    /// 微软文档明确指出 WScript.Shell 创建的 .lnk 其 AppUserModelID 属性存储
    /// 会被标记为不可写（SetValue 抛 STG_E_ACCESSDENIED）。
    /// </summary>
    internal static class AumidRegistration
    {
        /// <summary>快捷方式显示名称。</summary>
        private const string ShortcutFileName = "RimWorld Notifications Bridge.lnk";

        // PKEY_AppUserModel_ID：{9F4C2855-9F79-4B39-A8D0-E1D42DE1D5F3}，属性序号 5
        private static readonly Guid AppUserModelIdKey = new Guid("9F4C2855-9F79-4B39-A8D0-E1D42DE1D5F3");
        private const int AppUserModelIdPropertyId = 5;

        // ShellLink COM：CLSID_ShellLink、IID_IShellLinkW、IID_IPersistFile
        private static readonly Guid ShellLinkClassId = new Guid("00021401-0000-0000-C000-000000000046");
        private static readonly Guid ShellLinkInterfaceId = new Guid("000214F9-0000-0000-C000-000000000046");
        private static readonly Guid PersistFileInterfaceId = new Guid("0000010B-0000-0000-C000-000000000046");

        private const int ClsContextInprocServer = 1;

        /// <summary>确保 AUMID 已注册；未注册则创建快捷方式并写入 AUMID。</summary>
        public static void EnsureRegistered(string aumid, string executablePath)
        {
            try
            {
                string startMenuDirectory = Path.Combine(
                    Environment.GetFolderPath(Environment.SpecialFolder.ApplicationData),
                    "Microsoft", "Windows", "Start Menu", "Programs");
                Directory.CreateDirectory(startMenuDirectory);

                string shortcutPath = Path.Combine(startMenuDirectory, ShortcutFileName);
                CreateShortcutViaShellLink(shortcutPath, executablePath);
                SetAppUserModelId(shortcutPath, aumid);
            }
            catch (Exception exception)
            {
                Console.Error.WriteLine("[ToastBridge] AUMID 注册失败: " + exception.Message);
            }
        }

        /// <summary>通过 IShellLink + IPersistFile 创建快捷方式（官方推荐方式，保证 AUMID 可写）。</summary>
        private static void CreateShortcutViaShellLink(string shortcutPath, string executablePath)
        {
            // 若存在旧版（WScript.Shell 创建的）快捷方式，先删除以重置属性存储。
            if (File.Exists(shortcutPath))
            {
                File.Delete(shortcutPath);
            }

            Guid shellLinkInterfaceId = ShellLinkInterfaceId;
            Guid shellLinkClassId = ShellLinkClassId; // 静态只读字段不能直接作为 ref，复制到局部变量
            IntPtr shellLinkPointer;
            int result = CoCreateInstance(
                ref shellLinkClassId, IntPtr.Zero, ClsContextInprocServer, ref shellLinkInterfaceId, out shellLinkPointer);
            if (result != 0)
            {
                throw new COMException("CoCreateInstance(ShellLink) 失败: 0x" + result.ToString("X8"));
            }

            try
            {
                IShellLinkW shellLink = (IShellLinkW)Marshal.GetObjectForIUnknown(shellLinkPointer);
                shellLink.SetPath(executablePath);
                shellLink.SetWorkingDirectory(Path.GetDirectoryName(executablePath));
                shellLink.SetDescription("RimWorld 通知桥梁进程");
                shellLink.SetIconLocation(executablePath, 0);

                Guid persistFileInterfaceId = PersistFileInterfaceId;
                IntPtr persistFilePointer;
                result = Marshal.QueryInterface(shellLinkPointer, ref persistFileInterfaceId, out persistFilePointer);
                if (result != 0)
                {
                    throw new COMException("QueryInterface(IPersistFile) 失败: 0x" + result.ToString("X8"));
                }

                try
                {
                    IPersistFile persistFile = (IPersistFile)Marshal.GetObjectForIUnknown(persistFilePointer);
                    persistFile.Save(shortcutPath, true);
                    Marshal.FinalReleaseComObject(persistFile);
                }
                finally
                {
                    Marshal.Release(persistFilePointer);
                }
            }
            finally
            {
                Marshal.Release(shellLinkPointer);
            }
        }

        /// <summary>通过 IPropertyStore 向快捷方式写入 AppUserModelID。</summary>
        private static void SetAppUserModelId(string shortcutPath, string aumid)
        {
            Guid propertyStoreInterfaceId = typeof(IPropertyStore).GUID;
            IntPtr storePointer;
            // flags 必须为 GPS_READWRITE (2)，否则返回只读属性存储，SetValue 会抛 STG_E_ACCESSDENIED。
            const int gpsReadWrite = 2;
            int result = SHGetPropertyStoreFromParsingName(
                shortcutPath, IntPtr.Zero, gpsReadWrite, ref propertyStoreInterfaceId, out storePointer);
            if (result != 0)
            {
                throw new COMException("SHGetPropertyStoreFromParsingName 失败: 0x" + result.ToString("X8"));
            }

            try
            {
                IPropertyStore propertyStore = (IPropertyStore)Marshal.GetObjectForIUnknown(storePointer);
                PropertyKey propertyKey = new PropertyKey
                {
                    formatId = AppUserModelIdKey,
                    propertyId = AppUserModelIdPropertyId
                };

                PropVariant value;
                value.type = (short)VarEnum.VT_LPWSTR;
                value.pointer = Marshal.StringToCoTaskMemUni(aumid);
                try
                {
                    propertyStore.SetValue(ref propertyKey, ref value);
                    propertyStore.Commit();
                }
                finally
                {
                    Marshal.FreeCoTaskMem(value.pointer);
                }
                Marshal.FinalReleaseComObject(propertyStore);
            }
            finally
            {
                Marshal.Release(storePointer);
            }
        }

        [DllImport("ole32.dll")]
        private static extern int CoCreateInstance(
            ref Guid classId, IntPtr outerUnknown, int context, ref Guid interfaceId, out IntPtr instance);

        [DllImport("shell32.dll", CharSet = CharSet.Unicode)]
        private static extern int SHGetPropertyStoreFromParsingName(
            string path, IntPtr bindingContext, int flags, ref Guid riid, out IntPtr propertyStore);
    }

    /// <summary>IShellLinkW COM 接口（用于创建快捷方式）。</summary>
    [ComImport]
    [Guid("000214F9-0000-0000-C000-000000000046")]
    [InterfaceType(ComInterfaceType.InterfaceIsIUnknown)]
    internal interface IShellLinkW
    {
        void GetPath([Out, MarshalAs(UnmanagedType.LPWStr)] StringBuilder path, int maxChars, IntPtr fileData, uint flags);
        void GetIDList(out IntPtr idList);
        void SetIDList(IntPtr idList);
        void GetDescription([Out, MarshalAs(UnmanagedType.LPWStr)] StringBuilder name, int maxChars);
        void SetDescription([MarshalAs(UnmanagedType.LPWStr)] string name);
        void GetWorkingDirectory([Out, MarshalAs(UnmanagedType.LPWStr)] StringBuilder directory, int maxChars);
        void SetWorkingDirectory([MarshalAs(UnmanagedType.LPWStr)] string directory);
        void GetArguments([Out, MarshalAs(UnmanagedType.LPWStr)] StringBuilder arguments, int maxChars);
        void SetArguments([MarshalAs(UnmanagedType.LPWStr)] string arguments);
        void GetHotkey(out ushort hotkey);
        void SetHotkey(ushort hotkey);
        void GetShowCmd(out int showCommand);
        void SetShowCmd(int showCommand);
        void GetIconLocation([Out, MarshalAs(UnmanagedType.LPWStr)] StringBuilder iconPath, int maxChars, out int iconIndex);
        void SetIconLocation([MarshalAs(UnmanagedType.LPWStr)] string iconPath, int iconIndex);
        void SetRelativePath([MarshalAs(UnmanagedType.LPWStr)] string pathRelative, uint reserved);
        void Resolve(IntPtr windowHandle, uint flags);
        void SetPath([MarshalAs(UnmanagedType.LPWStr)] string path);
    }

    /// <summary>IPersistFile COM 接口（用于保存快捷方式到磁盘）。</summary>
    [ComImport]
    [Guid("0000010B-0000-0000-C000-000000000046")]
    [InterfaceType(ComInterfaceType.InterfaceIsIUnknown)]
    internal interface IPersistFile
    {
        void GetClassID(out Guid classId);
        [PreserveSig] int IsDirty();
        void Load([MarshalAs(UnmanagedType.LPWStr)] string fileName, uint mode);
        void Save([MarshalAs(UnmanagedType.LPWStr)] string fileName, [MarshalAs(UnmanagedType.Bool)] bool remember);
        void SaveCompleted([MarshalAs(UnmanagedType.LPWStr)] string fileName);
        void GetCurFile([Out, MarshalAs(UnmanagedType.LPWStr)] out string fileName);
    }

    /// <summary>IPropertyStore COM 接口。</summary>
    [ComImport]
    [Guid("886D8EEB-8CF2-4446-8D02-CDBA1DBDCF99")]
    [InterfaceType(ComInterfaceType.InterfaceIsIUnknown)]
    internal interface IPropertyStore
    {
        void GetCount(out uint count);
        void GetAt(uint index, out PropertyKey key);
        void GetValue(ref PropertyKey key, out PropVariant value);
        void SetValue(ref PropertyKey key, ref PropVariant value);
        void Commit();
    }

    /// <summary>PROPERTYKEY 结构。</summary>
    [StructLayout(LayoutKind.Sequential, Pack = 4)]
    internal struct PropertyKey
    {
        public Guid formatId;
        public int propertyId;
    }

    /// <summary>PROPVARIANT 结构（仅覆盖用到的字段）。</summary>
    [StructLayout(LayoutKind.Explicit)]
    internal struct PropVariant
    {
        [FieldOffset(0)] public short type;
        [FieldOffset(8)] public IntPtr pointer;
    }
}
