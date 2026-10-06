#!/bin/bash
# The eleventh reference batch (art/refs/batch11, copied to ComfyUI/input/refs as b11_<name>.png): the
# Italian front: houses, a farmhouse, a church, a ruin, a dry stone wall, a well, an olive and a cypress, through TRELLIS 2 one at a time.
# Log: trellis-batch11.log
cd "$(dirname "$0")"
until curl -s -m 3 http://127.0.0.1:8189/system_stats >/dev/null; do sleep 5; done
for spec in "it_house:20000" "it_farmhouse:20000" "it_church:24000" "it_ruin:16000" "it_wall:8000" "it_well:8000" "it_olive:20000" "it_cypress:16000"; do
  name=b11_${spec%%:*}; faces=${spec##*:}
  if ls "D:/Tools/ComfyUI_windows_portable/ComfyUI/output/3D/${name}_"*.glb >/dev/null 2>&1; then echo "$name: exists, skipped"; continue; fi
  echo "=== $name ($faces faces) $(date +%H:%M:%S)"
  node trellis-run.js "refs/$name.png" "$name" "$faces" 1 || echo "$name FAILED"
done
echo "=== batch done $(date +%H:%M:%S)"
