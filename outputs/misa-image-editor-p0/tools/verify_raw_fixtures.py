"""Decode the checked-in rawpy/LibRaw fixtures and record provenance results."""
from __future__ import annotations

import hashlib
import json
import sys
import time
from pathlib import Path

import rawpy
from PIL import Image

sys.path.insert(0, str(Path(__file__).resolve().parents[1]))
from misa_p0.image_io import probe, render_float


ROOT = Path(__file__).resolve().parents[1]
SOURCE = ROOT / "fixtures" / "external"
PREVIEWS = ROOT / "fixtures" / "generated" / "raw-previews"


def sha256(path: Path) -> str:
    return hashlib.sha256(path.read_bytes()).hexdigest()


def main() -> None:
    PREVIEWS.mkdir(parents=True, exist_ok=True)
    results = []
    for source in sorted(SOURCE.rglob("*")):
        if not source.is_file() or source.suffix.lower() not in {".nef", ".cr2", ".cr3", ".arw", ".nrw", ".dng"}:
            continue
        started = time.perf_counter()
        with rawpy.imread(str(source)) as raw:
            size = {"width": raw.sizes.width, "height": raw.sizes.height}
            lens = raw.lens
            other = raw.other
            preview = raw.postprocess(output_bps=8, use_camera_wb=True, no_auto_bright=True, half_size=True)
            image = Image.fromarray(preview, mode="RGB")
            image.thumbnail((1600, 1600), Image.Resampling.LANCZOS)
            preview_path = PREVIEWS / (source.stem + ".jpg")
            image.save(preview_path, quality=92, subsampling=0)
        results.append({
            "source": str(source.relative_to(ROOT / "fixtures")).replace("\\", "/"),
            "source_group": "raw.pixls.us CC0" if "raw-pixls-cc0" in source.parts else "rawpy upstream",
            "sha256": sha256(source),
            "bytes": source.stat().st_size,
            "probe_status": probe(source).status,
            "dimensions": size,
            "lens_make": lens.make,
            "lens_model": lens.model,
            "focal_length": other.focal_length,
            "iso": other.iso_speed,
            "preview": str(preview_path.relative_to(ROOT)).replace("\\", "/"),
            "preview_sha256": sha256(preview_path),
            "seconds": round(time.perf_counter() - started, 3),
        })
    output = ROOT / "fixtures" / "raw-fixture-verification.json"
    output.write_text(json.dumps({
        "source": "rawpy upstream and raw.pixls.us smoke fixtures",
        "source_commit": "a39c2e7a44911889c3360891012f862f904ba551",
        "license_note": "The rawpy upstream README attributes some downloads to rawsamples.ch under CC BY-NC-SA 4.0; raw.pixls.us selected records are CC0 in its repository JSON. Preserve source URLs and perform release review before redistribution.",
        "results": results,
    }, indent=2) + "\n", encoding="utf-8")
    print(output)
    print(json.dumps(results, indent=2))


if __name__ == "__main__":
    main()
