#!/bin/bash
# The fifth batch from TRELLIS into Resources/Props: the soldiers and gun crews as figures standing on the ground facing
# +Z at their real height (figure_export.py), the machine gunner lying down turned to face along his gun, the dead man by his length, the rest as props
# (prop_export.py), then their textures' import settings (propmeta.py). Skips what TRELLIS has not finished.
cd "$(dirname "$0")"; O=D:/Tools/ComfyUI_windows_portable/ComfyUI/output/3D; PY=D:/Tools/ComfyUI_windows_portable/python_trellis/python.exe
done_names=()
for spec in "inf_walk_a:fig:1.8" "inf_walk_b:fig:1.8" "inf_run_a:fig:1.75" "inf_run_b:fig:1.75" "inf_faust:fig:1.35" "inf_rifle_kneel:fig:1.3" \
            "crew_layer:fig:1.35" "crew_loader:fig:1.8" "crew_spotter:fig:1.35" "crew_ammo:fig:1.3" \
            "inf_mg_prone:fig:0.75:turn=50" "inf_dead:prop:1.85" \
            "house_normandy:prop:11" "house_ruin:prop:11" "izba:prop:10" "windmill:prop:13:y" "well_b:prop:3:y:well" "truck_burnt:prop:6.5" \
            "barbed_wire:prop:6:turn=45" "trench:prop:8" "bridge_stone:prop:16" "bridge_wood:prop:14" \
            "tree_apple:prop:6:y" "tree_birch:prop:14:y" "tree_poplar_b:prop:18:y:tree_poplar"; do
  IFS=: read -r name how size axis src <<< "$spec"
  glb=$(ls "$O/b5_${src:-$name}_"*.glb 2>/dev/null | tail -1); [ -z "$glb" ] && { echo "$name: not rendered yet"; continue; }
  if [ "$how" = fig ]; then $PY figure_export.py "$glb" "$name" "$size" stand raw $axis; else $PY prop_export.py "$glb" "$name" "$size" $axis; fi
  mkdir -p ../models/props && cp "$glb" "../models/props/$name.glb"; done_names+=("$name")
done
[ ${#done_names[@]} -gt 0 ] && $PY propmeta.py "${done_names[@]}"
