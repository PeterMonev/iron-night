# Holds W (and optionally D) in the focused game window for a while: keybd_event down ... up.
param([string]$proc = "IronNight", [double]$hold = 5, [switch]$right)
Add-Type @"
using System; using System.Runtime.InteropServices;
public class K {
  [DllImport("user32.dll")] public static extern void keybd_event(byte vk, byte scan, uint flags, UIntPtr extra);
  [DllImport("user32.dll")] public static extern bool SetForegroundWindow(IntPtr hwnd);
}
"@
Add-Type -AssemblyName System.Windows.Forms
$p = Get-Process $proc -ErrorAction Stop | Where-Object { $_.MainWindowHandle -ne 0 } | Select-Object -First 1
[System.Windows.Forms.SendKeys]::SendWait("%"); [K]::SetForegroundWindow($p.MainWindowHandle) | Out-Null; Start-Sleep -Milliseconds 400
[K]::keybd_event(0x57, 0x11, 0, [UIntPtr]::Zero)            # W down
if ($right) { [K]::keybd_event(0x44, 0x20, 0, [UIntPtr]::Zero) }  # D down
Start-Sleep -Seconds $hold
[K]::keybd_event(0x57, 0x11, 2, [UIntPtr]::Zero)            # W up
if ($right) { [K]::keybd_event(0x44, 0x20, 2, [UIntPtr]::Zero) }
Write-Output "held keys for $hold s"
