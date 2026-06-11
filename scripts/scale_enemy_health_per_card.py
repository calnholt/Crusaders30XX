#!/usr/bin/env python3
"""Multiply every enemy HealthPerCard value by a given multiplier."""

from __future__ import annotations

import argparse
import re
import sys
from pathlib import Path

HEALTH_PER_CARD_RE = re.compile(
    r"^(\s*HealthPerCard\s*=\s*)([\d.]+)f(\s*;.*)$",
    re.MULTILINE,
)

REPO_ROOT = Path(__file__).resolve().parents[1]
ENEMIES_DIR = REPO_ROOT / "ECS" / "Objects" / "Enemies"


def format_health_per_card(value: float) -> str:
    rounded = round(value, 4)
    text = f"{rounded:.4f}".rstrip("0").rstrip(".")
    return f"{text}f"


def scale_file(path: Path, multiplier: float) -> list[tuple[str, str]]:
    content = path.read_text(encoding="utf-8")
    changes: list[tuple[str, str]] = []

    def replace(match: re.Match[str]) -> str:
        prefix, old_text, suffix = match.groups()
        old_value = float(old_text)
        new_value = round(old_value * multiplier, 4)
        new_text = format_health_per_card(new_value)
        changes.append((f"{old_text}f", new_text))
        return f"{prefix}{new_text}{suffix}"

    updated = HEALTH_PER_CARD_RE.sub(replace, content)
    if updated != content:
        path.write_text(updated, encoding="utf-8")

    return changes


def main() -> int:
    parser = argparse.ArgumentParser(
        description="Multiply all enemy HealthPerCard values by a multiplier.",
    )
    parser.add_argument(
        "multiplier",
        type=float,
        help="Factor to multiply each HealthPerCard value by (e.g. 1.1)",
    )
    args = parser.parse_args()

    if args.multiplier <= 0:
        print("Error: multiplier must be greater than 0.", file=sys.stderr)
        return 1

    if not ENEMIES_DIR.is_dir():
        print(f"Error: enemies directory not found: {ENEMIES_DIR}", file=sys.stderr)
        return 1

    files_updated = 0
    assignments_updated = 0

    for path in sorted(ENEMIES_DIR.glob("*.cs")):
        changes = scale_file(path, args.multiplier)
        if not changes:
            continue

        files_updated += 1
        for old_value, new_value in changes:
            assignments_updated += 1
            print(f"{path.name}: {old_value} -> {new_value}")

    if assignments_updated == 0:
        print("Error: no HealthPerCard assignments found.", file=sys.stderr)
        return 1

    print(f"\nUpdated {assignments_updated} assignment(s) in {files_updated} file(s).")
    return 0


if __name__ == "__main__":
  raise SystemExit(main())
