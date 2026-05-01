#!/usr/bin/env python3
import argparse
import pathlib
import subprocess
import sys


EXPECTED_FORMATS = {
    "pe-x86-64": "pe-x86-64",
}


def validate(archive, expected_format, objdump):
    if expected_format not in EXPECTED_FORMATS:
        return [f"unsupported expected archive format {expected_format}"]

    if not archive.exists():
        return [f"{archive}: native archive does not exist"]

    result = subprocess.run(
        [objdump, "-f", str(archive)],
        check=False,
        text=True,
        stdout=subprocess.PIPE,
        stderr=subprocess.PIPE)
    if result.returncode != 0:
        return [f"{archive}: objdump failed: {result.stderr.strip()}"]

    expected = EXPECTED_FORMATS[expected_format]
    if expected not in result.stdout:
        return [f"{archive}: expected object format {expected}, objdump output was {result.stdout.strip()}"]

    return []


def main():
    parser = argparse.ArgumentParser()
    parser.add_argument("--archive", required=True)
    parser.add_argument("--expected-format", required=True)
    parser.add_argument("--objdump", default="objdump")
    args = parser.parse_args()

    errors = validate(pathlib.Path(args.archive), args.expected_format, args.objdump)
    if errors:
        for error in errors:
            print(f"native archive format policy: {error}", file=sys.stderr)
        return 1
    return 0


if __name__ == "__main__":
    raise SystemExit(main())
