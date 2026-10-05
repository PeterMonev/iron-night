#!/bin/bash
# The ninth batch from TRELLIS into Resources/Props: the paratroopers hanging in the harness, standing figures facing
# +Z (figure_export.py), the height taken over the raised hands and the risers above them; then the textures' import
# settings (propmeta.py).
cd "$(dirname "$0")"; O=D:/Tools/ComfyUI_windows_portable/ComfyUI/output/3D; PY=D:/Tools/ComfyUI_windows_portable/python_trellis/python.exe
done_names=()
for spec in "para_us:2.5" "para_su:2.5"; do
  IFS=: read -r name height <<< "$spec"
  glb=$(ls "$O/b9_${name}_"*.glb 2>/dev/null | tail -1); [ -z "$glb" ] && { echo "$name: not rendered yet"; continue; }
  $PY figure_export.py "$glb" "$name" "$height" stand raw
  cp "$glb" "../models/props/$name.glb"; done_names+=("$name")
done
[ ${#done_names[@]} -gt 0 ] && $PY propmeta.py "${done_names[@]}"
