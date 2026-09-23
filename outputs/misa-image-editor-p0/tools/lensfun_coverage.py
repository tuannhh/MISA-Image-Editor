"""Report coverage of the requested lens families in the installed Lensfun DB."""
from __future__ import annotations

import json
from pathlib import Path

import lensfunpy


REQUESTS = [
    ("Canon EF 24-70 f2.8L I", "Canon", "Canon EOS 5D Mark II", "Canon EF 24-70mm f/2.8L USM"),
    ("Canon EF 24-70 f2.8L II", "Canon", "Canon EOS 5D Mark II", "Canon EF 24-70mm f/2.8L II USM"),
    ("Sony 18-135 f4G", "Sony", "ILCE-6000", "E 18-135mm f/4 G OSS"),
    ("Sony 24-70 GM I", "Sony", "ILCE-7M4", "FE 24-70mm f/2.8 GM"),
    ("Sony 24-70 GM II", "Sony", "ILCE-7M4", "FE 24-70mm f/2.8 GM II"),
    ("Sony 70-200 GM I", "Sony", "ILCE-7M4", "FE 70-200mm f/2.8 GM OSS"),
    ("Sony 70-200 GM II", "Sony", "ILCE-7M4", "FE 70-200mm f/2.8 GM II"),
    ("Sony 16-35 GM I", "Sony", "ILCE-7M4", "FE 16-35mm f/2.8 GM"),
    ("Sony 16-35 GM II", "Sony", "ILCE-7M4", "FE 16-35mm f/2.8 GM II"),
    ("Sony 100-400 GM I", "Sony", "ILCE-7M4", "FE 100-400mm f/4.5-5.6 GM OSS"),
    ("Sony 100-400 GM II", "Sony", "ILCE-7M4", "FE 100-400mm f/4.5-5.6 GM II"),
    ("Tamron 17-70 f2.8", "Sony", "ILCE-6000", "E 17-70mm F2.8 B070"),
    ("Sigma 24-70 f2.8 Art", "Sony", "ILCE-7M4", "24-70mm F2.8 DG DN | Art 019"),
]


def main() -> None:
    db = lensfunpy.Database()
    rows = []
    for requested, camera_maker, camera_model, query in REQUESTS:
        cameras = db.find_cameras(camera_maker, camera_model, loose_search=True)
        candidates = db.find_lenses(cameras[0], maker=None, lens=query, loose_search=False) if cameras else []
        matches = [lens for lens in candidates if lens.model.casefold() == query.casefold()]
        nearest = []
        if not matches and "18-135" in query:
            nearest_candidates = db.find_lenses(cameras[0], maker=camera_maker, lens="18-135", loose_search=True)
            nearest = [lens for lens in nearest_candidates if "18-135" in lens.model.casefold()]
        rows.append({
            "requested": requested,
            "camera": camera_model,
            "query": query,
            "status": "available" if matches else "missing",
            "matches": [f"{lens.maker}: {lens.model}" for lens in matches[:8]],
            "nearest_matches": [f"{lens.maker}: {lens.model}" for lens in nearest[:8]],
        })
    output = Path(__file__).resolve().parents[1] / "fixtures" / "lensfun-coverage.json"
    output.write_text(json.dumps({"lensfun_version": lensfunpy.lensfun_version, "rows": rows}, indent=2) + "\n", encoding="utf-8")
    print(output)
    print(json.dumps(rows, indent=2))


if __name__ == "__main__":
    main()
