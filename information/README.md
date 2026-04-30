# Octaryn Engine

Octaryn Engine is a root-level SDL3 GPU voxel engine with a modern C++ runtime, generated shader assets, Taskflow-driven world jobs, and JSON-backed persistence.

## Active Layout

- `octaryn-engine/`: native engine project, CMake presets, runtime source, shaders, and engine icons
- `octaryn-engine/source/app/`, `octaryn-engine/source/core/`, `octaryn-engine/source/render/`, `octaryn-engine/source/world/`: active runtime source
- `octaryn-engine/source/shaders/`: shader sources, with active runtime stages authored in GLSL
- `octaryn-engine/source/api/`: C# engine API project
- `octaryn-basegame/`: C# base game project
- `octaryn-client/`: client project root
- `octaryn-server/`: server project root
- `cmake/`: workspace CMake modules and toolchains
- `tools/`: workspace-level build, asset, profiling, and control tools
- `tools/build/`: configure/build helper scripts

## Build

Fresh Linux host setup:

```sh
./tools/build/install_deps.sh --yes
```

Build Linux and Windows debug profiles:

```sh
./tools/build/build_all.sh
```

Install packages first, then build Linux and Windows:

```sh
./tools/build/build_all.sh --install
```

Build Linux and Windows release profiles too:

```sh
./tools/build/build_all.sh --release
```

Helper-script Linux workflow:

```sh
./tools/build/configure.sh --preset linux-debug
./tools/build/cmake_build.sh --preset linux-debug --target octaryn_engine_runtime_bundle
```

Windows cross-build from Linux:

```sh
./tools/build/configure.sh --preset windows-debug
./tools/build/cmake_build.sh --preset windows-debug --target octaryn_engine_runtime
```

Raw CMake workflow:

```sh
cmake --preset linux-debug
cmake --build --preset linux-debug --target octaryn_engine_runtime_bundle
```

## Shader Assets

- Active runtime shader sources use explicit GLSL suffixes such as `*.vert.glsl`, `*.frag.glsl`, and `*.comp.glsl`
- Checked-in runtime shader sources now live on the GLSL path only
- Generated runtime assets are built by `octaryn_engine_shader_assets`
- The offline compiler is `octaryn-engine-shader-tool`
- Generated shader outputs are staged into `assets/shaders/` next to the executable
- Runtime textures are generated and staged into `assets/textures/`
- Generated shader blobs are build outputs, not checked-in runtime artifacts

## Texture Assets

- The runtime block atlas is generated from Pixlli at build time and cached under `.octaryn-cache/texture-packs/`
- Generated texture outputs are `atlas.png`, `atlas_n.png`, and `atlas_s.png`
- `atlas.png` is the sRGB albedo atlas
- `atlas_n.png` preserves Pixlli `_n` normal/height data for PBR and POM
- `atlas_s.png` preserves Pixlli `_s` specular/material data for PBR
- Use `-DOCTARYN_TEXTURE_PACK_PATH=/path/to/pack.zip` to build from a local pack instead of downloading
- Use `-DOCTARYN_TEXTURE_PACK_SHA256=<hash>` to enforce a specific pack payload

## Persistence

Runtime saves are written under the configured save root as:

- `settings.json`
- `player_0.json`
- `world/chunk_<cx>_<cz>.json`

## Notes

- Product outputs live under `build/octaryn-engine/<preset>/`
- Runtime binaries live under `build/octaryn-engine/<preset>/bin`
- Runtime save/output data for scripted runs lives under `build/octaryn-engine/<preset>/runtime`
- Build logs live under `build/octaryn-engine/<preset>/logs`
- CMake workspace files live under `build/shared/workspace/<preset>/cmake`
- Shared third-party source and build caches live under `build/shared/`
- `shaderc` is used for explicit GLSL shader compilation in the offline shader tool
- `SDL3_shadercross` is used for shader reflection and MSL transpilation in the offline shader tool
- Runtime asset lookup is rooted under `assets/`

## License

Octaryn Engine is licensed under the MIT License. See `LICENSE`.
