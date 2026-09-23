"""Measure P0 RGB decoder/export drift against the deterministic gradient fixture.

This is a repeatable display-space baseline for the current P0 pipeline.  It
does not certify camera RAW colour or claim Adobe compatibility.
"""
from __future__ import annotations

import json
import sys
import tempfile
from pathlib import Path

import numpy as np

ROOT = Path(__file__).resolve().parents[1]
if str(ROOT) not in sys.path:
    sys.path.insert(0, str(ROOT))

from misa_p0.image_io import HEIF_CODEC_AVAILABLE, export_rgb, render_float


FIXTURES = ROOT / "fixtures" / "generated"


def expected_gradient(width: int = 256, height: int = 192) -> np.ndarray:
    x = np.linspace(0, 255, width, dtype=np.uint8)
    y = np.linspace(0, 255, height, dtype=np.uint8)[:, None]
    return np.stack(
        [np.broadcast_to(x, (height, width)), np.broadcast_to(y, (height, width)), np.full((height, width), 96, dtype=np.uint8)],
        axis=2,
    ).astype(np.float32) / np.float32(255)


def rgb_to_lab(rgb: np.ndarray) -> np.ndarray:
    rgb = np.asarray(rgb, dtype=np.float32)
    linear = np.where(rgb <= 0.04045, rgb / 12.92, ((rgb + 0.055) / 1.055) ** 2.4)
    xyz = np.empty_like(linear)
    xyz[..., 0] = linear[..., 0] * 0.4124564 + linear[..., 1] * 0.3575761 + linear[..., 2] * 0.1804375
    xyz[..., 1] = linear[..., 0] * 0.2126729 + linear[..., 1] * 0.7151522 + linear[..., 2] * 0.0721750
    xyz[..., 2] = linear[..., 0] * 0.0193339 + linear[..., 1] * 0.1191920 + linear[..., 2] * 0.9503041
    ratio = xyz / np.array([0.95047, 1.0, 1.08883], dtype=np.float32)
    epsilon = 216 / 24389
    kappa = 24389 / 27
    f = np.where(ratio > epsilon, np.cbrt(ratio), (kappa * ratio + 16) / 116)
    return np.stack([116 * f[..., 1] - 16, 500 * (f[..., 0] - f[..., 1]), 200 * (f[..., 1] - f[..., 2])], axis=2)


def delta_e76(actual: np.ndarray, expected: np.ndarray) -> np.ndarray:
    return np.linalg.norm(rgb_to_lab(actual) - rgb_to_lab(expected), axis=2)


def measure(path: Path, expected: np.ndarray) -> dict[str, object]:
    actual = render_float(path, {})
    if actual.shape != expected.shape:
        raise AssertionError(f"unexpected shape for {path.name}: {actual.shape}, expected {expected.shape}")
    delta = delta_e76(actual, expected)
    try:
        displayed_path = str(path.relative_to(ROOT)).replace("\\", "/")
    except ValueError:
        displayed_path = str(path)
    result = {
        "path": displayed_path,
        "shape": list(actual.shape),
        "mean_delta_e76": round(float(delta.mean()), 4),
        "p95_delta_e76": round(float(np.percentile(delta, 95)), 4),
        "max_delta_e76": round(float(delta.max()), 4),
    }
    # These limits are an explicit P0 display RGB regression threshold, not a camera-colour acceptance criterion.
    result["passed"] = result["mean_delta_e76"] <= 3.0 and result["p95_delta_e76"] <= 8.0 and result["max_delta_e76"] <= 35.0
    return result


def build_report() -> dict[str, object]:
    reference = expected_gradient()
    samples = [measure(FIXTURES / "p0-rgb-gradient.png", reference), measure(FIXTURES / "p0-rgb-gradient.jpg", reference)]
    if HEIF_CODEC_AVAILABLE:
        samples.append(measure(FIXTURES / "p0-heic8.heic", reference))
    with tempfile.TemporaryDirectory(prefix="misa-p0-colour-") as temporary:
        exported = Path(temporary) / "p0-export.jpg"
        export_rgb(FIXTURES / "p0-rgb-gradient.png", exported, {})
        export_measurement = measure(exported, reference)
        export_measurement["path"] = "temporary/p0-export.jpg"
        samples.append(export_measurement)
    return {
        "schema": 1,
        "space": "sRGB display RGB decoded to CIE Lab D65 for DeltaE76 measurement",
        "scope": "P0 deterministic RGB decoder/export baseline; not RAW camera colour validation",
        "threshold": {"mean_delta_e76_max": 3.0, "p95_delta_e76_max": 8.0, "max_delta_e76_max": 35.0},
        "samples": samples,
        "passed": all(bool(sample["passed"]) for sample in samples),
    }


def main() -> None:
    report = build_report()
    output = ROOT / "fixtures" / "color-baseline-verification.json"
    output.write_text(json.dumps(report, indent=2) + "\n", encoding="utf-8")
    print(output)
    print(json.dumps(report, indent=2))
    if not report["passed"]:
        raise SystemExit(1)


if __name__ == "__main__":
    main()
