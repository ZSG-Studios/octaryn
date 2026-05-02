# Octaryn Port Loop Prompt

Use this prompt when starting a new Octaryn port pass.

```text
Get to work on the Octaryn port loop in `/home/zacharyr/octaryn-workspace`.

Use agents efficiently and effectively where parallel inspection or implementation helps. Follow `AGENTS.md`, the canonical master plan in `docs/architecture/octaryn-master-plan.md`, and the supplemental appendix/checklist in `docs/architecture/octaryn-appendix.md`.

Priority:
- `docs/architecture/octaryn-master-plan.md` is the source of truth for architecture, ECS/API direction, module policy, dependencies, validation gates, and phase order.
- `docs/architecture/octaryn-appendix.md` is a supplemental migration checklist and source-to-destination reference.
- If the master plan and appendix conflict, update the appendix to match the master plan.

Goal:
Continue the clean owner-split architecture port with strict separation between:

- `octaryn-client/`
- `octaryn-server/`
- `octaryn-shared/`
- `octaryn-basegame/`
- `tools/`
- `cmake/`

Rules:
- Inspect first, make a brief source-to-destination plan, then execute.
- Keep the codebase clean, modular, current, and easy to navigate.
- Do not create any new `engine/`, `octaryn-engine/`, generic `runtime/`, `common`, `helpers`, `misc`, or catch-all buckets.
- Treat `old-architecture/` as source material only.
- Port behavior into the correct owner with the smallest practical changes.
- Preserve behavior unless a boundary/API change is required.
- Keep client presentation/rendering out of server.
- Keep server authority/persistence/simulation out of client.
- Keep gameplay/content in basegame.
- Keep shared implementation-free and contract/API focused.
- Keep module/game/mod APIs explicit, capability-scoped, and deny-by-default.
- Keep build outputs under `build/<preset>/<owner>/` and logs under `logs/<owner>/`.
- Do not use smoke tests or `ctest`.

Work loop:
1. Inspect current repo state, dirty files, `AGENTS.md`, `docs/architecture/octaryn-master-plan.md`, `docs/architecture/octaryn-appendix.md`, and active validation paths.
2. Identify the highest-value next non-blocked slice from the master plan.
3. Use agents for independent inspection or disjoint implementation work.
4. Make focused edits only.
5. Remove dead, duplicate, temporary, compatibility, or legacy code touched by the task.
6. Validate with targeted builds, owner launch probes, direct runtime checks, or structure validators as appropriate.
7. Report exactly what changed, what was validated, and what remains open.

Before finishing, confirm:
- Client/server/shared/basegame boundaries stayed clean.
- No generic engine/runtime bucket was added.
- Naming is simple and consistent.
- Comments are minimal and useful.
- No unapproved dependencies or module-facing internals were introduced.
- Old-architecture files touched were mapped to explicit destination owners.
- Validation was run where practical, or explain why not.
```
