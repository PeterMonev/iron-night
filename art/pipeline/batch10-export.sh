#!/bin/bash
# The tenth batch from TRELLIS into Resources/Props: the anti-tank men of the last stand sandbags, figures facing
# +Z with the weapon along +Z (figure_export.py, turned), their real height; then the textures' import
# settings (propmeta.py).
cd "$(dirname "$0")"; O=D:/Tools/ComfyUI_windows_portable/ComfyUI/output/3D; PY=D:/Tools/ComfyUI_windows_portable/python_trellis/python.exe
done_names=()
for spec in "ally_us_bazooka:1.3" "ally_su_ptrd:0.75"; do
  IFS=: read -r name height <<< "$spec"
  glb=$(ls "$O/b10_${name}_"*.glb 2>/dev/null | tail -1); [ -z "$glb" ] && { echo "$name: not rendered yet"; continue; }
  $PY figure_export.py "$glb" "$name" "$height" stand raw
  cp "$glb" "../models/props/$name.glb"; done_names+=("$name")
done
[ ${#done_names[@]} -gt 0 ] && $PY propmeta.py "${done_names[@]}"
