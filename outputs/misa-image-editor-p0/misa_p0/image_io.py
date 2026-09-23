"""P0 image probing and RGB preview/export path."""
from __future__ import annotations
from dataclasses import dataclass
from pathlib import Path
from typing import Any
import numpy as np
from PIL import Image, ImageEnhance, ImageOps, ImageDraw, ImageFont

try:
    from pillow_heif import register_heif_opener

    register_heif_opener()
    HEIF_CODEC_AVAILABLE = True
except ImportError:
    HEIF_CODEC_AVAILABLE = False

try:
    import rawpy

    RAWPY_AVAILABLE = True
except ImportError:
    rawpy = None
    RAWPY_AVAILABLE = False

RAW_EXTENSIONS={".arw",".cr2",".cr3",".nef",".nrw",".raf",".orf",".rw2",".dng"}
HEIC_EXTENSIONS={".heic",".heif",".hif",".avif"}
@dataclass(frozen=True)
class Probe:
    path:str; format:str|None; width:int|None; height:int|None; orientation:int; status:str; message:str=""
def probe(path):
    source=Path(path); suffix=source.suffix.lower()
    if suffix in RAW_EXTENSIONS:
        if not RAWPY_AVAILABLE:
            return Probe(str(source), None, None, None, 1, "needs_codec_spike", "RAW requires LibRaw/RawSpeed")
        try:
            with rawpy.imread(str(source)) as raw:
                return Probe(str(source), suffix[1:].upper(), raw.sizes.width, raw.sizes.height, 1, "supported")
        except Exception as exc:
            return Probe(str(source), None, None, None, 1, "needs_codec_spike", f"RAW decoder present; fixture not decodable: {exc}")
    if suffix in HEIC_EXTENSIONS and not (HEIF_CODEC_AVAILABLE and suffix in {".heic", ".heif", ".hif"}):
        return Probe(str(source),None,None,None,1,"needs_codec_spike","HEIC/AVIF requires libheif")
    try:
        with Image.open(source) as image:
            exif=image.getexif(); return Probe(str(source),image.format,image.width,image.height,int(exif.get(274,1)),"supported")
    except Exception as exc: return Probe(str(source),None,None,None,1,"unsupported",str(exc))
def render_rgb(source,recipe:dict[str,Any]):
    with Image.open(source) as opened: image=ImageOps.exif_transpose(opened).convert("RGB")
    basic=recipe.get("basic",{}); exposure=float(basic.get("exposure",0))
    if exposure: image=ImageEnhance.Brightness(image).enhance(2.0**exposure)
    contrast=float(basic.get("contrast",0))
    if contrast: image=ImageEnhance.Contrast(image).enhance(max(0,1+contrast/100))
    saturation=float(basic.get("saturation",0))
    if saturation: image=ImageEnhance.Color(image).enhance(max(0,1+saturation/100))
    crop=recipe.get("crop",{}); left,top,right,bottom=[float(crop.get(k,d)) for k,d in (("left",0),("top",0),("right",1),("bottom",1))]
    if (left,top,right,bottom)!=(0,0,1,1):
        width,height=image.size; image=image.crop((round(left*width),round(top*height),round(right*width),round(bottom*height)))
    return image
def export_rgb(source,destination,recipe):
    image=render_rgb(source,recipe); watermark=recipe.get("watermark")
    if watermark and watermark.get("text"):
        draw=ImageDraw.Draw(image,"RGBA"); text=str(watermark["text"]); opacity=int(max(0,min(255,float(watermark.get("opacity",65))/100*255))); margin=int(watermark.get("margin",24)); font=ImageFont.load_default(); box=draw.textbbox((0,0),text,font=font)
        draw.text((image.width-(box[2]-box[0])-margin,image.height-(box[3]-box[1])-margin),text,fill=(255,255,255,opacity),font=font)
    output=Path(destination); output.parent.mkdir(parents=True,exist_ok=True)
    image.save(output,quality=95 if output.suffix.lower() in {".jpg",".jpeg"} else None)


def _apply_basic_float(rgb: np.ndarray, basic: dict[str, Any]) -> np.ndarray:
    """Apply the P0 subset in float32 RGB display space [0, 1]."""
    result = np.asarray(rgb, dtype=np.float32).copy()
    exposure = float(basic.get("exposure", 0))
    if exposure:
        result *= np.float32(2.0 ** exposure)
    contrast = float(basic.get("contrast", 0))
    if contrast:
        result = (result - 0.5) * np.float32(1.0 + contrast / 100.0) + 0.5
    luma = np.sum(result * np.array([0.2126, 0.7152, 0.0722], dtype=np.float32), axis=2, keepdims=True)
    shadows = float(basic.get("shadows", 0)) / 100.0
    highlights = float(basic.get("highlights", 0)) / 100.0
    if shadows:
        result += np.clip(1.0 - luma * 2.0, 0.0, 1.0) * shadows * 0.35
    if highlights:
        result += np.clip((luma - 0.5) * 2.0, 0.0, 1.0) * highlights * 0.35
    whites = float(basic.get("whites", 0)) / 100.0
    blacks = float(basic.get("blacks", 0)) / 100.0
    if whites:
        result += np.clip((luma - 0.65) / 0.35, 0.0, 1.0) * whites * 0.25
    if blacks:
        result += np.clip((0.35 - luma) / 0.35, 0.0, 1.0) * blacks * 0.25
    saturation = float(basic.get("saturation", 0))
    if saturation:
        result = luma + (result - luma) * np.float32(1.0 + saturation / 100.0)
    return np.clip(result, 0.0, 1.0)


def _rectangle_mask(height: int, width: int, mask: dict[str, Any]) -> np.ndarray:
    left = max(0.0, min(1.0, float(mask.get("left", 0))))
    top = max(0.0, min(1.0, float(mask.get("top", 0))))
    right = max(left, min(1.0, float(mask.get("right", 1))))
    bottom = max(top, min(1.0, float(mask.get("bottom", 1))))
    alpha = np.zeros((height, width), dtype=np.float32)
    alpha[round(top * height):round(bottom * height), round(left * width):round(right * width)] = 1.0
    return alpha


def _raster_mask(height: int, width: int, mask: dict[str, Any]) -> np.ndarray:
    path = mask.get("path")
    if not path:
        raise ValueError("raster mask requires a path")
    with Image.open(path) as opened:
        grayscale = ImageOps.exif_transpose(opened).convert("L").resize((width, height), Image.Resampling.BILINEAR)
        alpha = np.asarray(grayscale, dtype=np.float32) / np.float32(255.0)
    if bool(mask.get("invert", False)):
        alpha = np.float32(1.0) - alpha
    return alpha


def render_float(source, recipe: dict[str, Any]) -> np.ndarray:
    """Decode a supported RGB source, apply recipe, and return float32 RGB."""
    suffix = Path(source).suffix.lower()
    if suffix in RAW_EXTENSIONS and RAWPY_AVAILABLE:
        with rawpy.imread(str(source)) as raw:
            rgb = raw.postprocess(output_bps=8, use_camera_wb=True, no_auto_bright=True)
            rgb = np.asarray(rgb, dtype=np.float32) / 255.0
    else:
        with Image.open(source) as opened:
            image = ImageOps.exif_transpose(opened).convert("RGB")
            rgb = np.asarray(image, dtype=np.float32) / 255.0
    result = _apply_basic_float(rgb, recipe.get("basic", {}))
    mask = recipe.get("mask")
    if isinstance(mask, dict) and mask.get("type", "rectangle") in {"rectangle", "raster"}:
        alpha_builder = _rectangle_mask if mask.get("type", "rectangle") == "rectangle" else _raster_mask
        alpha = alpha_builder(result.shape[0], result.shape[1], mask)[..., None]
        local = _apply_basic_float(result, mask.get("basic", {}))
        result = result * (1.0 - alpha) + local * alpha
    crop = recipe.get("crop", {})
    left, top, right, bottom = [float(crop.get(k, d)) for k, d in (("left", 0), ("top", 0), ("right", 1), ("bottom", 1))]
    if (left, top, right, bottom) != (0, 0, 1, 1):
        height, width = result.shape[:2]
        result = result[round(top * height):round(bottom * height), round(left * width):round(right * width)]
    return np.clip(result, 0.0, 1.0).astype(np.float32, copy=False)


def render_rgb(source, recipe: dict[str, Any]):
    """Render the float pipeline to an 8-bit RGB preview image."""
    pixels = np.round(render_float(source, recipe) * 255.0).astype(np.uint8)
    return Image.fromarray(pixels, mode="RGB")
