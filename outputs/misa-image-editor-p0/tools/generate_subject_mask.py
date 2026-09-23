"""Generate a grayscale subject mask with the checked-in offline MODNet model.

This is intentionally a small process boundary for the WPF P0 shell. The UI
does not import Python or ONNX Runtime into its process; the provider can be
replaced by a native/WinML worker later without changing the recipe contract.
"""
from __future__ import annotations

import json
import sys
from pathlib import Path

import numpy as np
import onnxruntime as ort
from PIL import Image, ImageOps


def infer(source: Path, model: Path, destination: Path) -> dict[str, object]:
    with Image.open(source) as opened:
        image = ImageOps.exif_transpose(opened).convert("RGB")
        original_size = image.size
        width, height = image.size
        input_size = 512
        if max(height, width) < input_size or min(height, width) > input_size:
            if width >= height:
                height = input_size
                width = max(32, int(original_size[0] / original_size[1] * input_size))
            else:
                width = input_size
                height = max(32, int(original_size[1] / original_size[0] * input_size))
        width -= width % 32
        height -= height % 32
        resized = image.resize((max(32, width), max(32, height)), Image.Resampling.BILINEAR)
    pixels = np.asarray(resized, dtype=np.float32) / np.float32(255.0)
    pixels = (pixels - np.float32(0.5)) / np.float32(0.5)
    tensor = np.transpose(pixels, (2, 0, 1))[None, ...]
    providers = list(ort.get_available_providers()) or ["CPUExecutionProvider"]
    session = ort.InferenceSession(str(model), providers=providers)
    input_meta = session.get_inputs()[0]
    output = session.run([session.get_outputs()[0].name], {input_meta.name: tensor})[0]
    matte = np.asarray(output[0, 0], dtype=np.float32)
    matte = np.asarray(Image.fromarray(matte, mode="F").resize(original_size, Image.Resampling.BILINEAR), dtype=np.float32)
    matte = np.clip(matte, 0.0, 1.0)
    destination.parent.mkdir(parents=True, exist_ok=True)
    Image.fromarray(np.round(matte * 255.0).astype(np.uint8), mode="L").save(destination)
    return {
        "source": str(source),
        "model": str(model),
        "mask": str(destination),
        "width": int(matte.shape[1]),
        "height": int(matte.shape[0]),
        "providers": providers,
        "mean": float(matte.mean()),
    }


def main() -> int:
    if len(sys.argv) != 4:
        print("usage: generate_subject_mask.py SOURCE MODEL DESTINATION", file=sys.stderr)
        return 2
    source, model, destination = map(Path, sys.argv[1:])
    if not source.exists():
        print(f"source does not exist: {source}", file=sys.stderr)
        return 3
    if not model.exists():
        print(f"model does not exist: {model}", file=sys.stderr)
        return 4
    try:
        print(json.dumps(infer(source, model, destination), ensure_ascii=True))
    except Exception as exc:  # process boundary reports the provider failure to the UI
        print(f"subject mask failed: {exc}", file=sys.stderr)
        return 5
    return 0


if __name__ == "__main__":
    raise SystemExit(main())
