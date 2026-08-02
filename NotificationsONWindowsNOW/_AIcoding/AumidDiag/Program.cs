using System;
using System.IO;
using System.Runtime.InteropServices;
using System.Text;

// AUMID 写入诊断程序：分步定位 SetValue 拒绝访问的具体原因。
// 在 %TEMP%\AumidDiag 下创建快捷方式，避免开始菜单目录的干扰因素。
class Program
{
    static void Main(string[] args)
    {
        string targetDir = Path.Combine(Path.GetTempPath(), "AumidDiag");
        Directory.CreateDirectory(targetDir);
        string lnk = Path.Combine(targetDir, "test.lnk");
        if (File.Exists(lnk)) File.Delete(lnk);

        string exe = System.Reflection.Assembly.GetEntryAssembly().Location;

        Console.WriteLine("== 诊断 AUMID 属性写入 ==");
        Console.WriteLine("快捷方式路径: " + lnk);

        // 步骤1：IShellLink 创建
        try
        {
            CreateShortcut(lnk, exe);
            Console.WriteLine("[1] IShellLink 创建快捷方式 OK, exists=" + File.Exists(lnk));
        }
        catch (Exception e)
        {
            Console.WriteLine("[1] IShellLink 创建失败: " + e.Message);
            return;
        }

        // 步骤2：SHGetPropertyStoreFromParsingName（flags=2 即 GPS_READWRITE，可写属性存储）
        Guid iid = typeof(IPropertyStore).GUID;
        IntPtr storePtr;
        int hr = SHGetPropertyStoreFromParsingName(lnk, IntPtr.Zero, 2 /* GPS_READWRITE */, ref iid, out storePtr);
        Console.WriteLine("[2] SHGetPropertyStoreFromParsingName hr=0x" + hr.ToString("X8"));
        if (hr != 0) return;

        IPropertyStore store = (IPropertyStore)Marshal.GetObjectForIUnknown(storePtr);
        try
        {
            // 步骤3：GetCount
            uint count;
            try
            {
                store.GetCount(out count);
                Console.WriteLine("[3] GetCount=" + count);
            }
            catch (Exception e)
            {
                Console.WriteLine("[3] GetCount 失败: " + e.Message);
                return;
            }

            var key = new PropertyKey { fmtid = new Guid("9F4C2855-9F79-4B39-A8D0-E1D42DE1D5F3"), pid = 5 };

            // 步骤4：GetValue（读取 AUMID 当前值）
            try
            {
                PropVariant pv;
                store.GetValue(ref key, out pv);
                Console.WriteLine("[4] GetValue OK, vt=" + pv.vt);
            }
            catch (Exception e)
            {
                Console.WriteLine("[4] GetValue 失败: " + e.Message);
            }

            // 步骤5：SetValue
            try
            {
                var val = new PropVariant();
                val.vt = 31; // VT_LPWSTR
                val.ptr = Marshal.StringToCoTaskMemUni("Test.Aumid.Diag");
                try
                {
                    store.SetValue(ref key, ref val);
                    Console.WriteLine("[5] SetValue OK");
                }
                finally
                {
                    Marshal.FreeCoTaskMem(val.ptr);
                }
            }
            catch (Exception e)
            {
                Console.WriteLine("[5] SetValue 失败: " + e.Message);
                return;
            }

            // 步骤6：Commit
            try
            {
                store.Commit();
                Console.WriteLine("[6] Commit OK");
            }
            catch (Exception e)
            {
                Console.WriteLine("[6] Commit 失败: " + e.Message);
            }
        }
        finally
        {
            Marshal.FinalReleaseComObject(store);
            Marshal.Release(storePtr);
        }
    }

    static void CreateShortcut(string lnkPath, string exePath)
    {
        Guid shellLinkClassId = new Guid("00021401-0000-0000-C000-000000000046");
        Guid shellLinkIid = new Guid("000214F9-0000-0000-C000-000000000046");
        Guid persistIid = new Guid("0000010B-0000-0000-C000-000000000046");

        IntPtr slPtr;
        int hr = CoCreateInstance(ref shellLinkClassId, IntPtr.Zero, 1, ref shellLinkIid, out slPtr);
        if (hr != 0) throw new COMException("CoCreateInstance hr=0x" + hr.ToString("X8"));

        try
        {
            IShellLinkW sl = (IShellLinkW)Marshal.GetObjectForIUnknown(slPtr);
            sl.SetPath(exePath);
            sl.SetWorkingDirectory(Path.GetDirectoryName(exePath));
            sl.SetDescription("diagnostic");
            sl.SetIconLocation(exePath, 0);

            IntPtr pfPtr;
            hr = Marshal.QueryInterface(slPtr, ref persistIid, out pfPtr);
            if (hr != 0) throw new COMException("QI IPersistFile hr=0x" + hr.ToString("X8"));
            try
            {
                IPersistFile pf = (IPersistFile)Marshal.GetObjectForIUnknown(pfPtr);
                pf.Save(lnkPath, true);
                Marshal.FinalReleaseComObject(pf);
            }
            finally
            {
                Marshal.Release(pfPtr);
            }
        }
        finally
        {
            Marshal.Release(slPtr);
        }
    }

    [DllImport("ole32.dll")]
    static extern int CoCreateInstance(ref Guid clsid, IntPtr outer, int context, ref Guid iid, out IntPtr ppv);

    [DllImport("shell32.dll", CharSet = CharSet.Unicode)]
    static extern int SHGetPropertyStoreFromParsingName(string path, IntPtr pbc, int flags, ref Guid riid, out IntPtr ppv);
}

[ComImport, Guid("000214F9-0000-0000-C000-000000000046"), InterfaceType(ComInterfaceType.InterfaceIsIUnknown)]
interface IShellLinkW
{
    void GetPath([Out, MarshalAs(UnmanagedType.LPWStr)] StringBuilder path, int maxChars, IntPtr fd, uint flags);
    void GetIDList(out IntPtr idList);
    void SetIDList(IntPtr idList);
    void GetDescription([Out, MarshalAs(UnmanagedType.LPWStr)] StringBuilder name, int maxChars);
    void SetDescription([MarshalAs(UnmanagedType.LPWStr)] string name);
    void GetWorkingDirectory([Out, MarshalAs(UnmanagedType.LPWStr)] StringBuilder dir, int maxChars);
    void SetWorkingDirectory([MarshalAs(UnmanagedType.LPWStr)] string dir);
    void GetArguments([Out, MarshalAs(UnmanagedType.LPWStr)] StringBuilder args, int maxChars);
    void SetArguments([MarshalAs(UnmanagedType.LPWStr)] string args);
    void GetHotkey(out ushort hotkey);
    void SetHotkey(ushort hotkey);
    void GetShowCmd(out int showCmd);
    void SetShowCmd(int showCmd);
    void GetIconLocation([Out, MarshalAs(UnmanagedType.LPWStr)] StringBuilder iconPath, int maxChars, out int iconIndex);
    void SetIconLocation([MarshalAs(UnmanagedType.LPWStr)] string iconPath, int iconIndex);
    void SetRelativePath([MarshalAs(UnmanagedType.LPWStr)] string relPath, uint reserved);
    void Resolve(IntPtr hwnd, uint flags);
    void SetPath([MarshalAs(UnmanagedType.LPWStr)] string path);
}

[ComImport, Guid("0000010B-0000-0000-C000-000000000046"), InterfaceType(ComInterfaceType.InterfaceIsIUnknown)]
interface IPersistFile
{
    void GetClassID(out Guid classId);
    [PreserveSig] int IsDirty();
    void Load([MarshalAs(UnmanagedType.LPWStr)] string fileName, uint mode);
    void Save([MarshalAs(UnmanagedType.LPWStr)] string fileName, [MarshalAs(UnmanagedType.Bool)] bool remember);
    void SaveCompleted([MarshalAs(UnmanagedType.LPWStr)] string fileName);
    void GetCurFile([Out, MarshalAs(UnmanagedType.LPWStr)] out string fileName);
}

[ComImport, Guid("886D8EEB-8CF2-4446-8D02-CDBA1DBDCF99"), InterfaceType(ComInterfaceType.InterfaceIsIUnknown)]
interface IPropertyStore
{
    void GetCount(out uint count);
    void GetAt(uint index, out PropertyKey key);
    void GetValue(ref PropertyKey key, out PropVariant value);
    void SetValue(ref PropertyKey key, ref PropVariant value);
    void Commit();
}

[StructLayout(LayoutKind.Sequential, Pack = 4)]
struct PropertyKey
{
    public Guid fmtid;
    public int pid;
}

[StructLayout(LayoutKind.Explicit)]
struct PropVariant
{
    [FieldOffset(0)] public short vt;
    [FieldOffset(8)] public IntPtr ptr;
}
