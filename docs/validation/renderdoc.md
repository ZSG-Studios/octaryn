# RenderDoc Validation

RenderDoc validation applies to client rendering work.

## Target Use

- Capture frame setup, swapchain/presentation, terrain passes, shader resources, and GPU uploads.
- Use captures when changing render pipelines, shader reflection, GPU buffer layout, atlas binding, or mesh upload.

## Active Tooling

RenderDoc is a first-class debug tool. Debug presets stage the wrapper through `octaryn_debug_tools` into `build/<preset>/tools/capture/renderdoc_tool.sh`, with the source wrapper kept at `tools/capture/renderdoc_tool.sh`.

```sh
tools/build/cmake_build.sh debug-linux --target octaryn_debug_tools
tools/capture/renderdoc_tool.sh --preset debug-linux build
tools/capture/renderdoc_tool.sh --preset debug-linux launch
tools/capture/renderdoc_tool.sh --preset debug-linux --program build/debug-linux/client/native/bin/octaryn_client_launch_probe capture
```

The wrapper fetches/builds project-local RenderDoc from the workspace-managed dependency cache, uses all available cores, registers the user Vulkan layer when needed, writes logs under `logs/tools/renderdoc_tool.log`, and writes capture artifacts under `logs/tools/renderdoc/`. Capture intentionally runs with X11/Xwayland environment variables because this path is for RenderDoc Vulkan capture, not normal client launch.
