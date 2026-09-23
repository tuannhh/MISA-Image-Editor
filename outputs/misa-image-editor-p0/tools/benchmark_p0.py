"""Repeatable local latency/RSS probe for the P0 float preview path."""
from __future__ import annotations

import ctypes
import argparse
import json
import sys
import tempfile
import time
from ctypes import wintypes
from pathlib import Path

import numpy as np
from PIL import Image

sys.path.insert(0, str(Path(__file__).resolve().parents[1]))
from misa_p0.image_io import render_float


class _MemoryCounters(ctypes.Structure):
    _fields_ = [
        ("cb", ctypes.c_ulong),
        ("PageFaultCount", ctypes.c_ulong),
        ("PeakWorkingSetSize", ctypes.c_size_t),
        ("WorkingSetSize", ctypes.c_size_t),
        ("QuotaPeakPagedPoolUsage", ctypes.c_size_t),
        ("QuotaPagedPoolUsage", ctypes.c_size_t),
        ("QuotaPeakNonPagedPoolUsage", ctypes.c_size_t),
        ("QuotaNonPagedPoolUsage", ctypes.c_size_t),
        ("PagefileUsage", ctypes.c_size_t),
        ("PeakPagefileUsage", ctypes.c_size_t),
    ]


def peak_working_set_mb() -> float | None:
    counters = _MemoryCounters()
    counters.cb = ctypes.sizeof(counters)
    process = ctypes.windll.kernel32.GetCurrentProcess()
    get_info = ctypes.WinDLL("psapi").GetProcessMemoryInfo
    get_info.argtypes = [wintypes.HANDLE, ctypes.POINTER(_MemoryCounters), wintypes.DWORD]
    get_info.restype = wintypes.BOOL
    ok = get_info(process, ctypes.byref(counters), counters.cb)
    return counters.PeakWorkingSetSize / (1024 * 1024) if ok else None


def benchmark(source: Path, label: str, recipe: dict[str, object]) -> dict[str, object]:
    started = time.perf_counter()
    rendered = render_float(source, recipe)
    elapsed = time.perf_counter() - started
    height, width = rendered.shape[:2]
    peak = peak_working_set_mb()
    return {
        "source": label,
        "width": width,
        "height": height,
        "megapixels": round(width * height / 1_000_000, 3),
        "seconds": round(elapsed, 4),
        "megapixels_per_second": round((width * height / 1_000_000) / elapsed, 2),
        "peak_working_set_mb": None if peak is None else round(peak, 2),
        "dtype": str(rendered.dtype),
    }


def main() -> None:
    parser = argparse.ArgumentParser(description="Benchmark the P0 float preview path.")
    parser.add_argument("--source", type=Path, help="Optional real HEIF/RAW/RGB fixture to decode and render")
    parser.add_argument("--output", type=Path, help="Optional JSON output path")
    args = parser.parse_args()
    width, height = 4000, 3000
    x = np.linspace(0, 255, width, dtype=np.uint8)
    row = np.stack([x, np.flip(x), np.full(width, 96, dtype=np.uint8)], axis=1)
    pixels = np.repeat(row[None, :, :], height, axis=0)
    recipe = {"basic": {"exposure": 0.35, "contrast": 12, "saturation": 8}}
    if args.source:
        source = args.source.resolve()
        result = benchmark(source, str(args.source), recipe)
    else:
        with tempfile.TemporaryDirectory(prefix="misa-p0-benchmark-") as tmp:
            source = Path(tmp) / "12mp.png"
            Image.fromarray(pixels, mode="RGB").save(source)
            result = benchmark(source, "synthetic-12mp", recipe)
    serialized = json.dumps(result, indent=2)
    if args.output:
        args.output.parent.mkdir(parents=True, exist_ok=True)
        args.output.write_text(serialized + "\n", encoding="utf-8")
    print(serialized)


if __name__ == "__main__":
    main()
