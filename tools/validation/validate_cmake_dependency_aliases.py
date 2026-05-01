#!/usr/bin/env python3
import argparse
import pathlib
import re
import sys


REQUIRED_ALIASES = {
    "cmake/Dependencies/NativeDependencyAliases.cmake": (
        "octaryn::deps::spdlog",
        "octaryn::deps::cpptrace",
        "octaryn::deps::mimalloc",
        "octaryn::deps::tracy",
        "octaryn::deps::taskflow",
    ),
    "cmake/Dependencies/ClientDependencies.cmake": (
        "octaryn::deps::sdl3",
        "octaryn::deps::openal",
        "octaryn::deps::miniaudio",
        "octaryn::deps::glaze",
    ),
    "cmake/Dependencies/ToolDependencies.cmake": (
        "octaryn::deps::shadercross",
        "octaryn::deps::shaderc",
        "octaryn::deps::spirv_tools",
        "octaryn::deps::spirv_cross",
    ),
}

FORBIDDEN_OWNER_LINKS = (
    "SDL3::",
    "OpenAL::",
    "spdlog::",
    "cpptrace::",
    "Tracy::",
    "Taskflow::",
    "glaze::",
    "SPIRV-",
    "SPIRV_Cross::",
    "SPIRV-Cross::",
    "SDL3_shadercross::",
)


def cmake_command_blocks(text, command):
    pattern = re.compile(rf"{re.escape(command)}\s*\(", re.IGNORECASE)
    for match in pattern.finditer(text):
        start = match.start()
        index = match.end()
        depth = 1
        while index < len(text) and depth > 0:
            if text[index] == "(":
                depth += 1
            elif text[index] == ")":
                depth -= 1
            index += 1
        yield text[start:index]


def validate(repo_root):
    errors = []

    for relative_path, aliases in REQUIRED_ALIASES.items():
        path = repo_root / relative_path
        if not path.exists():
            errors.append(f"{relative_path}: missing dependency module")
            continue
        text = path.read_text(encoding="utf-8")
        for alias in aliases:
            if alias not in text:
                errors.append(f"{relative_path}: missing required dependency alias {alias}")

    owners_dir = repo_root / "cmake" / "Owners"
    for path in sorted(owners_dir.glob("*.cmake")):
        relative_path = path.relative_to(repo_root)
        text = path.read_text(encoding="utf-8")
        for block in cmake_command_blocks(text, "target_link_libraries"):
            for forbidden in FORBIDDEN_OWNER_LINKS:
                if forbidden in block:
                    errors.append(
                        f"{relative_path}: owner target_link_libraries must use octaryn dependency wrappers, not {forbidden}"
                    )

    return errors


def main():
    parser = argparse.ArgumentParser()
    parser.add_argument("--repo-root", default=".")
    args = parser.parse_args()

    errors = validate(pathlib.Path(args.repo_root))
    if errors:
        for error in errors:
            print(f"cmake dependency aliases: {error}", file=sys.stderr)
        return 1
    return 0


if __name__ == "__main__":
    raise SystemExit(main())
