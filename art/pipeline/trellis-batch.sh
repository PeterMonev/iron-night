#!/bin/bash
# Runs the second reference batch through TRELLIS 2 one model at a time (the GPU holds one job). Log: trellis-batch.log
cd "$(dirname "$0")"
for spec in "panther:20000" "stug:20000" "halftrack:20000" "flak88:16000" "searchlight:14000" "soldier:12000" "barn:24000" "truck:16000" "haystack:8000" "deadtree:14000" "sandbags:12000" "cottage:24000" "church:30000" "wall_a:10000" "wall_b:10000" "cart:12000" "crate:5000" "pole:5000"; do
  name=${spec%%:*}; faces=${spec##*:}
  if ls "D:/Tools/ComfyUI_windows_portable/ComfyUI/output/3D/${name}_"*.glb >/dev/null 2>&1; then echo "$name: exists, skipped"; continue; fi
  echo "=== $name ($faces faces) $(date +%H:%M:%S)"
  node trellis-run.js "refs/$name.png" "$name" "$faces" 1 || echo "$name FAILED"
done
echo "=== batch done $(date +%H:%M:%S)"
