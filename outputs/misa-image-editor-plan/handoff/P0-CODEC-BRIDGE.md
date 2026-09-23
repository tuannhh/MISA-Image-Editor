# Handoff: P0 RAW and camera HEIF preview bridge

## Included

- Deferred RAW and camera HEIF catalog entries generate cached JPEG previews on selection.
- `tools/generate_preview.py` runs outside the WPF process and uses rawpy/LibRaw or pillow-heif.
- Cached previews feed the existing non-destructive recipe, mask, crop, watermark, and JPEG/PNG export path.
- Representative Sony ARW, Canon CR3, Nikon NEF, and Canon HIF verification is recorded in
  `fixtures/preview-bridge-verification.json`.

## Runtime requirement

The framework-dependent candidate needs .NET 10 Desktop Runtime and a Python environment containing
Pillow, NumPy, rawpy, and pillow-heif. Set `MISA_PYTHON` when Python is not on `PATH`. Missing
providers are reported and do not remove the catalog reference.

## Gate before production RAW support

Add user-camera fixtures and verify orientation, camera color, highlights, memory, and export quality.
Then replace or harden this process bridge with the native worker/IPC path described in ADR-0001.
