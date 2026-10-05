#!/bin/bash
# The eighth batch from TRELLIS into Resources/Props: our infantry for the fire fights as figures on the ground facing
# +Z at their real height (figure_export.py; the men behind a machine gun turned 45 degrees to face along it; the fallen
# by their length, prop_export.py), then the
# textures' import settings (propmeta.py); and the photographed flames into Resources/Fx (batch8_fire.py).
cd "$(dirname "$0")"; O=D:/Tools/ComfyUI_windows_portable/ComfyUI/output/3D; PY=D:/Tools/ComfyUI_windows_portable/python_trellis/python.exe
done_names=()
for spec in "ally_us_kneel:1.3" "ally_us_stand:1.8" "ally_us_prone:0.75:turn=45" "ally_su_kneel:1.3" "ally_su_stand:1.8" "ally_su_prone:0.75:turn=45" "ally_us_dead:1.85:dead" "ally_su_dead:1.85:dead"; do
  IFS=: read -r name height turn <<< "$spec"
  glb=$(ls "$O/b8_${name}_"*.glb 2>/dev/null | tail -1); [ -z "$glb" ] && { echo "$name: not rendered yet"; continue; }
  if [ "$turn" = dead ]; then $PY prop_export.py "$glb" "$name" "$height"; else $PY figure_export.py "$glb" "$name" "$height" stand raw $turn; fi   # the fallen by their length
  cp "$glb" "../models/props/$name.glb"; done_names+=("$name")
done
[ ${#done_names[@]} -gt 0 ] && $PY propmeta.py "${done_names[@]}"
$PY batch8_fire.py
