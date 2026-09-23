"""Versioned non-destructive edit recipes."""
from __future__ import annotations
from dataclasses import dataclass, field
import copy, hashlib, json
from typing import Any, Iterable

RECIPE_SCHEMA_VERSION = 1

def _defaults():
    return {"basic":{"exposure":0.0,"contrast":0.0,"highlights":0.0,"shadows":0.0,"whites":0.0,"blacks":0.0,"saturation":0.0},
            "crop":{"left":0.0,"top":0.0,"right":1.0,"bottom":1.0},"watermark":None}

@dataclass
class Recipe:
    schema_version: int = RECIPE_SCHEMA_VERSION
    process_version: str = "p0-rgb-1"
    values: dict[str, Any] = field(default_factory=_defaults)
    def snapshot(self): return Recipe(self.schema_version,self.process_version,copy.deepcopy(self.values))
    def to_dict(self): return {"schema_version":self.schema_version,"process_version":self.process_version,"values":copy.deepcopy(self.values)}
    @classmethod
    def from_dict(cls, raw):
        if int(raw.get("schema_version",0)) != RECIPE_SCHEMA_VERSION: raise ValueError("unsupported recipe schema")
        values=_defaults(); values.update(copy.deepcopy(raw.get("values",{})))
        return cls(RECIPE_SCHEMA_VERSION,str(raw.get("process_version","p0-rgb-1")),values)
    def digest(self):
        return hashlib.sha256(json.dumps(self.to_dict(),sort_keys=True,separators=(",",":")).encode()).hexdigest()

@dataclass(frozen=True)
class RecipeSnapshot:
    source_asset_id: int
    selected_paths: tuple[str,...]
    recipe: dict[str,Any]
    @classmethod
    def copy_from(cls, asset_id, recipe: Recipe, paths: Iterable[str]):
        selected=tuple(dict.fromkeys(paths)); picked={}
        for path in selected:
            value=recipe.values
            for part in path.split("."):
                if not isinstance(value,dict) or part not in value: raise KeyError(path)
                value=value[part]
            cursor=picked; parts=path.split(".")
            for part in parts[:-1]: cursor=cursor.setdefault(part,{})
            cursor[parts[-1]]=copy.deepcopy(value)
        return cls(asset_id,selected,picked)
    def apply_to(self,destination: Recipe):
        result=destination.snapshot()
        for path in self.selected_paths:
            source=self.recipe
            for part in path.split("."): source=source[part]
            cursor=result.values; parts=path.split(".")
            for part in parts[:-1]: cursor=cursor.setdefault(part,{})
            cursor[parts[-1]]=copy.deepcopy(source)
        return result