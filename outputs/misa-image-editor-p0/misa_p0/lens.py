"""P0 lens metadata resolver with explicit unprofiled states."""
from __future__ import annotations

from dataclasses import dataclass
from pathlib import Path

from PIL import Image

try:
    import exifread

    EXIFREAD_AVAILABLE = True
except ImportError:
    exifread = None
    EXIFREAD_AVAILABLE = False

try:
    import lensfunpy

    LENSFUN_AVAILABLE = True
    _LENSFUN_DB = lensfunpy.Database()
except ImportError:
    lensfunpy = None
    LENSFUN_AVAILABLE = False
    _LENSFUN_DB = None


KNOWN_LENS_ALIASES = {
    "Canon EF 24-70mm f/2.8L": ("Canon", "24-70"),
    "Sony FE 18-135mm F3.5-5.6 OSS": ("Sony", "18-135"),
    "Sony FE 24-70mm F2.8 GM": ("Sony", "24-70"),
    "Sony FE 70-200mm F2.8 GM": ("Sony", "70-200"),
    "Sony FE 16-35mm F2.8 GM": ("Sony", "16-35"),
    "Sony FE 100-400mm GM OSS": ("Sony", "100-400"),
    "Tamron 17-70mm F/2.8 Di III-A VC RXD": ("Tamron", "17-70"),
    "Sigma 24-70mm F2.8 DG DN Art": ("Sigma", "24-70"),
}

LENSFUN_MAKER_ALIASES = {
    "canon": "Canon",
    "nikon corporation": "Nikon",
    "sony": "Sony",
}


@dataclass(frozen=True)
class LensResolution:
    make: str
    model: str
    lens_model: str
    status: str
    profile_id: str | None = None
    message: str = ""


def _read_metadata(source: str | Path) -> tuple[str, str, str]:
    """Read make/model/lens from RGB containers first, then RAW-capable EXIF tags."""
    path = Path(source)
    make = model = lens_model = ""
    try:
        with Image.open(path) as image:
            exif = image.getexif()
            make = str(exif.get(271, "")).strip()
            model = str(exif.get(272, "")).strip()
            lens_model = str(exif.get(42036, "")).strip()
    except Exception:
        pass
    if lens_model or not EXIFREAD_AVAILABLE:
        return make, model, lens_model
    try:
        with path.open("rb") as stream:
            tags = exifread.process_file(stream, details=False)
        exif_make = str(tags.get("Image Make", "")).strip()
        exif_model = str(tags.get("Image Model", "")).strip()
        exif_lens = str(tags.get("EXIF LensModel", tags.get("MakerNote LensModel", ""))).strip()
        make = exif_make or make
        model = exif_model or model
        lens_model = exif_lens or lens_model
    except Exception:
        pass
    return make, model, lens_model


def resolve_lens(source: str | Path) -> LensResolution:
    make, model, lens_model = _read_metadata(source)
    if not (make or model or lens_model):
        return LensResolution("", "", "", "metadata_unreadable", message="No readable EXIF make/model/lens metadata")

    if LENSFUN_AVAILABLE and make and model and lens_model:
        try:
            lensfun_maker = LENSFUN_MAKER_ALIASES.get(make.casefold(), make)
            cameras = _LENSFUN_DB.find_cameras(lensfun_maker, model, loose_search=True)
            if cameras:
                matches = _LENSFUN_DB.find_lenses(cameras[0], maker=lensfun_maker, lens=lens_model, loose_search=True)
                if matches:
                    profile = matches[0]
                    return LensResolution(make, model, lens_model, "profile_available", f"lensfun:{profile.maker}:{profile.model}", "Lensfun calibration database match")
        except Exception:
            # Metadata matching remains best effort; unresolved profiles are
            # explicit and fall through to the alias resolver below.
            pass

    haystack = f"{make} {model} {lens_model}".lower()
    for profile_name, (brand, focal_range) in KNOWN_LENS_ALIASES.items():
        if brand.lower() in haystack and focal_range in haystack:
            return LensResolution(make, model, lens_model, "profile_missing", message=f"recognized lens family; calibration required: {profile_name}")
    return LensResolution(make, model, lens_model, "unmatched", message="no verified profile alias")
