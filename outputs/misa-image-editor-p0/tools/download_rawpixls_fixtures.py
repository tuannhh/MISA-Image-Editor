"""Download a small CC0 camera matrix from raw.pixls.us with hash checks."""
from __future__ import annotations

import hashlib
import json
import urllib.request
from pathlib import Path


ROOT = Path(__file__).resolve().parents[1]
DEST = ROOT / "fixtures" / "external" / "raw-pixls-cc0"
FILES = (
    (
        "Sony-SLT-A77.ARW",
        "https://raw.pixls.us/getfile.php/3963/nice/Sony%20-%20SLT-A77%20-%2012bit%2012bit%20compressed%20%283:2%29.ARW",
        "161f574b92b7f06634af45950c8d2c885b2b35ee7ed53ee505ee027485af127c",
    ),
    (
        "Nikon-Z6-lossless.NEF",
        "https://raw.pixls.us/getfile.php/3582/nice/Nikon%20-%20Z%206%20-%2014bit%2014bit%20lossless%20compressed%20%283:2%29.NEF",
        "c079345fc93f53a4f0d322f8ddaae505f920c36015f25b58befccaa61db1af31",
    ),
    (
        "Canon-EOS-R.CR3",
        "https://raw.pixls.us/getfile.php/4515/nice/Canon%20-%20EOS%20R%20-%203:2.CR3",
        "89bd55532b0cceb5efa360e97827ba35c2076bce0de4675f3a4413a883dac880",
    ),
)


def sha256(path: Path) -> str:
    digest = hashlib.sha256()
    with path.open("rb") as stream:
        for chunk in iter(lambda: stream.read(1024 * 1024), b""):
            digest.update(chunk)
    return digest.hexdigest()


def main() -> int:
    DEST.mkdir(parents=True, exist_ok=True)
    result = []
    for filename, url, expected in FILES:
        destination = DEST / filename
        if not destination.exists():
            print(f"downloading {filename}", flush=True)
            with urllib.request.urlopen(url, timeout=180) as source, destination.open("wb") as output:
                while True:
                    chunk = source.read(1024 * 1024)
                    if not chunk:
                        break
                    output.write(chunk)
        actual = sha256(destination)
        if actual != expected:
            raise SystemExit(f"hash mismatch for {filename}: {actual} != {expected}")
        result.append({"path": str(destination.relative_to(ROOT / "fixtures")).replace("\\", "/"), "bytes": destination.stat().st_size, "sha256": actual, "source": url, "license": "CC0 1.0 as recorded by raw.pixls.us"})
    print(json.dumps(result, indent=2))
    return 0


if __name__ == "__main__":
    raise SystemExit(main())
