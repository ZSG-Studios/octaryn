#!/usr/bin/env python3
import argparse
import pathlib
import re
import sys


INCLUDE_PATTERN = re.compile(r'^\s*#\s*include\s+[<"]([^>"]+)[>"]')
OWNER_ROOTS = {
    "client": pathlib.Path("octaryn-client/Source/Native"),
    "server": pathlib.Path("octaryn-server/Source/Native"),
}
CMAKE_OWNER_FILES = {
    "client": pathlib.Path("cmake/Owners/ClientTargets.cmake"),
    "server": pathlib.Path("cmake/Owners/ServerTargets.cmake"),
}
FORBIDDEN_OWNER_TOKENS = {
    "client": ("octaryn-server/", "ServerHostAbi", "octaryn_server_"),
    "server": ("octaryn-client/", "ClientHostAbi", "octaryn_client_"),
}
FORBIDDEN_OWNER_SYMBOLS = {
    "client": ("octaryn_server_",),
    "server": ("octaryn_client_",),
}
ALLOWED_SHARED_SYMBOLS = {
    "client": ("octaryn_server_snapshot_header",),
    "server": ("octaryn_client_command_frame",),
}
ALLOWED_OWNER_SOURCE_ROOTS = {
    "client": (
        "octaryn-client/Source/Native/",
        "octaryn-shared/Source/Native/HostAbi/",
    ),
    "server": (
        "octaryn-server/Source/Native/",
        "octaryn-shared/Source/Native/HostAbi/",
    ),
}
ALLOWED_SHARED_TOKENS = ("octaryn-shared/", "HostAbi", "octaryn_host_abi", "octaryn_shared_abi")
QUOTED_WORKSPACE_PATH_PATTERN = re.compile(r'"(?:\$\{OCTARYN_WORKSPACE_ROOT_DIR\}/)?([^"]+)"')


def validate(repo_root):
    errors = []
    for owner, root in OWNER_ROOTS.items():
        owner_root = repo_root / root
        if owner_root.exists():
            for path in sorted(owner_root.rglob("*")):
                if path.suffix.lower() not in (".c", ".h", ".cpp", ".hpp"):
                    continue
                errors.extend(validate_source_file(repo_root, owner, path))

        cmake_file = repo_root / CMAKE_OWNER_FILES[owner]
        if cmake_file.exists():
            errors.extend(validate_cmake_file(repo_root, owner, cmake_file))

    return errors


def validate_source_file(repo_root, owner, path):
    errors = []
    text = path.read_text(encoding="utf-8")
    for line_number, line in enumerate(text.splitlines(), start=1):
        match = INCLUDE_PATTERN.match(line)
        if not match:
            continue
        include = match.group(1)
        for token in FORBIDDEN_OWNER_TOKENS[owner]:
            if token in include:
                relative = path.relative_to(repo_root)
                errors.append(f"{relative}:{line_number}: {owner} native source includes forbidden owner token {token}: {include}")

    for token in FORBIDDEN_OWNER_SYMBOLS[owner]:
        for match in re.finditer(rf"\b{re.escape(token)}[A-Za-z0-9_]*\b", text):
            if match.group(0) in ALLOWED_SHARED_SYMBOLS[owner]:
                continue
            line_number = text.count("\n", 0, match.start()) + 1
            relative = path.relative_to(repo_root)
            errors.append(f"{relative}:{line_number}: {owner} native source references forbidden owner symbol {match.group(0)}")
    return errors


def validate_cmake_file(repo_root, owner, path):
    errors = []
    text = path.read_text(encoding="utf-8")
    for token in FORBIDDEN_OWNER_TOKENS[owner]:
        if token in text:
            relative = path.relative_to(repo_root)
            errors.append(f"{relative}: {owner} CMake references forbidden owner token {token}")

    if not any(token in text for token in ALLOWED_SHARED_TOKENS):
        relative = path.relative_to(repo_root)
        errors.append(f"{relative}: {owner} CMake target must explicitly reference shared host ABI include/link surface")

    errors.extend(validate_cmake_source_paths(repo_root, owner, path, text))

    return errors


def validate_cmake_source_paths(repo_root, owner, path, text):
    errors = []
    relative_cmake = path.relative_to(repo_root)
    allowed_roots = ALLOWED_OWNER_SOURCE_ROOTS[owner]
    in_sources = False

    for line_number, line in enumerate(text.splitlines(), start=1):
        stripped = line.strip()
        if stripped == "SOURCES":
            in_sources = True
            continue

        if in_sources and stripped in ("PUBLIC_INCLUDE_DIRS", "PRIVATE_LINKS"):
            in_sources = False

        if not in_sources:
            continue

        for match in QUOTED_WORKSPACE_PATH_PATTERN.finditer(line):
            referenced = match.group(1)
            if referenced.startswith("${"):
                continue
            if not any(referenced.startswith(root) for root in allowed_roots):
                errors.append(
                    f"{relative_cmake}:{line_number}: {owner} native target source must live under {allowed_roots}, got {referenced}")

    return errors


def main():
    parser = argparse.ArgumentParser()
    parser.add_argument("--repo-root", default=".")
    args = parser.parse_args()

    errors = validate(pathlib.Path(args.repo_root).resolve())
    if errors:
        for error in errors:
            print(f"native owner boundary policy: {error}", file=sys.stderr)
        return 1
    return 0


if __name__ == "__main__":
    raise SystemExit(main())
