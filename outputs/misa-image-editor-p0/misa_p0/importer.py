"""Folder import spike. Unsupported codecs remain visible as deferred."""
from __future__ import annotations
from pathlib import Path
from .catalog import Catalog
from .image_io import probe
SUPPORTED_EXTENSIONS={".jpg",".jpeg",".png"}
def import_folder(catalog: Catalog, folder, recursive=True):
    root=Path(folder); session=catalog.begin_import(root); imported=[]; deferred=[]; failed=[]
    paths=root.rglob("*") if recursive else root.glob("*")
    for path in sorted((p for p in paths if p.is_file()), key=lambda p:str(p).lower()):
        if path.suffix.lower() not in SUPPORTED_EXTENSIONS and path.suffix.lower() not in {".arw",".cr2",".cr3",".nef",".nrw",".raf",".orf",".rw2",".dng",".heic",".heif",".avif"}:
            continue
        result=probe(path)
        if result.status=="supported":
            aid=catalog.add_asset(path,result.width,result.height,result.orientation); catalog.record_import(session,aid,"imported"); imported.append(aid)
        elif result.status=="needs_codec_spike":
            catalog.record_import(session,None,"deferred",result.message); deferred.append(str(path))
        else:
            catalog.record_import(session,None,"failed",result.message); failed.append(str(path))
    return {"session_id":session,"imported_asset_ids":imported,"deferred_paths":deferred,"failed_paths":failed}