"""SQLite catalog used by P0."""
from __future__ import annotations
from contextlib import contextmanager
import hashlib, json, sqlite3
from pathlib import Path
from typing import Iterator, Sequence
from .recipe import Recipe

SCHEMA = """
CREATE TABLE IF NOT EXISTS assets(id INTEGER PRIMARY KEY,path TEXT NOT NULL UNIQUE,fingerprint TEXT NOT NULL,width INTEGER,height INTEGER,orientation INTEGER NOT NULL DEFAULT 1,status TEXT NOT NULL DEFAULT 'available',recipe_json TEXT NOT NULL);
CREATE TABLE IF NOT EXISTS import_sessions(id INTEGER PRIMARY KEY,source_folder TEXT NOT NULL,created_at TEXT NOT NULL DEFAULT CURRENT_TIMESTAMP,imported_count INTEGER NOT NULL DEFAULT 0,deferred_count INTEGER NOT NULL DEFAULT 0,failed_count INTEGER NOT NULL DEFAULT 0);
CREATE TABLE IF NOT EXISTS import_items(session_id INTEGER NOT NULL REFERENCES import_sessions(id) ON DELETE CASCADE,asset_id INTEGER REFERENCES assets(id),status TEXT NOT NULL,message TEXT NOT NULL DEFAULT '',PRIMARY KEY(session_id,asset_id,status));
CREATE TABLE IF NOT EXISTS collections(id INTEGER PRIMARY KEY,name TEXT NOT NULL UNIQUE);
CREATE TABLE IF NOT EXISTS collection_items(collection_id INTEGER NOT NULL REFERENCES collections(id) ON DELETE CASCADE,asset_id INTEGER NOT NULL REFERENCES assets(id) ON DELETE CASCADE,PRIMARY KEY(collection_id,asset_id));
CREATE TABLE IF NOT EXISTS settings(key TEXT PRIMARY KEY,value TEXT);
CREATE TABLE IF NOT EXISTS revisions(id INTEGER PRIMARY KEY,asset_id INTEGER NOT NULL REFERENCES assets(id),recipe_json TEXT NOT NULL,created_at TEXT NOT NULL DEFAULT CURRENT_TIMESTAMP);
"""

class Catalog:
    def __init__(self,path):
        self.path=Path(path); self.path.parent.mkdir(parents=True,exist_ok=True)
        self.db=sqlite3.connect(self.path); self.db.execute("PRAGMA foreign_keys=ON"); self.db.executescript(SCHEMA); self.db.commit()
    def close(self): self.db.close()
    @contextmanager
    def transaction(self)->Iterator[sqlite3.Connection]:
        try: yield self.db; self.db.commit()
        except Exception: self.db.rollback(); raise
    @staticmethod
    def fingerprint(path):
        h=hashlib.sha256()
        with Path(path).open("rb") as stream:
            for chunk in iter(lambda:stream.read(1024*1024),b""): h.update(chunk)
        return h.hexdigest()
    def add_asset(self,path,width,height,orientation=1):
        path=Path(path); existing=self.db.execute("SELECT id FROM assets WHERE path=?",(str(path),)).fetchone()
        if existing: return int(existing[0])
        with self.transaction() as db:
            cur=db.execute("INSERT INTO assets(path,fingerprint,width,height,orientation,recipe_json) VALUES(?,?,?,?,?,?)",(str(path),self.fingerprint(path),width,height,orientation,json.dumps(Recipe().to_dict(),sort_keys=True)))
            return int(cur.lastrowid)
    def create_collection(self,name,selected_asset_ids:Sequence[int]=(),target=False):
        with self.transaction() as db:
            cur=db.execute("INSERT INTO collections(name) VALUES(?)",(name,)); cid=int(cur.lastrowid)
            db.executemany("INSERT OR IGNORE INTO collection_items(collection_id,asset_id) VALUES(?,?)",((cid,i) for i in selected_asset_ids))
            if target: db.execute("INSERT OR REPLACE INTO settings(key,value) VALUES('target_collection_id',?)",(str(cid),))
            return cid
    def set_target(self,cid):
        if not self.db.execute("SELECT 1 FROM collections WHERE id=?",(cid,)).fetchone(): raise ValueError("target collection does not exist")
        with self.transaction() as db: db.execute("INSERT OR REPLACE INTO settings(key,value) VALUES('target_collection_id',?)",(str(cid),))
    def target_collection(self):
        row=self.db.execute("SELECT value FROM settings WHERE key='target_collection_id'").fetchone()
        return int(row[0]) if row else None
    def add_selected_to_target(self,asset_ids):
        target=self.target_collection()
        if target is None: raise ValueError("no target collection is set")
        with self.transaction() as db: db.executemany("INSERT OR IGNORE INTO collection_items(collection_id,asset_id) VALUES(?,?)",((target,i) for i in asset_ids))
        return len(asset_ids)
    def collection_asset_ids(self,cid): return [int(r[0]) for r in self.db.execute("SELECT asset_id FROM collection_items WHERE collection_id=? ORDER BY asset_id",(cid,))]
    def begin_import(self, source_folder):
        with self.transaction() as db:
            cur=db.execute("INSERT INTO import_sessions(source_folder) VALUES(?)",(str(source_folder),))
            return int(cur.lastrowid)
    def record_import(self, session_id, asset_id, status, message=""):
        with self.transaction() as db:
            db.execute("INSERT INTO import_items(session_id,asset_id,status,message) VALUES(?,?,?,?)",(session_id,asset_id,status,message))
            column={"imported":"imported_count","deferred":"deferred_count","failed":"failed_count"}.get(status)
            if column: db.execute(f"UPDATE import_sessions SET {column}={column}+1 WHERE id=?",(session_id,))
    def last_import_asset_ids(self):
        row=self.db.execute("SELECT session_id FROM import_items WHERE status='imported' AND asset_id IS NOT NULL GROUP BY session_id ORDER BY session_id DESC LIMIT 1").fetchone()
        if row is None: return []
        return [int(r[0]) for r in self.db.execute("SELECT asset_id FROM import_items WHERE session_id=? AND status='imported' ORDER BY rowid",(row[0],))]
    def save_recipe(self,asset_id,recipe):
        payload=json.dumps(recipe.to_dict(),sort_keys=True)
        with self.transaction() as db:
            db.execute("UPDATE assets SET recipe_json=? WHERE id=?",(payload,asset_id)); cur=db.execute("INSERT INTO revisions(asset_id,recipe_json) VALUES(?,?)",(asset_id,payload)); return int(cur.lastrowid)
    def load_recipe(self,asset_id):
        row=self.db.execute("SELECT recipe_json FROM assets WHERE id=?",(asset_id,)).fetchone()
        if row is None: raise KeyError(asset_id)
        return Recipe.from_dict(json.loads(row[0]))
