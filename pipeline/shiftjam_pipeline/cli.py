"""Command line entry point: `python -m shiftjam_pipeline.cli generate dialogue --brief "..."`."""

from __future__ import annotations

import argparse
import sys

from . import crew, export


def main(argv: list[str] | None = None) -> int:
    p = argparse.ArgumentParser(prog="shiftjam-pipeline")
    sub = p.add_subparsers(dest="cmd", required=True)
    g = sub.add_parser("generate", help="generate one content type")
    g.add_argument("kind", choices=crew.KINDS)
    g.add_argument("--brief", required=True, help="what the content should cover")
    g.add_argument("--count", type=int, default=5)
    args = p.parse_args(argv)

    entries = crew.Crew().run(args.kind, args.brief, args.count)
    path = export.write_content(args.kind, entries)
    export.sync_mgcb()
    print(f"wrote {len(entries)} {args.kind} entries -> {path}")
    return 0


if __name__ == "__main__":
    sys.exit(main())
