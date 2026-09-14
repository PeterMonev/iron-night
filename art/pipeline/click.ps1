# Clicks at a fraction of the game window's client area. Usage: click.ps1 <process> <fx> <fy>
param([string]$proc = "IronNight", [double]$fx = 0.5, [double]$fy = 0.5)
Add-Type @"
using System; using System.Runtime.InteropServices;
public class C {
  [DllImport("user32.dll")] public static extern bool GetClientRect(IntPtr hwnd, out RECT r);
  [DllImport("user32.dll")] public static extern bool ClientToScreen(IntPtr hwnd, ref POINT p);
  [DllImport("user32.dll")] public static extern bool SetCursorPos(int x, int y);
  [DllImport("user32.dll")] public static extern void mouse_event(uint flags, int dx, int dy, uint data, UIntPtr extra);
  [DllImport("user32.dll")] public static extern bool SetForegroundWindow(IntPtr hwnd);
  [DllImport("user32.dll")] public static extern bool SetProcessDPIAware();
  [StructLayout(LayoutKind.Sequential)] public struct RECT { public int L, T, R, B; }
  [StructLayout(LayoutKind.Sequential)] public struct POINT { public int X, Y; }
}
"@
[C]::SetProcessDPIAware() | Out-Null
Add-Type -AssemblyName System.Windows.Forms
$p = if ($proc -match "^[0-9]+$") { Get-Process -Id ([int]$proc) -ErrorAction Stop } else { Get-Process $proc -ErrorAction Stop | Where-Object { $_.MainWindowHandle -ne 0 } | Select-Object -First 1 }
[System.Windows.Forms.SendKeys]::SendWait("%"); [C]::SetForegroundWindow($p.MainWindowHandle) | Out-Null; Start-Sleep -Milliseconds 300
$r = New-Object C+RECT; [C]::GetClientRect($p.MainWindowHandle, [ref]$r) | Out-Null
$pt = New-Object C+POINT; $pt.X = [int](($r.R - $r.L) * $fx); $pt.Y = [int](($r.B - $r.T) * $fy); [C]::ClientToScreen($p.MainWindowHandle, [ref]$pt) | Out-Null
[C]::SetCursorPos($pt.X, $pt.Y) | Out-Null; Start-Sleep -Milliseconds 120
[C]::mouse_event(0x0002, 0, 0, 0, [UIntPtr]::Zero); Start-Sleep -Milliseconds 90; [C]::mouse_event(0x0004, 0, 0, 0, [UIntPtr]::Zero)
Write-Output "clicked $fx,$fy"
