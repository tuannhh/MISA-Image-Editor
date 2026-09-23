"""Write hashes and runtime metadata for the Windows P0 candidate publish."""
from __future__ import annotations

import hashlib
import json
import platform
import sys
from datetime import datetime, timezone
from pathlib import Path


ROOT = Path(__file__).resolve().parents[1]
CANDIDATE = ROOT / "artifacts" / "p0-candidate"


def digest(path: Path) -> str:
    hasher = hashlib.sha256()
    with path.open("rb") as stream:
        for chunk in iter(lambda: stream.read(1024 * 1024), b""):
            hasher.update(chunk)
    return hasher.hexdigest()


def main() -> int:
    files = []
    for path in sorted(CANDIDATE.rglob("*")):
        if path.is_file() and path.name != "candidate-manifest.json":
            files.append({"path": path.relative_to(CANDIDATE).as_posix(), "bytes": path.stat().st_size, "sha256": digest(path)})
    manifest = {
        "schema": 1,
        "status": "p0-candidate",
        "created_utc": datetime.now(timezone.utc).isoformat(),
        "python_creator": sys.version.split()[0],
        "platform": platform.platform(),
        "target": "net10.0-windows framework-dependent x64 host",
        "files": files,
    }
    output = CANDIDATE / "candidate-manifest.json"
    output.write_text(json.dumps(manifest, indent=2) + "\n", encoding="utf-8")
    print(json.dumps(manifest, indent=2))
    return 0


if __name__ == "__main__":
    raise SystemExit(main())
