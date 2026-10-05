#!/bin/bash
# The tenth reference batch (art/refs/batch10, copied to ComfyUI/input/refs as b10_<name>.png): the
# anti-tank men for the last stand sandbags, a bazooka kneeling and a PTRD prone, through TRELLIS 2 one at a time.
# Log: trellis-batch10.log
cd "$(dirname "$0")"
until curl -s -m 3 http://127.0.0.1:8189/system_stats >/dev/null; do sleep 5; done
for spec in "ally_us_bazooka:10000" "ally_su_ptrd:10000"; do
  name=b10_${spec%%:*}; faces=${spec##*:}
  if ls "D:/Tools/ComfyUI_windows_portable/ComfyUI/output/3D/${name}_"*.glb >/dev/null 2>&1; then echo "$name: exists, skipped"; continue; fi
  echo "=== $name ($faces faces) $(date +%H:%M:%S)"
  node trellis-run.js "refs/$name.png" "$name" "$faces" 1 || echo "$name FAILED"
done
echo "=== batch done $(date +%H:%M:%S)"
