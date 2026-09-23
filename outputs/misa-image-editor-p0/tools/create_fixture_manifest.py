"""Create deterministic RGB/HEIF fixtures and a manifest for the P0 harness.

Real camera RAW/HEIC files are deliberately not fabricated; the manifest
records them as required external evidence until the user's camera matrix is
available.
"""
from __future__ import annotations

import hashlib
import json
from pathlib import Path

import numpy as np
from PIL import Image

try:
    from pillow_heif import register_heif_opener

    register_heif_opener()
    HEIF_CODEC_AVAILABLE = True
except ImportError:
    HEIF_CODEC_AVAILABLE = False


ROOT = Path(__file__).resolve().parents[1] / "fixtures"
GENERATED = ROOT / "generated"


def sha256(path: Path) -> str:
    return hashlib.sha256(path.read_bytes()).hexdigest()


def main() -> None:
    GENERATED.mkdir(parents=True, exist_ok=True)
    width, height = 256, 192
    x = np.linspace(0, 255, width, dtype=np.uint8)
    y = np.linspace(0, 255, height, dtype=np.uint8)[:, None]
    pixels = np.stack([np.broadcast_to(x, (height, width)), np.broadcast_to(y, (height, width)), np.full((height, width), 96, dtype=np.uint8)], axis=2)
    png = GENERATED / "p0-rgb-gradient.png"
    jpg = GENERATED / "p0-rgb-gradient.jpg"
    Image.fromarray(pixels, mode="RGB").save(png)
    Image.fromarray(pixels, mode="RGB").save(jpg, quality=95, subsampling=0)
    heic = GENERATED / "p0-heic8.heic"
    heif10 = GENERATED / "p0-heif10.heif"
    if HEIF_CODEC_AVAILABLE:
        Image.fromarray(pixels, mode="RGB").save(heic, format="HEIF", quality=90)
        ten_bit = np.arange(width * height, dtype=np.uint16).reshape(height, width) % 1024
        Image.fromarray(ten_bit).save(heif10, format="HEIF", chroma="444")
    external = []
    external_root = ROOT / "external"
    if external_root.exists():
        for source in sorted(external_root.rglob("*")):
            if not source.is_file() or source.suffix.lower() not in {".arw", ".cr2", ".cr3", ".nef", ".nrw", ".dng", ".heic", ".heif", ".hif"}:
                continue
            relative = source.relative_to(ROOT).as_posix()
            if "raw-pixls-cc0" in source.parts:
                provenance = "raw.pixls.us repository sample; CC0 record; see external/raw-pixls-cc0/README.txt"
            elif "rawdb-cc0" in source.parts:
                provenance = "rawdb.dnglab.org camera sample; CC0 1.0 record; see external/rawdb-cc0/README.txt"
            else:
                provenance = "rawpy upstream test fixture; see raw-fixture-verification.json"
            external.append({
                "path": relative,
                "format": source.suffix[1:].upper(),
                "bytes": source.stat().st_size,
                "sha256": sha256(source),
                "source": provenance,
            })
    manifest = {
        "schema": 1,
        "generated": [
            {"path": str(png.relative_to(ROOT)).replace("\\", "/"), "format": "PNG", "width": width, "height": height, "sha256": sha256(png)},
            {"path": str(jpg.relative_to(ROOT)).replace("\\", "/"), "format": "JPEG", "width": width, "height": height, "sha256": sha256(jpg)},
        ] + ([
            {"path": str(heic.relative_to(ROOT)).replace("\\", "/"), "format": "HEIC 8-bit", "width": width, "height": height, "sha256": sha256(heic)},
            {"path": str(heif10.relative_to(ROOT)).replace("\\", "/"), "format": "HEIF 10-bit synthetic", "width": width, "height": height, "bit_depth": 10, "sha256": sha256(heif10)},
        ] if HEIF_CODEC_AVAILABLE else []),
        "external": external,
        "required_external": [
            {"extension": ".heic", "status": "partial_fixture" if HEIF_CODEC_AVAILABLE else "missing_fixture", "models": ["synthetic p0-heic8"] if HEIF_CODEC_AVAILABLE else []},
            {"extension": ".heif", "status": "partial_fixture", "models": ["Canon PowerShot V1 HIF"]},
            {"extension": ".hif", "status": "partial_fixture", "models": ["Canon PowerShot V1"]},
        ] + [
            {"extension": ".arw", "status": "partial_fixture", "models": ["Sony DSLR-A300"]},
            {"extension": ".cr3", "status": "partial_fixture", "models": ["Canon EOS M50 CRAW"]},
            {"extension": ".nrw", "status": "partial_fixture", "models": ["Nikon Coolpix P330"]},
        ],
        "note": "The external RAW and camera HEIF entries are decoder smoke fixtures with provenance recorded separately. The generated HEIF 10-bit file only validates the codec path; the Canon HIF sample covers one real camera model, while the user's full HEIF/HEIC matrix is still required. The ARW/CR3/NEF/NRW entries are representative public samples, not the user's requested camera matrix; validate camera models, color behavior and license provenance before final P0 RAW/HEIF acceptance.",
    }
    (ROOT / "manifest.json").write_text(json.dumps(manifest, indent=2) + "\n", encoding="utf-8")
    print(json.dumps(manifest, indent=2))


if __name__ == "__main__":
    main()
