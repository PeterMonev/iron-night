#!/bin/bash
# Exports the third batch's props into the Unity project (skips the ones not rendered yet).
cd "$(dirname "$0")"; O=D:/Tools/ComfyUI_windows_portable/ComfyUI/output/3D; PY=D:/Tools/ComfyUI_windows_portable/python_trellis/python.exe
for spec in "tree_oak:12:y" "tree_poplar:16:y" "hedge:8" "spruce_snow:12:y" "bunker:6" "barrels:2" "well:2.5" "gate:3.5" "signpost:3:y" "soldier_b:1.3:y" "commander:0.95:y" "wreck:6" "marker_smoke:1.2:y"; do
  IFS=: read -r name size axis <<< "$spec"
  glb=$(ls "$O/${name}_"*.glb 2>/dev/null | tail -1); [ -z "$glb" ] && { echo "$name: not rendered yet"; continue; }
  [ -f "../models/props/$name.glb" ] && [ "../models/props/$name.glb" -nt "$glb" ] && { echo "$name: up to date"; continue; }
  $PY prop_export.py "$glb" "$name" "$size" $axis && cp "$glb" "../models/props/$name.glb"
done
