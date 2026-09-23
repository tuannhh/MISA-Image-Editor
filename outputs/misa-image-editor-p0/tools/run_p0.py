from __future__ import annotations
import json, sys, tempfile
from pathlib import Path
from PIL import Image
sys.path.insert(0,str(Path(__file__).resolve().parents[1]))
from misa_p0.catalog import Catalog
from misa_p0.image_io import probe, export_rgb
from misa_p0.recipe import Recipe, RecipeSnapshot
def main():
    with tempfile.TemporaryDirectory(prefix="misa-p0-") as tmp:
        root=Path(tmp); source=root/"fixture.png"; output=root/"exports"/"fixture.jpg"; Image.new("RGB",(64,48),(30,90,180)).save(source); catalog=Catalog(root/"catalog.sqlite"); aid=catalog.add_asset(source,64,48); cid=catalog.create_collection("P0 target",target=True); catalog.add_selected_to_target([aid]); recipe=Recipe(); recipe.values["basic"]["exposure"]=0.5; recipe.values["watermark"]={"text":"MISA P0","opacity":65}; copied=RecipeSnapshot.copy_from(aid,recipe,["basic.exposure","watermark"]); catalog.save_recipe(aid,copied.apply_to(catalog.load_recipe(aid))); export_rgb(source,output,catalog.load_recipe(aid).to_dict()["values"]); print(json.dumps({"asset_id":aid,"target_collection_id":cid,"target_assets":catalog.collection_asset_ids(cid),"probe":probe(source).__dict__,"export":str(output),"recipe_digest":catalog.load_recipe(aid).digest()},indent=2)); catalog.close()
if __name__=="__main__": main()