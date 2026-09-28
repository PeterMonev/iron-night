#!/bin/bash
# The hangar's props from ChatGPT pictures (art/refs/hangar, copied into ComfyUI/input/refs) through TRELLIS on 8189,
# one after another; then each exported into Resources/Props at its real size. A model already rendered is skipped.
cd "$(dirname "$0")"; O=D:/Tools/ComfyUI_windows_portable/ComfyUI/output/3D; PY=D:/Tools/ComfyUI_windows_portable/python_trellis/python.exe
# name : picture : faces : size in metres : y (size is the height, not turned)
for spec in "mechanic:ref_mechanic:30000:1.3:y" "weldcart:ref_weldcart:20000:1.45:y" "workbench:ref_workbench:24000:1.9" \
            "engine_radial:ref_aircraft_engine:24000:1.65:y" "engine_inline:ref_engine:24000:1.9" "lamp:ref_lamp:12000:1.0:y" "wheels:ref_wheels:20000:1.3"; do
  IFS=: read -r name pic faces size axis <<< "$spec"
  if ! ls "$O/${name}_"*.glb >/dev/null 2>&1; then node trellis-run.js "refs/$pic.png" "$name" "$faces" 1 || { echo "$name: failed"; continue; }; fi
  glb=$(ls "$O/${name}_"*.glb | tail -1)
  $PY prop_export.py "$glb" "$name" "$size" $axis && mkdir -p ../models/props && cp "$glb" "../models/props/$name.glb" && echo "$name: exported"
done
