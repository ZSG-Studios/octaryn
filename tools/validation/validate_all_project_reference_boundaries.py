#!/usr/bin/env python3
import argparse
import pathlib
import sys

import validate_project_reference_boundaries


ACTIVE_PROJECT_ROOTS = (
    "octaryn-shared",
    "octaryn-basegame",
    "octaryn-client",
    "octaryn-server",
    "octaryn-games",
    "octaryn-modules",
    "octaryn-mods",
    "tools/validation",
)

EXCLUDED_PATH_PARTS = {
    "build",
    "old-architecture",
    "bin",
    "obj",
}


def discover_project_files(repo_root):
    projects = []
    for root_name in ACTIVE_PROJECT_ROOTS:
        root = repo_root / root_name
        if not root.exists():
            continue

        for project_file in root.rglob("*.csproj"):
            relative_parts = project_file.relative_to(repo_root).parts
            if any(part in EXCLUDED_PATH_PARTS for part in relative_parts):
                continue

            projects.append(project_file)

    return sorted(projects, key=lambda path: path.relative_to(repo_root).as_posix())


def main():
    parser = argparse.ArgumentParser()
    parser.add_argument("--repo-root", default=".")
    args = parser.parse_args()

    repo_root = pathlib.Path(args.repo_root).resolve()
    errors = []
    projects = discover_project_files(repo_root)
    if not projects:
        print("project reference boundary policy: no active .csproj files discovered", file=sys.stderr)
        return 1

    for project_file in projects:
        try:
            errors.extend(validate_project_reference_boundaries.validate(repo_root, project_file))
        except RuntimeError as error:
            errors.append(str(error))

    if errors:
        for error in errors:
            print(f"project reference boundary policy: {error}", file=sys.stderr)
        return 1

    return 0


if __name__ == "__main__":
    raise SystemExit(main())
