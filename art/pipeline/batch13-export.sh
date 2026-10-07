#!/bin/bash
# The thirteenth batch (the hangar crews at ease) from TRELLIS into Resources/Props: figures facing +Z at their real
# height (figure_export.py; sitting on a crate and crouching ones lower), the textures' import settings
# (propmeta.py), and each OBJ readable like the crew figures before them (CrewIdle moves the mesh itself).
cd "$(dirname "$0")"; O=D:/Tools/ComfyUI_windows_portable/ComfyUI/output/3D; PY=D:/Tools/ComfyUI_windows_portable/python_trellis/python.exe
P=../../unity/Assets/_Game/Resources/Props
done_names=()
for spec in "crew_us_smoke:1.8" "crew_us_map:1.45" "crew_us_rag:1.8" "crew_us_mess:1.15" "crew_su_smoke:1.8" "crew_su_map:1.45" "crew_su_rag:1.8" "crew_su_mess:1.15" \
            "crew_us_gunner_4_idle:1.42" "crew_us_loader_4_idle:1.72" "crew_us_driver_4_idle:1.72" "crew_us_radio_4_idle:1.72" \
            "crew_su_gunner_4_idle:1.42" "crew_su_loader_4_idle:1.72" "crew_su_driver_4_idle:1.75" "crew_su_radio_4_idle:1.12"; do
  IFS=: read -r name height <<< "$spec"
  glb=$(ls "$O/b13_${name}_"*.glb 2>/dev/null | tail -1); [ -z "$glb" ] && { echo "$name: not rendered yet"; continue; }
  $PY figure_export.py "$glb" "$name" "$height" stand raw
  cp "$glb" "../models/props/$name.glb"; done_names+=("$name")
done
[ ${#done_names[@]} -gt 0 ] && $PY propmeta.py "${done_names[@]}"
# the mesh readable, as crew_us_gunner's: its import settings with a new guid (only where Unity has not made one yet)
for n in "${done_names[@]}"; do
  [ -f "$P/$n.obj.meta" ] && continue
  sed "0,/guid: [0-9a-f]\{32\}/s//guid: $($PY -c 'import uuid;print(uuid.uuid4().hex)')/" "$P/crew_us_gunner.obj.meta" > "$P/$n.obj.meta"
done
