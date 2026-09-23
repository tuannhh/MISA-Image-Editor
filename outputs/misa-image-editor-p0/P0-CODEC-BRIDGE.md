# P0 RAW and camera HEIF preview bridge

The Library keeps RAW and camera HEIF files as catalog references, then generates a cached JPEG
preview when a deferred item is selected. The WPF process starts `tools/generate_preview.py`, which
uses rawpy/LibRaw for ARW, CR2, CR3, NEF, NRW and related RAW files, and pillow-heif for HEIC, HEIF,
HIF and AVIF. The preview is written under `%LOCALAPPDATA%\\MisaImageEditor\\previews` using a key
that changes when the source path, size, or modification time changes.

The cached preview participates in the same non-destructive recipe preview, mask, crop, watermark,
and JPEG/PNG export path as JPEG/PNG. The original file is never modified. A missing Python provider
or decoder is reported in the status bar and leaves the catalog item intact.

This bridge is a P0 codec spike and preview path. It does not claim production RAW color management,
full-resolution RAW export, TIFF16 output, or every camera compression variant. The checked matrix
currently covers representative Sony ARW, Canon CR3, Nikon NEF, and Canon HIF fixtures; user camera
files must still be added before a release-quality codec gate.
