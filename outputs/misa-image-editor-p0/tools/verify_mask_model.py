"""Run the checked-in offline subject-mask model and record a P0 smoke result."""
from __future__ import annotations

import hashlib
import json
import sys
import time
from pathlib import Path

import numpy as np
from PIL import Image

sys.path.insert(0, str(Path(__file__).resolve().parents[1]))
from misa_p0.mask import infer_subject_mask, probe_offline_provider


ROOT = Path(__file__).resolve().parents[1]
MODEL = ROOT / "models" / "modnet_photographic.onnx"
SOURCE = ROOT / "fixtures" / "generated" / "rawdb-canon-v1-heif-preview.jpg"
MASK = ROOT / "fixtures" / "generated" / "rawdb-canon-v1-subject-mask.png"


def sha256(path: Path) -> str:
    return hashlib.sha256(path.read_bytes()).hexdigest()


def main() -> None:
    status = probe_offline_provider(MODEL)
    if status.model_status != "ready":
        raise SystemExit(status.message)
    started = time.perf_counter()
    matte = infer_subject_mask(SOURCE, MODEL)
    elapsed = time.perf_counter() - started
    MASK.parent.mkdir(parents=True, exist_ok=True)
    Image.fromarray(np.round(matte * 255.0).astype(np.uint8), mode="L").save(MASK)
    record = {
        "source": str(SOURCE.relative_to(ROOT)).replace("\\", "/"),
        "model": str(MODEL.relative_to(ROOT)).replace("\\", "/"),
        "model_sha256": sha256(MODEL),
        "model_license": "Apache-2.0 (as documented by yakhyo/modnet)",
        "providers": list(status.execution_providers),
        "mask": str(MASK.relative_to(ROOT)).replace("\\", "/"),
        "mask_sha256": sha256(MASK),
        "shape": list(matte.shape),
        "dtype": str(matte.dtype),
        "min": float(matte.min()),
        "max": float(matte.max()),
        "mean": float(matte.mean()),
        "nonzero_fraction": float(np.mean(matte > 0.05)),
        "seconds": round(elapsed, 3),
    }
    output = ROOT / "fixtures" / "mask-model-verification.json"
    output.write_text(json.dumps(record, indent=2) + "\n", encoding="utf-8")
    print(json.dumps(record, indent=2))


if __name__ == "__main__":
    main()
