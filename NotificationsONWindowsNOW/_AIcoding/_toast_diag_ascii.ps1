# Diagnostic: send a plain ASCII toast via Windows PowerShell (WinRT).
# Uses ASCII-only content so no .ps1 encoding issue.
[Windows.UI.Notifications.ToastNotificationManager, Windows.UI.Notifications, ContentType = WindowsRuntime] | Out-Null
[Windows.Data.Xml.Dom.XmlDocument, Windows.Data.Xml.Dom.XmlDocument, ContentType = WindowsRuntime] | Out-Null
$xml = '<toast><visual><binding template="ToastGeneric"><text>Toast Test</text><text>System Toast works without AUMID</text></binding></visual></toast>'
$doc = New-Object Windows.Data.Xml.Dom.XmlDocument
$doc.LoadXml($xml)
$toast = New-Object Windows.UI.Notifications.ToastNotification($doc)
[Windows.UI.Notifications.ToastNotificationManager]::CreateToastNotifier('TestAppNoAumid').Show($toast)
Write-Host 'sent-ok'
