#!/bin/bash
# Exports every finished prop of the second batch into the Unity project (skips the ones not rendered yet).
cd "$(dirname "$0")"; O=D:/Tools/ComfyUI_windows_portable/ComfyUI/output/3D; PY=D:/Tools/ComfyUI_windows_portable/python_trellis/python.exe
for spec in "barn:12" "truck:6" "haystack:2.4" "deadtree:8:y" "sandbags:7" "cottage:9" "church:22" "wall_a:6" "wall_b:6" "cart:3.5" "crate:1.6" "pole:8:y"; do
  IFS=: read -r name size axis <<< "$spec"
  glb=$(ls "$O/${name}_"*.glb 2>/dev/null | tail -1); [ -z "$glb" ] && { echo "$name: not rendered yet"; continue; }
  [ -f "../models/props/$name.glb" ] && [ "../models/props/$name.glb" -nt "$glb" ] && { echo "$name: up to date"; continue; }
  $PY prop_export.py "$glb" "$name" "$size" $axis && cp "$glb" "../models/props/$name.glb"
done
