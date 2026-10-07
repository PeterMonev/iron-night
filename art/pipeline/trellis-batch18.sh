#!/bin/bash
# The eighteenth reference batch (art/refs/batch18, copied to ComfyUI/input/refs as b18_<name>.png): the
# hangar crews in a T-pose, to be rigged in Mixamo and given real animations, through TRELLIS 2 one at a time.
# Log: trellis-batch18.log
cd "$(dirname "$0")"
until curl -s -m 3 http://127.0.0.1:8189/system_stats >/dev/null; do sleep 5; done
for spec in "rig_su_driver_4:20000" "rig_su_gunner:20000" "rig_su_gunner_4:20000" "rig_su_loader_4:20000" "rig_su_radio_4:20000" "rig_us_driver:20000" "rig_us_driver_4:20000" "rig_us_gunner:20000" "rig_us_gunner_4:20000" "rig_us_loader:20000" "rig_us_loader_4:20000" "rig_us_radio:20000" "rig_us_radio_4:20000"; do
  name=b18_${spec%%:*}; faces=${spec##*:}
  if ls "D:/Tools/ComfyUI_windows_portable/ComfyUI/output/3D/${name}_"*.glb >/dev/null 2>&1; then echo "$name: exists, skipped"; continue; fi
  echo "=== $name ($faces faces) $(date +%H:%M:%S)"
  node trellis-run.js "refs/$name.png" "$name" "$faces" 1 || echo "$name FAILED"
done
echo "=== batch done $(date +%H:%M:%S)"
