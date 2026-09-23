"""P0 offline segmentation provider capability probe.

This module does not ship a subject model. It makes the missing model state
explicit and keeps model loading behind one boundary for later integration.
"""
from __future__ import annotations

from dataclasses import dataclass
from pathlib import Path

import numpy as np
from PIL import Image, ImageOps

try:
    import onnxruntime as ort

    ONNX_RUNTIME_AVAILABLE = True
except ImportError:
    ort = None
    ONNX_RUNTIME_AVAILABLE = False


@dataclass(frozen=True)
class ProviderStatus:
    runtime_available: bool
    execution_providers: tuple[str, ...]
    model_status: str
    message: str


def probe_offline_provider(model_path: str | Path | None = None) -> ProviderStatus:
    if not ONNX_RUNTIME_AVAILABLE:
        return ProviderStatus(False, (), "runtime_missing", "onnxruntime is not installed")
    providers = tuple(ort.get_available_providers())
    if model_path is None:
        return ProviderStatus(True, providers, "model_missing", "runtime is available; no subject/background model is bundled")
    path = Path(model_path)
    if not path.exists():
        return ProviderStatus(True, providers, "model_missing", f"model path does not exist: {path}")
    try:
        ort.InferenceSession(str(path), providers=list(providers) or None)
    except Exception as exc:
        return ProviderStatus(True, providers, "model_invalid", str(exc))
    return ProviderStatus(True, providers, "ready", "offline model loaded")


def _preprocess_rgb(image: Image.Image, input_size: int) -> tuple[np.ndarray, tuple[int, int]]:
    """Prepare an RGB image for MODNet-style dynamic NCHW input."""
    rgb = ImageOps.exif_transpose(image).convert("RGB")
    orig_w, orig_h = rgb.size
    if max(orig_h, orig_w) < input_size or min(orig_h, orig_w) > input_size:
        if orig_w >= orig_h:
            new_h = input_size
            new_w = int(orig_w / orig_h * input_size)
        else:
            new_w = input_size
            new_h = int(orig_h / orig_w * input_size)
    else:
        new_h, new_w = orig_h, orig_w
    new_h = max(32, new_h - (new_h % 32))
    new_w = max(32, new_w - (new_w % 32))
    resized = rgb.resize((new_w, new_h), Image.Resampling.BILINEAR)
    pixels = np.asarray(resized, dtype=np.float32) / np.float32(255.0)
    pixels = (pixels - np.float32(0.5)) / np.float32(0.5)
    return np.transpose(pixels, (2, 0, 1))[None, ...], (orig_w, orig_h)


def infer_subject_mask(
    source: str | Path,
    model_path: str | Path,
    *,
    input_size: int = 512,
    providers: list[str] | None = None,
) -> np.ndarray:
    """Run an offline ONNX subject matting model and return float32 alpha [0, 1]."""
    if not ONNX_RUNTIME_AVAILABLE:
        raise RuntimeError("onnxruntime is not installed")
    model = Path(model_path)
    if not model.exists():
        raise FileNotFoundError(model)
    selected_providers = providers or list(ort.get_available_providers()) or ["CPUExecutionProvider"]
    session = ort.InferenceSession(str(model), providers=selected_providers)
    input_meta = session.get_inputs()[0]
    output_names = [output.name for output in session.get_outputs()]
    with Image.open(source) as opened:
        tensor, (orig_w, orig_h) = _preprocess_rgb(opened, input_size)
    raw = session.run(output_names, {input_meta.name: tensor})[0]
    matte = np.asarray(raw[0, 0], dtype=np.float32)
    resized = Image.fromarray(matte, mode="F").resize((orig_w, orig_h), Image.Resampling.BILINEAR)
    return np.clip(np.asarray(resized, dtype=np.float32), 0.0, 1.0)


def save_subject_mask(source: str | Path, destination: str | Path, model_path: str | Path) -> dict[str, object]:
    """Infer and save an 8-bit grayscale subject mask with verification metadata."""
    matte = infer_subject_mask(source, model_path)
    output = Path(destination)
    output.parent.mkdir(parents=True, exist_ok=True)
    Image.fromarray(np.round(matte * 255.0).astype(np.uint8), mode="L").save(output)
    return {
        "source": str(source),
        "model": str(model_path),
        "width": int(matte.shape[1]),
        "height": int(matte.shape[0]),
        "dtype": str(matte.dtype),
        "min": float(matte.min()),
        "max": float(matte.max()),
        "mean": float(matte.mean()),
        "mask_path": str(output),
    }
