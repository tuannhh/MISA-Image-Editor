"""Decode checked-in real camera HEIF/HIF fixtures and record provenance."""
from __future__ import annotations

import hashlib
import json
import sys
import time
from pathlib import Path

from PIL import Image

try:
    import pillow_heif
except ImportError as exc:  # pragma: no cover - environment gate
    raise SystemExit(f"pillow-heif is required: {exc}")

pillow_heif.register_heif_opener()
sys.path.insert(0, str(Path(__file__).resolve().parents[1]))
from misa_p0.image_io import probe, render_float


ROOT = Path(__file__).resolve().parents[1]
FIXTURE = ROOT / "fixtures" / "external" / "rawdb-cc0" / "Canon-PowerShot-V1-ISO-200-HDR.HIF"
PREVIEW = ROOT / "fixtures" / "generated" / "rawdb-canon-v1-heif-preview.jpg"


def sha256(path: Path) -> str:
    return hashlib.sha256(path.read_bytes()).hexdigest()


def main() -> None:
    started = time.perf_counter()
    result = probe(FIXTURE)
    if result.status != "supported":
        raise SystemExit(f"camera HEIF probe failed: {result}")
    with Image.open(FIXTURE) as image:
        format_name = image.format
        size = {"width": image.width, "height": image.height}
        orientation = int(image.getexif().get(274, 1))
        preview = image.convert("RGB")
        preview.thumbnail((1600, 1600), Image.Resampling.LANCZOS)
        PREVIEW.parent.mkdir(parents=True, exist_ok=True)
        preview.save(PREVIEW, quality=92, subsampling=0)
    heif = pillow_heif.open_heif(FIXTURE)
    record = {
        "source": str(FIXTURE.relative_to(ROOT / "fixtures")).replace("\\", "/"),
        "source_group": "rawdb.dnglab.org Canon PowerShot V1",
        "source_url": "https://rawdb.dnglab.org/api/sets/Canon/PowerShot%20V1",
        "download_url": "https://rawdb.dnglab.org/api/download/Canon/PowerShot%20V1/heif/CANON_V1_ISO_200_HDR.HIF",
        "license": "CC0 1.0 (as recorded by RawDB)",
        "sha256": sha256(FIXTURE),
        "bytes": FIXTURE.stat().st_size,
        "probe_status": result.status,
        "format": format_name,
        "dimensions": size,
        "orientation": orientation,
        "bit_depth": heif.info.get("bit_depth"),
        "preview": str(PREVIEW.relative_to(ROOT)).replace("\\", "/"),
        "preview_sha256": sha256(PREVIEW),
        "seconds": round(time.perf_counter() - started, 3),
    }
    pixels = render_float(FIXTURE, {})
    record["render_shape"] = list(pixels.shape)
    output = ROOT / "fixtures" / "camera-heif-verification.json"
    output.write_text(json.dumps(record, indent=2) + "\n", encoding="utf-8")
    print(json.dumps(record, indent=2))


if __name__ == "__main__":
    main()
