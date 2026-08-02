# Diagnostic: test AUMID property write on an existing .lnk via IPropertyStore.
# ASCII only. Run with: powershell.exe -NoProfile -ExecutionPolicy Bypass -File this.ps1

Add-Type -TypeDefinition @'
using System;
using System.Runtime.InteropServices;

[ComImport, Guid("886D8EEB-8CF2-4446-8D02-CDBA1DBDCF99"), InterfaceType(ComInterfaceType.InterfaceIsIUnknown)]
public interface IPropertyStore
{
    void GetCount(out uint cProps);
    void GetAt(uint iProp, out PropertyKey pkey);
    void GetValue(ref PropertyKey key, out PropVariant pv);
    void SetValue(ref PropertyKey key, ref PropVariant pv);
    void Commit();
}

[StructLayout(LayoutKind.Sequential, Pack = 4)]
public struct PropertyKey { public Guid fmtid; public int pid; }

[StructLayout(LayoutKind.Explicit)]
public struct PropVariant
{
    [FieldOffset(0)] public short vt;
    [FieldOffset(8)] public IntPtr ptr;
}

public static class AumidHelper
{
    [DllImport("shell32.dll", CharSet = CharSet.Unicode)]
    public static extern int SHGetPropertyStoreFromParsingName(string pszPath, IntPtr pbc, int flags, ref Guid riid, out IntPtr ppv);

    public static string Test(string lnkPath, string aumid)
    {
        Guid iid = typeof(IPropertyStore).GUID;
        IntPtr ppv;
        int hr = SHGetPropertyStoreFromParsingName(lnkPath, IntPtr.Zero, 0, ref iid, out ppv);
        if (hr != 0) return "SHGet fail: 0x" + hr.ToString("X8");
        try
        {
            var store = (IPropertyStore)Marshal.GetObjectForIUnknown(ppv);
            var key = new PropertyKey { fmtid = new Guid("9F4C2855-9F79-4B39-A8D0-E1D42DE1D5F3"), pid = 5 };
            var pv = new PropVariant();
            pv.vt = 31; // VT_LPWSTR
            pv.ptr = Marshal.StringToCoTaskMemUni(aumid);
            try
            {
                store.SetValue(ref key, ref pv);
                store.Commit();
                return "OK";
            }
            catch (Exception e) { return "SetValue fail: " + e.Message; }
            finally { Marshal.FreeCoTaskMem(pv.ptr); }
        }
        finally { Marshal.Release(ppv); }
    }
}
'@

$lnk = "$env:APPDATA\Microsoft\Windows\Start Menu\Programs\RimWorld Notifications Bridge.lnk"
Write-Host "lnk exists: $(Test-Path $lnk)"
$result = [AumidHelper]::Test($lnk, "RimWorld.NotificationsONWindowsNOW")
Write-Host "Result: $result"
