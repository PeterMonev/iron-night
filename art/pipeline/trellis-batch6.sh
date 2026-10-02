#!/bin/bash
# The sixth reference batch (art/refs/batch6, copied to ComfyUI/input/refs as b6_<name>.png): the Ardennes' stone
# houses, sawmill, log-roofed foxhole, snowed-in lorry, wayside chapel and its firs and pine, through TRELLIS 2 one at a
# time. Log: trellis-batch6.log
cd "$(dirname "$0")"
until curl -s -m 3 http://127.0.0.1:8189/system_stats >/dev/null; do sleep 5; done
for spec in "house_belgian:16000" "farm_belgian:16000" "sawmill:14000" "foxhole_logs:10000" "truck_snow:14000" "chapel_wayside:10000" "fir_snow:12000" "fir_snow_b:12000" "pine_snow:10000" "fir_young:8000"; do
  name=b6_${spec%%:*}; faces=${spec##*:}
  if ls "D:/Tools/ComfyUI_windows_portable/ComfyUI/output/3D/${name}_"*.glb >/dev/null 2>&1; then echo "$name: exists, skipped"; continue; fi
  echo "=== $name ($faces faces) $(date +%H:%M:%S)"
  node trellis-run.js "refs/$name.png" "$name" "$faces" 1 || echo "$name FAILED"
done
echo "=== batch done $(date +%H:%M:%S)"
