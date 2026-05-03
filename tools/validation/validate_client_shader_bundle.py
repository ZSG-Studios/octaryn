#!/usr/bin/env python3
import argparse
import filecmp
import pathlib
import sys


def relative_files(root):
    return {
        path.relative_to(root).as_posix()
        for path in root.rglob("*")
        if path.is_file() and path.name != ".gitkeep"
    }


def validate(source_root, bundle_shader_root):
    errors = []

    if not source_root.exists():
        errors.append(f"{source_root}: client shader source root is missing")
    if not bundle_shader_root.exists():
        errors.append(f"{bundle_shader_root}: client bundle shader root is missing")
    if errors:
        return errors

    source_files = relative_files(source_root)
    bundled_files = relative_files(bundle_shader_root)
    if not source_files:
        errors.append(f"{source_root}: client shader source root has no shader files")

    missing = sorted(source_files - bundled_files)
    extra = sorted(bundled_files - source_files)
    if missing:
        errors.append(f"{bundle_shader_root}: missing client-owned shader files: {missing}")
    if extra:
        errors.append(f"{bundle_shader_root}: contains undeclared client shader files: {extra}")

    for relative in sorted(source_files & bundled_files):
        source_file = source_root / relative
        bundled_file = bundle_shader_root / relative
        if not filecmp.cmp(source_file, bundled_file, shallow=False):
            errors.append(f"{bundled_file}: does not match client-owned source {source_file}")

    return errors


def main():
    parser = argparse.ArgumentParser()
    parser.add_argument("--source-root", required=True)
    parser.add_argument("--bundle-shader-root", required=True)
    args = parser.parse_args()

    errors = validate(
        pathlib.Path(args.source_root).resolve(),
        pathlib.Path(args.bundle_shader_root).resolve())
    if errors:
        for error in errors:
            print(f"client shader bundle policy: {error}", file=sys.stderr)
        return 1
    return 0


if __name__ == "__main__":
    raise SystemExit(main())
