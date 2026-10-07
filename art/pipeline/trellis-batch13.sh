#!/bin/bash
# The thirteenth reference batch (art/refs/batch13, copied to ComfyUI/input/refs as b13_<name>.png): the
# hangar crews at ease: smoking, a map on a crate, a rag, a mess tin; and the eight women aces in their own poses, through TRELLIS 2 one at a time.
# Log: trellis-batch13.log
cd "$(dirname "$0")"
until curl -s -m 3 http://127.0.0.1:8189/system_stats >/dev/null; do sleep 5; done
for spec in "crew_us_smoke:12000" "crew_us_map:12000" "crew_us_rag:12000" "crew_us_mess:12000" "crew_su_smoke:12000" "crew_su_map:12000" "crew_su_rag:12000" "crew_su_mess:12000" "crew_us_gunner_4_idle:12000" "crew_us_loader_4_idle:12000" "crew_us_driver_4_idle:12000" "crew_us_radio_4_idle:12000" "crew_su_gunner_4_idle:12000" "crew_su_loader_4_idle:12000" "crew_su_driver_4_idle:12000" "crew_su_radio_4_idle:12000"; do
  name=b13_${spec%%:*}; faces=${spec##*:}
  if ls "D:/Tools/ComfyUI_windows_portable/ComfyUI/output/3D/${name}_"*.glb >/dev/null 2>&1; then echo "$name: exists, skipped"; continue; fi
  echo "=== $name ($faces faces) $(date +%H:%M:%S)"
  node trellis-run.js "refs/$name.png" "$name" "$faces" 1 || echo "$name FAILED"
done
echo "=== batch done $(date +%H:%M:%S)"
