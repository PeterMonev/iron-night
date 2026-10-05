#!/bin/bash
# The ninth reference batch (art/refs/batch9, copied to ComfyUI/input/refs as b9_<name>.png): the
# paratroopers, American and Soviet, hanging in their harness, through TRELLIS 2 one at a time.
# Log: trellis-batch9.log
cd "$(dirname "$0")"
until curl -s -m 3 http://127.0.0.1:8189/system_stats >/dev/null; do sleep 5; done
for spec in "para_us:10000" "para_su:10000"; do
  name=b9_${spec%%:*}; faces=${spec##*:}
  if ls "D:/Tools/ComfyUI_windows_portable/ComfyUI/output/3D/${name}_"*.glb >/dev/null 2>&1; then echo "$name: exists, skipped"; continue; fi
  echo "=== $name ($faces faces) $(date +%H:%M:%S)"
  node trellis-run.js "refs/$name.png" "$name" "$faces" 1 || echo "$name FAILED"
done
echo "=== batch done $(date +%H:%M:%S)"
