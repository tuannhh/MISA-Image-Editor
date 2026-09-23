"""Generate a WPF-friendly JPEG preview for a RAW or camera HEIF source.

This is a replaceable process boundary for the P0 desktop shell. It keeps LibRaw
and libheif bindings out of the WPF process while using the same checked Python
environment as the fixture pipeline. The output is a display/export preview,
not a claim of production RAW color rendering.
"""
from __future__ import annotations

import json
import sys
from pathlib import Path

from PIL import Image, ImageOps

try:
    from pillow_heif import register_heif_opener

    register_heif_opener()
except ImportError:
    pass

try:
    import rawpy
except ImportError:
    rawpy = None


RAW_EXTENSIONS = {".arw", ".cr2", ".cr3", ".nef", ".nrw", ".raf", ".orf", ".rw2", ".dng"}
HEIF_EXTENSIONS = {".heic", ".heif", ".hif", ".avif"}


def generate(source: Path, destination: Path, max_side: int = 2400) -> dict[str, object]:
    suffix = source.suffix.lower()
    if suffix in RAW_EXTENSIONS:
        if rawpy is None:
            raise RuntimeError("rawpy is not installed")
        # Python opens Unicode Windows paths; LibRaw receives a file stream.
        with source.open("rb") as stream, rawpy.imread(stream) as raw:
            pixels = raw.postprocess(output_bps=8, use_camera_wb=True, no_auto_bright=True, half_size=True)
        image = Image.fromarray(pixels, mode="RGB")
        decoder = "rawpy/LibRaw"
    elif suffix in HEIF_EXTENSIONS:
        with Image.open(source) as opened:
            image = ImageOps.exif_transpose(opened).convert("RGB")
            decoder = "pillow-heif"
    else:
        with Image.open(source) as opened:
            image = ImageOps.exif_transpose(opened).convert("RGB")
            decoder = opened.format or "Pillow"
    if max(image.size) > max_side:
        image.thumbnail((max_side, max_side), Image.Resampling.LANCZOS)
    destination.parent.mkdir(parents=True, exist_ok=True)
    image.save(destination, format="JPEG", quality=92, subsampling=0)
    return {
        "source": str(source),
        "destination": str(destination),
        "decoder": decoder,
        "width": image.width,
        "height": image.height,
    }


def main() -> int:
    if len(sys.argv) not in {3, 4}:
        print("usage: generate_preview.py SOURCE DESTINATION [MAX_SIDE]", file=sys.stderr)
        return 2
    source = Path(sys.argv[1])
    destination = Path(sys.argv[2])
    max_side = int(sys.argv[3]) if len(sys.argv) == 4 else 2400
    if not source.exists():
        print(f"source does not exist: {source}", file=sys.stderr)
        return 3
    try:
        # Redirected Windows stdout may be cp1252. Keep the JSON protocol ASCII-safe.
        print(json.dumps(generate(source, destination, max_side), ensure_ascii=True))
    except Exception as exc:
        print(f"preview generation failed: {exc}", file=sys.stderr)
        return 5
    return 0


if __name__ == "__main__":
    raise SystemExit(main())
