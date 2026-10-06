#!/bin/bash
# The eleventh batch (Italy) from TRELLIS into Resources/Props (prop_export.py: long axis along Z, on the ground, at
# its real size; "y" sizes by height), then the textures' import settings (propmeta.py). Skips what TRELLIS has not
# finished.
cd "$(dirname "$0")"; O=D:/Tools/ComfyUI_windows_portable/ComfyUI/output/3D; PY=D:/Tools/ComfyUI_windows_portable/python_trellis/python.exe
done_names=()
for spec in "it_house:10" "it_farmhouse:15" "it_church:20" "it_ruin:11" "it_wall:6:turn=45" "it_well:2.8:y" \
            "it_olive:6:y" "it_cypress:12:y"; do
  IFS=: read -r name size axis <<< "$spec"
  glb=$(ls "$O/b11_${name}_"*.glb 2>/dev/null | tail -1); [ -z "$glb" ] && { echo "$name: not rendered yet"; continue; }
  $PY prop_export.py "$glb" "$name" "$size" $axis
  mkdir -p ../models/props && cp "$glb" "../models/props/$name.glb"; done_names+=("$name")
done
[ ${#done_names[@]} -gt 0 ] && $PY propmeta.py "${done_names[@]}"
