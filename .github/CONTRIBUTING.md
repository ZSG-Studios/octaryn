# Contributing to Octaryn

Octaryn is a ZSG Studios engine project. Keep contributions focused, reproducible, and aligned with the current architecture.

## Ground Rules

- Use clear names for files, folders, types, functions, and tests.
- Keep changes scoped to one purpose.
- Do not add legacy compatibility layers, fallback systems, or deprecated APIs unless a maintainer asks for them.
- Preserve the separation between `octaryn-client`, `octaryn-server`, `octaryn-basegame`, `octaryn-shared`, tools, docs, build, and validation.
- Do not commit generated build output, logs, local secrets, or temporary files.
- Do not submit copyrighted code or assets from projects that ZSG Studios cannot use.

## Bugs

Bug reports should include:

- Build or commit tested.
- Operating system and graphics/API details when relevant.
- Steps to reproduce.
- Expected result.
- Actual result.
- Logs, screenshots, captures, or small test data when useful.

## Pull Requests

Before opening a PR:

- Keep the branch up to date with `main`.
- Run the most relevant build or validation checks for the touched area.
- Explain what changed and why.
- Link related issues when possible.
- Call out any known gaps or follow-up work.

## Reviews

Reviews should focus on correctness, architecture boundaries, performance, maintainability, and test coverage. Resolve review threads when the requested change or answer is complete.

