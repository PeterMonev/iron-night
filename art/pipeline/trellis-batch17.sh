#!/bin/bash
# The seventeenth reference batch (art/refs/batch17, copied to ComfyUI/input/refs as b17_<name>.png): the
# men walking about the hangar, two strides each: a mechanic with a toolbox, a man with a shell crate, through TRELLIS 2 one at a time.
# Log: trellis-batch17.log
cd "$(dirname "$0")"
until curl -s -m 3 http://127.0.0.1:8189/system_stats >/dev/null; do sleep 5; done
for spec in "walk_us_mech_a:10000" "walk_us_mech_b:10000" "walk_us_crate_a:10000" "walk_us_crate_b:10000" "walk_su_mech_a:10000" "walk_su_mech_b:10000" "walk_su_crate_a:10000" "walk_su_crate_b:10000"; do
  name=b17_${spec%%:*}; faces=${spec##*:}
  if ls "D:/Tools/ComfyUI_windows_portable/ComfyUI/output/3D/${name}_"*.glb >/dev/null 2>&1; then echo "$name: exists, skipped"; continue; fi
  echo "=== $name ($faces faces) $(date +%H:%M:%S)"
  node trellis-run.js "refs/$name.png" "$name" "$faces" 1 || echo "$name FAILED"
done
echo "=== batch done $(date +%H:%M:%S)"
