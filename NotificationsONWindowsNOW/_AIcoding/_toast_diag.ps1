# 诊断脚本：检查通知相关设置 + 直接调用 WinRT 发送测试 Toast
# 必须用 Windows PowerShell（powershell.exe）运行，PowerShell 7 不支持 WinRT 类型。

Write-Host "=== Windows 版本 ==="
[System.Environment]::OSVersion.Version.ToString()

Write-Host "`n=== 专注助手状态 ==="
$quietHours = Get-ItemProperty "HKCU:\Software\Microsoft\Windows\CurrentVersion\QuietHours" -ErrorAction SilentlyContinue
if ($quietHours) {
    $quietHours | Format-List
} else {
    Write-Host "无 QuietHours 注册表项（未开启专注助手）"
}

Write-Host "`n=== Push 通知开关 ==="
$pushNotifications = Get-ItemProperty "HKCU:\Software\Microsoft\Windows\CurrentVersion\PushNotifications" -ErrorAction SilentlyContinue
if ($pushNotifications) {
    $pushNotifications | Format-List
} else {
    Write-Host "无 PushNotifications 注册表项（默认开启）"
}

Write-Host "`n=== 直接发送测试 Toast（无 AUMID 注册）==="
try {
    [Windows.UI.Notifications.ToastNotificationManager, Windows.UI.Notifications, ContentType = WindowsRuntime] | Out-Null
    [Windows.Data.Xml.Dom.XmlDocument, Windows.Data.Xml.Dom.XmlDocument, ContentType = WindowsRuntime] | Out-Null
    $xml = '<toast><visual><binding template="ToastGeneric"><text>PowerShell 直发测试</text><text>看到这条说明系统 Toast 可用（无 AUMID）</text></binding></visual></toast>'
    $doc = New-Object Windows.Data.Xml.Dom.XmlDocument
    $doc.LoadXml($xml)
    $toast = New-Object Windows.UI.Notifications.ToastNotification($doc)
    [Windows.UI.Notifications.ToastNotificationManager]::CreateToastNotifier('PSDirectTestNoAumid').Show($toast)
    Write-Host "已发送，请检查通知中心是否弹出"
} catch {
    Write-Host "发送失败: $_"
}
