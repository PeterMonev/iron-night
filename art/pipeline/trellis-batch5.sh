#!/bin/bash
# The fifth reference batch (art/refs/batch5, copied to ComfyUI/input/refs as b5_<name>.png): the walking infantry,
# the gun crews and the new map pieces, through TRELLIS 2 one at a time. Log: trellis-batch5.log
cd "$(dirname "$0")"
for spec in "inf_walk_a:10000" "inf_walk_b:10000" "inf_run_a:10000" "inf_run_b:10000" "inf_faust:10000" "inf_rifle_kneel:10000" "inf_mg_prone:10000" "inf_dead:8000" "crew_layer:10000" "crew_loader:10000" "crew_spotter:10000" "crew_ammo:10000" "bridge_stone:12000" "bridge_wood:12000" "barbed_wire:8000" "trench:10000" "house_normandy:16000" "house_ruin:16000" "izba:16000" "windmill:12000" "well:8000" "truck_burnt:14000" "tree_apple:14000" "tree_poplar:10000" "tree_birch:12000"; do
  name=b5_${spec%%:*}; faces=${spec##*:}
  if ls "D:/Tools/ComfyUI_windows_portable/ComfyUI/output/3D/${name}_"*.glb >/dev/null 2>&1; then echo "$name: exists, skipped"; continue; fi
  echo "=== $name ($faces faces) $(date +%H:%M:%S)"
  node trellis-run.js "refs/$name.png" "$name" "$faces" 1 || echo "$name FAILED"
done
echo "=== batch done $(date +%H:%M:%S)"
