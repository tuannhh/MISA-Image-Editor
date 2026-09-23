# P0 batch export handoff

Date: 2026-09-17. Status: candidate, not stable.

`Export selected...` now supports a parent folder, optional relative child folder, JPEG/PNG choice, and per-photo recipe rendering for selected JPEG/PNG assets. It applies Basic and raster-mask edits plus persisted resize/watermark settings. Existing filenames receive `-edited` and a numeric suffix on collision. RAW/HEIF entries are counted as skipped until native decoding is integrated.

The path validator prevents rooted or parent-traversing child folders. The next owner should exercise the dialog on a real album and compare output pixels before adding RAW/HEIF export.
