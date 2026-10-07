#!/bin/bash
# The seventeenth batch (the hangar walkers, two strides each) from TRELLIS into Resources/Props: figures facing +Z at their real
# height (figure_export.py; sitting on a crate and crouching ones lower), the textures' import settings
# (propmeta.py), and each OBJ readable like the crew figures before them (CrewIdle moves the mesh itself).
cd "$(dirname "$0")"; O=D:/Tools/ComfyUI_windows_portable/ComfyUI/output/3D; PY=D:/Tools/ComfyUI_windows_portable/python_trellis/python.exe
P=../../unity/Assets/_Game/Resources/Props
done_names=()
for spec in "walk_us_mech_a:1.8" "walk_us_mech_b:1.8" "walk_us_crate_a:1.8" "walk_us_crate_b:1.8" "walk_su_mech_a:1.8" "walk_su_mech_b:1.8" "walk_su_crate_a:1.8" "walk_su_crate_b:1.8"; do
  IFS=: read -r name height <<< "$spec"
  glb=$(ls "$O/b17_${name}_"*.glb 2>/dev/null | tail -1); [ -z "$glb" ] && { echo "$name: not rendered yet"; continue; }
  $PY figure_export.py "$glb" "$name" "$height" stand raw
  cp "$glb" "../models/props/$name.glb"; done_names+=("$name")
done
[ ${#done_names[@]} -gt 0 ] && $PY propmeta.py "${done_names[@]}"
# the mesh readable, as crew_us_gunner's: its import settings with a new guid (only where Unity has not made one yet)
for n in "${done_names[@]}"; do
  [ -f "$P/$n.obj.meta" ] && continue
  sed "0,/guid: [0-9a-f]\{32\}/s//guid: $($PY -c 'import uuid;print(uuid.uuid4().hex)')/" "$P/crew_us_gunner.obj.meta" > "$P/$n.obj.meta"
done
