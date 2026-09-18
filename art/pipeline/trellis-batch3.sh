#!/bin/bash
# The third reference batch through TRELLIS 2, one model at a time. Log: trellis-batch3.log
cd "$(dirname "$0")"
for spec in "sherman_turret:8000" "easy8:20000" "hellcat:20000" "chaffee:20000" "pershing:20000" "t34_85:20000" "kv85:20000" "su100:20000" "is2:20000" "firefly:20000" "m10:20000" "hetzer:20000" "kingtiger:20000" "nebelwerfer:14000" "flak38:14000" "kubelwagen:14000" "c47:16000" "tree_oak:16000" "tree_poplar:10000" "hedge:12000" "spruce_snow:14000" "bunker:16000" "barrels:8000" "well:8000" "gate:8000" "signpost:5000" "soldier_b:12000" "commander:10000" "wreck:20000" "marker_smoke:5000"; do
  name=${spec%%:*}; faces=${spec##*:}
  if ls "D:/Tools/ComfyUI_windows_portable/ComfyUI/output/3D/${name}_"*.glb >/dev/null 2>&1; then echo "$name: exists, skipped"; continue; fi
  echo "=== $name ($faces faces) $(date +%H:%M:%S)"
  node trellis-run.js "refs/$name.png" "$name" "$faces" 1 || echo "$name FAILED"
done
echo "=== batch done $(date +%H:%M:%S)"
