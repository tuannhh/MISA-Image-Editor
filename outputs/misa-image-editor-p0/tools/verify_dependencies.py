from __future__ import annotations

import importlib.metadata
import json
import shutil
import subprocess
import sys
from pathlib import Path


PACKAGES = (
    "numpy",
    "Pillow",
    "pillow-heif",
    "rawpy",
    "onnxruntime",
    "lensfunpy",
    "flatbuffers",
    "protobuf",
    "ExifRead",
)


def command_info(path: str | None, args: tuple[str, ...] = ("--version",)) -> dict[str, object]:
    if not path:
        return {"path": None, "version": None, "status": "missing"}
    try:
        result = subprocess.run([path, *args], capture_output=True, text=True, timeout=20, check=False)
        text = (result.stdout or result.stderr).strip().splitlines()
        status = "supported" if result.returncode == 0 else ("present" if text else "error")
        return {"path": path, "version": text[0] if text else None, "status": status}
    except Exception as exc:  # pragma: no cover - diagnostics must survive tool failures
        return {"path": path, "version": None, "status": "error", "message": str(exc)}


def main() -> int:
    root = Path(__file__).resolve().parents[1]
    packages: dict[str, object] = {}
    for name in PACKAGES:
        try:
            packages[name] = importlib.metadata.version(name)
        except importlib.metadata.PackageNotFoundError:
            packages[name] = None

    onnx_providers: list[str] = []
    try:
        import onnxruntime

        onnx_providers = list(onnxruntime.get_available_providers())
    except ImportError:
        pass

    candidates = {
        "dotnet": shutil.which("dotnet") or r"C:\Program Files\dotnet\dotnet.exe",
        "cmake": shutil.which("cmake") or r"C:\Program Files\CMake\bin\cmake.exe",
        "git": shutil.which("git") or r"C:\Program Files\Git\cmd\git.exe",
        "msvc": r"C:\Program Files (x86)\Microsoft Visual Studio\2022\BuildTools\VC\Tools\MSVC\14.44.35207\bin\Hostx64\x64\cl.exe",
    }
    tools = {name: command_info(path) if Path(path).exists() else {"path": None, "version": None, "status": "missing"} for name, path in candidates.items()}
    result = {
        "python": {"executable": sys.executable, "version": sys.version.split()[0]},
        "packages": packages,
        "onnx_providers": onnx_providers,
        "tools": tools,
    }
    output = root / "fixtures" / "dependency-verification.json"
    output.write_text(json.dumps(result, indent=2) + "\n", encoding="utf-8")
    print(json.dumps(result, indent=2))
    return 0 if all(value is not None for value in packages.values()) else 1


if __name__ == "__main__":
    raise SystemExit(main())
