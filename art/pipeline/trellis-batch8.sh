#!/bin/bash
# The eighth reference batch (art/refs/batch8, copied to ComfyUI/input/refs as b8_<name>.png): our own infantry for the
# fire fights, American and Soviet, kneeling, standing and behind a machine gun, through TRELLIS 2 one at a time.
# Log: trellis-batch8.log
cd "$(dirname "$0")"
until curl -s -m 3 http://127.0.0.1:8189/system_stats >/dev/null; do sleep 5; done
for spec in "ally_us_kneel:10000" "ally_us_stand:10000" "ally_us_prone:10000" "ally_su_kneel:10000" "ally_su_stand:10000" "ally_su_prone:10000"; do
  name=b8_${spec%%:*}; faces=${spec##*:}
  if ls "D:/Tools/ComfyUI_windows_portable/ComfyUI/output/3D/${name}_"*.glb >/dev/null 2>&1; then echo "$name: exists, skipped"; continue; fi
  echo "=== $name ($faces faces) $(date +%H:%M:%S)"
  node trellis-run.js "refs/$name.png" "$name" "$faces" 1 || echo "$name FAILED"
done
echo "=== batch done $(date +%H:%M:%S)"
