"""Verify the optional desktop RAW/HEIF preview process boundary."""
from __future__ import annotations

import json
import sys
import time
from pathlib import Path

from PIL import Image, ImageOps

ROOT = Path(__file__).resolve().parents[1]
sys.path.insert(0, str(ROOT / "tools"))
from generate_preview import generate  # noqa: E402


def main() -> int:
    source_root = ROOT / "fixtures" / "external"
    output_root = ROOT / "fixtures" / "generated" / "preview-bridge"
    targets = []
    for extension in (".arw", ".cr3", ".nef", ".hif"):
        target = next(source_root.rglob(f"*{extension}"), None)
        if target is not None:
            targets.append(target)
    results = []
    for source in targets:
        output = output_root / f"{source.stem}.jpg"
        started = time.perf_counter()
        info = generate(source, output)
        with Image.open(output) as rendered:
            if ImageOps.exif_transpose(rendered).size != rendered.size:
                raise RuntimeError(f"preview orientation is not normalized: {source}")
            output_size = list(rendered.size)
        results.append({
            "source": str(source.relative_to(ROOT)).replace("\\", "/"),
            "output": str(output.relative_to(ROOT)).replace("\\", "/"),
            "decoder": info["decoder"],
            "width": info["width"],
            "height": info["height"],
            "output_size": output_size,
            "output_orientation": 1,
            "seconds": round(time.perf_counter() - started, 3),
            "bytes": output.stat().st_size,
        })
    if len(results) < 4:
        raise RuntimeError(f"preview bridge matrix found only {len(results)} representative files")
    report = ROOT / "fixtures" / "preview-bridge-verification.json"
    report.write_text(json.dumps({"source": "checked-in representative RAW/HEIF fixtures", "results": results}, indent=2) + "\n", encoding="utf-8")
    print(report)
    print(json.dumps(results, indent=2))
    return 0


if __name__ == "__main__":
    raise SystemExit(main())
