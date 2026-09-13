# Drives the leader in the running build with a simulated thumb: press in the window, drag in a direction, hold, release.
# Usage: drive.ps1 <process> <dx> <dy> <holdSeconds>   (dy negative = up the screen = forward)
param([string]$proc = "IronNight", [int]$dx = 0, [int]$dy = -120, [double]$hold = 4)
Add-Type @"
using System; using System.Runtime.InteropServices;
public class M {
  [DllImport("user32.dll")] public static extern bool GetWindowRect(IntPtr hwnd, out RECT r);
  [DllImport("user32.dll")] public static extern bool SetCursorPos(int x, int y);
  [DllImport("user32.dll")] public static extern void mouse_event(uint flags, int dx, int dy, uint data, UIntPtr extra);
  [DllImport("user32.dll")] public static extern bool SetForegroundWindow(IntPtr hwnd);
  [DllImport("user32.dll")] public static extern bool SetProcessDPIAware();
  [StructLayout(LayoutKind.Sequential)] public struct RECT { public int L, T, R, B; }
}
"@
[M]::SetProcessDPIAware() | Out-Null
$p = Get-Process $proc -ErrorAction Stop | Where-Object { $_.MainWindowHandle -ne 0 } | Select-Object -First 1
Add-Type -AssemblyName System.Windows.Forms; [System.Windows.Forms.SendKeys]::SendWait("%"); [M]::SetForegroundWindow($p.MainWindowHandle) | Out-Null; Start-Sleep -Milliseconds 400
$r = New-Object M+RECT; [M]::GetWindowRect($p.MainWindowHandle, [ref]$r) | Out-Null
$cx = [int](($r.L + $r.R) / 2); $cy = [int](($r.T + $r.B) * 0.55)
[M]::SetCursorPos($cx, $cy) | Out-Null; Start-Sleep -Milliseconds 100
[M]::mouse_event(0x0002, 0, 0, 0, [UIntPtr]::Zero)   # left down
for ($i = 1; $i -le 10; $i++) { [M]::SetCursorPos($cx + [int]($dx * $i / 10), $cy + [int]($dy * $i / 10)) | Out-Null; Start-Sleep -Milliseconds 40 }
Start-Sleep -Seconds $hold
[M]::mouse_event(0x0004, 0, 0, 0, [UIntPtr]::Zero)   # left up
Write-Output "drove $dx,$dy for $hold s"
