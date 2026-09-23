from __future__ import annotations
import hashlib, json
import tempfile, unittest
from pathlib import Path
from PIL import Image, ImageDraw
try:
    import pillow_heif
except ImportError:
    pillow_heif = None
from misa_p0.catalog import Catalog
from misa_p0.image_io import HEIF_CODEC_AVAILABLE, probe, export_rgb, render_float
from misa_p0.importer import import_folder
from misa_p0.lens import EXIFREAD_AVAILABLE, LENSFUN_AVAILABLE, resolve_lens
from misa_p0.mask import infer_subject_mask, probe_offline_provider
from misa_p0.recipe import Recipe, RecipeSnapshot
from tools.verify_color_baseline import build_report
class P0Tests(unittest.TestCase):
    def test_fixture_manifest_hashes_match_files(self):
        root = Path(__file__).resolve().parents[1] / "fixtures"
        manifest = json.loads((root / "manifest.json").read_text(encoding="utf-8"))
        for entry in manifest["external"]:
            digest = hashlib.sha256((root / entry["path"]).read_bytes()).hexdigest()
            self.assertEqual(digest, entry["sha256"], entry["path"])

    def test_recipe_contract_declares_raster_mask(self):
        root = Path(__file__).resolve().parents[1]
        contract = json.loads((root / "windows" / "contracts" / "recipe-v1.json").read_text(encoding="utf-8"))
        mask = contract["properties"]["mask"]
        self.assertIn("raster", mask["properties"]["type"]["enum"])
        self.assertEqual(mask["properties"]["path"]["type"], "string")
        self.assertEqual(mask["properties"]["invert"]["type"], "boolean")

    def test_collection_target_and_b_are_idempotent(self):
        with tempfile.TemporaryDirectory() as tmp:
            root=Path(tmp); image=root/"a.png"; Image.new("RGB",(16,10),(255,0,0)).save(image); catalog=Catalog(root/"catalog.sqlite"); aid=catalog.add_asset(image,16,10); cid=catalog.create_collection("Target",target=True); catalog.add_selected_to_target([aid,aid]); self.assertEqual(catalog.collection_asset_ids(cid),[aid]); catalog.close()
    def test_catalog_reopens_with_target_and_recipe(self):
        with tempfile.TemporaryDirectory() as tmp:
            root=Path(tmp); image=root/"a.png"; Image.new("RGB",(16,10),(255,0,0)).save(image)
            catalog=Catalog(root/"catalog.sqlite"); aid=catalog.add_asset(image,16,10); cid=catalog.create_collection("Target",target=True); recipe=Recipe(); recipe.values["basic"]["exposure"]=1.0; catalog.save_recipe(aid,recipe); catalog.close()
            reopened=Catalog(root/"catalog.sqlite"); self.assertEqual(reopened.target_collection(),cid); self.assertEqual(reopened.load_recipe(aid).values["basic"]["exposure"],1.0); reopened.close()
    def test_recipe_copy_is_snapshot_and_paste_keeps_unselected_fields(self):
        source=Recipe(); source.values["basic"]["exposure"]=1.25; source.values["basic"]["contrast"]=20; copied=RecipeSnapshot.copy_from(1,source,["basic.exposure"]); source.values["basic"]["exposure"]=-2; destination=Recipe(); destination.values["basic"]["contrast"]=-20; pasted=copied.apply_to(destination); self.assertEqual(pasted.values["basic"]["exposure"],1.25); self.assertEqual(pasted.values["basic"]["contrast"],-20)
    def test_supported_probe_and_export(self):
        with tempfile.TemporaryDirectory() as tmp:
            root=Path(tmp); source=root/"a.png"; target=root/"out"/"a.jpg"; Image.new("RGB",(20,12),(40,80,120)).save(source); self.assertEqual(probe(source).status,"supported"); export_rgb(source,target,{"basic":{"exposure":0.5},"watermark":{"text":"MISA","opacity":50}}); self.assertTrue(target.exists()); rendered=Image.open(target); self.assertEqual(rendered.size,(20,12)); rendered.close()
    def test_color_baseline_is_within_p0_display_threshold(self):
        report = build_report()
        self.assertTrue(report["passed"], report)
    def test_import_session_tracks_supported_and_deferred(self):
        with tempfile.TemporaryDirectory() as tmp:
            root=Path(tmp); Image.new("RGB",(8,6),(1,2,3)).save(root/"ok.png"); (root/"camera.ARW").write_bytes(b"placeholder"); catalog=Catalog(root/"catalog.sqlite"); result=import_folder(catalog,root); self.assertEqual(len(result["imported_asset_ids"]),1); self.assertEqual(len(result["deferred_paths"]),1); self.assertEqual(catalog.last_import_asset_ids(),result["imported_asset_ids"]); catalog.close()
    def test_empty_later_import_does_not_erase_last_successful_import(self):
        with tempfile.TemporaryDirectory() as tmp:
            root=Path(tmp); image=root/"ok.png"; Image.new("RGB",(8,6),(1,2,3)).save(image); catalog=Catalog(root/"catalog.sqlite"); first=import_folder(catalog,root); empty=root/"empty"; empty.mkdir(); catalog.begin_import(empty); self.assertEqual(catalog.last_import_asset_ids(),first["imported_asset_ids"]); catalog.close()
    def test_raw_probe_is_explicitly_deferred(self): self.assertEqual(probe("sample.ARW").status,"needs_codec_spike")
    def test_external_raw_fixtures_decode_and_render(self):
        fixture_root = Path(__file__).resolve().parents[1] / "fixtures" / "external"
        names = [
            "rawpy-upstream/iss030e122639.NEF",
            "rawpy-upstream/iss042e297200.NEF",
            "rawpy-upstream/RAW_CANON_40D_SRAW_V103.CR2",
            "rawpy-upstream/RAW_CANON_5DMARK2_PREPROD.CR2",
            "raw-pixls-cc0/Sony-DSLR-A300.ARW",
            "raw-pixls-cc0/Sony-SLT-A77.ARW",
            "raw-pixls-cc0/Canon-EOS-M50-CRAW.CR3",
            "raw-pixls-cc0/Canon-EOS-R.CR3",
            "raw-pixls-cc0/Nikon-Coolpix-P330.NRW",
            "raw-pixls-cc0/Nikon-Z6-lossless.NEF",
        ]
        for relative in names:
            result = probe(fixture_root / relative)
            name = Path(relative).name
            self.assertEqual(result.status, "supported", name)
            pixels = render_float(fixture_root / relative, {})
            self.assertEqual(pixels.dtype.name, "float32")
            self.assertEqual(pixels.shape[2], 3)
    def test_heic_codec_is_explicitly_available_or_deferred(self):
        with tempfile.TemporaryDirectory() as tmp:
            source = Path(tmp) / "sample.heic"
            if not HEIF_CODEC_AVAILABLE:
                self.assertEqual(probe(source).status, "needs_codec_spike")
                return
            Image.new("RGB", (12, 8), (40, 80, 120)).save(source, format="HEIF", quality=90)
            self.assertEqual(probe(source).status, "supported")
            self.assertEqual(render_float(source, {}).shape, (8, 12, 3))
    def test_generated_heif10_fixture_reports_ten_bit(self):
        if not HEIF_CODEC_AVAILABLE or pillow_heif is None:
            self.skipTest("pillow-heif unavailable")
        fixture = Path(__file__).resolve().parents[1] / "fixtures" / "generated" / "p0-heif10.heif"
        opened = pillow_heif.open_heif(fixture)
        self.assertEqual(opened[0].info.get("bit_depth"), 10)
        self.assertEqual(probe(fixture).status, "supported")
        pixels = render_float(fixture, {})
        self.assertEqual(pixels.dtype.name, "float32")
        self.assertEqual(pixels.shape, (192, 256, 3))
    def test_external_camera_heif_fixture_decodes_and_renders(self):
        if not HEIF_CODEC_AVAILABLE:
            self.skipTest("pillow-heif unavailable")
        fixture = Path(__file__).resolve().parents[1] / "fixtures" / "external" / "rawdb-cc0" / "Canon-PowerShot-V1-ISO-200-HDR.HIF"
        result = probe(fixture)
        self.assertEqual(result.status, "supported")
        self.assertEqual(result.format, "HEIF")
        pixels = render_float(fixture, {})
        self.assertEqual(pixels.dtype.name, "float32")
        self.assertEqual(pixels.shape, (5760, 3840, 3))
    def test_float_pipeline_applies_manual_rectangle_mask(self):
        with tempfile.TemporaryDirectory() as tmp:
            source = Path(tmp) / "gradient.png"
            Image.new("RGB", (20, 10), (40, 40, 40)).save(source)
            pixels = render_float(source, {"mask": {"type": "rectangle", "left": 0, "top": 0, "right": 0.5, "bottom": 1, "basic": {"exposure": 1}}})
            self.assertEqual(pixels.dtype.name, "float32")
            self.assertGreater(float(pixels[:, 2, 0].mean()), float(pixels[:, 15, 0].mean()))
    def test_float_pipeline_applies_raster_mask(self):
        with tempfile.TemporaryDirectory() as tmp:
            root = Path(tmp)
            source = root / "gradient.png"
            mask_path = root / "mask.png"
            Image.new("RGB", (20, 10), (40, 40, 40)).save(source)
            mask_image = Image.new("L", (20, 10), 0)
            ImageDraw.Draw(mask_image).rectangle((0, 0, 9, 9), fill=255)
            mask_image.save(mask_path)
            base = render_float(source, {})
            pixels = render_float(source, {"mask": {"type": "raster", "path": str(mask_path), "basic": {"exposure": 1}}})
            self.assertGreater(float(pixels[:, 2, 0].mean()), float(base[:, 2, 0].mean()))
            self.assertAlmostEqual(float(pixels[:, 15, 0].mean()), float(base[:, 15, 0].mean()), places=3)
    def test_float_pipeline_inverts_raster_mask_for_background(self):
        with tempfile.TemporaryDirectory() as tmp:
            root = Path(tmp)
            source = root / "gradient.png"
            mask_path = root / "mask.png"
            Image.new("RGB", (20, 10), (40, 40, 40)).save(source)
            mask_image = Image.new("L", (20, 10), 0)
            ImageDraw.Draw(mask_image).rectangle((0, 0, 9, 9), fill=255)
            mask_image.save(mask_path)
            base = render_float(source, {})
            pixels = render_float(source, {"mask": {"type": "raster", "path": str(mask_path), "invert": True, "basic": {"exposure": 1}}})
            self.assertAlmostEqual(float(pixels[:, 2, 0].mean()), float(base[:, 2, 0].mean()), places=3)
            self.assertGreater(float(pixels[:, 15, 0].mean()), float(base[:, 15, 0].mean()))
    def test_render_normalizes_exif_orientation_before_export(self):
        with tempfile.TemporaryDirectory() as tmp:
            source = Path(tmp) / "oriented.jpg"
            image = Image.new("RGB", (20, 10), (30, 60, 90))
            exif = image.getexif()
            exif[274] = 6
            image.save(source, exif=exif)
            self.assertEqual(probe(source).orientation, 6)
            self.assertEqual(render_float(source, {}).shape, (20, 10, 3))
    def test_lens_resolver_reads_exif_and_does_not_fake_calibration(self):
        with tempfile.TemporaryDirectory() as tmp:
            source = Path(tmp) / "lens.jpg"
            image = Image.new("RGB", (20, 10), (30, 60, 90))
            exif = image.getexif()
            exif[271] = "Sony"
            exif[272] = "ILCE-7M4"
            exif[42036] = "FE 24-70mm F2.8 GM"
            image.save(source, exif=exif)
            result = resolve_lens(source)
            self.assertEqual(result.status, "profile_available" if LENSFUN_AVAILABLE else "profile_missing")
            if LENSFUN_AVAILABLE: self.assertIsNotNone(result.profile_id)
            else: self.assertIsNone(result.profile_id)
    def test_lens_resolver_reads_public_raw_exif_when_available(self):
        if not EXIFREAD_AVAILABLE:
            self.skipTest("ExifRead unavailable")
        root = Path(__file__).resolve().parents[1] / "fixtures" / "external" / "raw-pixls-cc0"
        sony = resolve_lens(root / "Sony-SLT-A77.ARW")
        self.assertEqual(sony.make, "SONY")
        self.assertEqual(sony.model, "SLT-A77V")
        self.assertEqual(sony.lens_model, "20mm F2.8")
        self.assertIn(sony.status, {"profile_available", "profile_missing", "unmatched"})
        nikon = resolve_lens(root / "Nikon-Z6-lossless.NEF")
        self.assertEqual(nikon.make, "NIKON CORPORATION")
        self.assertEqual(nikon.model, "NIKON Z 6")
        self.assertEqual(nikon.lens_model, "NIKKOR Z 24-70mm f/4 S")
        self.assertEqual(nikon.status, "profile_available" if LENSFUN_AVAILABLE else "unmatched")
    def test_mask_provider_reports_missing_model_without_faking_ai(self):
        status = probe_offline_provider()
        self.assertIn(status.model_status, {"model_missing", "runtime_missing"})
    def test_offline_subject_model_loads_and_returns_mask(self):
        model = Path(__file__).resolve().parents[1] / "models" / "modnet_photographic.onnx"
        source = Path(__file__).resolve().parents[1] / "fixtures" / "generated" / "rawdb-canon-v1-heif-preview.jpg"
        if not model.exists():
            self.skipTest("MODNet model not checked in")
        status = probe_offline_provider(model)
        self.assertEqual(status.model_status, "ready")
        mask = infer_subject_mask(source, model)
        self.assertEqual(mask.dtype.name, "float32")
        self.assertEqual(mask.shape, (1600, 1067))
        self.assertTrue(bool((mask >= 0).all() and (mask <= 1).all()))
        self.assertGreater(float(mask.max()), 0.1)
