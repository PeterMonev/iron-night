#!/bin/bash
# The sixth batch from TRELLIS into Resources/Props (prop_export.py: long axis along Z, on the ground, at its real size;
# "y" sizes by height; turn=<deg> straightens a model TRELLIS left on the diagonal), then the textures' import settings
# (propmeta.py). Skips what TRELLIS has not finished.
cd "$(dirname "$0")"; O=D:/Tools/ComfyUI_windows_portable/ComfyUI/output/3D; PY=D:/Tools/ComfyUI_windows_portable/python_trellis/python.exe
done_names=()
for spec in "house_belgian:10" "farm_belgian:15" "sawmill:10" "foxhole_logs:5" "truck_snow:6.5" "chapel_wayside:6:y" \
            "fir_snow:22:y" "fir_snow_b:16:y" "pine_snow:20:y" "fir_young:3:y"; do
  IFS=: read -r name size axis <<< "$spec"
  glb=$(ls "$O/b6_${name}_"*.glb 2>/dev/null | tail -1); [ -z "$glb" ] && { echo "$name: not rendered yet"; continue; }
  $PY prop_export.py "$glb" "$name" "$size" $axis
  mkdir -p ../models/props && cp "$glb" "../models/props/$name.glb"; done_names+=("$name")
done
[ ${#done_names[@]} -gt 0 ] && $PY propmeta.py "${done_names[@]}"
