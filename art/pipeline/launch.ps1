# Starts the game in a phone-sized window and prints its PID, so the other scripts touch only this instance.
param([string]$extra = "")
if ($extra -ne "") { $extra = "--" + $extra }
$args_ = "-screen-width 540 -screen-height 960 -screen-fullscreen 0 -logFile D:/Codes/Projects/lightswarm/player3d.log $extra"
$p = Start-Process -FilePath "D:\Codes\Projects\lightswarm\builds\win\IronNight.exe" -ArgumentList $args_ -PassThru
Write-Output $p.Id
