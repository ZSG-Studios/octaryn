# Shader Migration

Shader sources and shader asset generation are client/tool concerns.

## Rules

- Runtime shader sources move to `octaryn-client/Shaders/`.
- Generated shader outputs are build artifacts and stay ignored.
- The shader compiler belongs under root tools when ported, not inside basegame.
- Basegame may own content shader inputs only when they are default-game assets.

## Old Targets

| Old target | Destination |
| --- | --- |
| `octaryn_engine_shader_tool` | root shader tool target |
| `octaryn_engine_shader_assets` | client shader asset generation |
| generated shader staging into old runtime dir | client bundle staging |
