# Handoff: P0 automatic subject mask bridge

## Included

- WPF Mask panel action for automatic subject masks on JPEG/PNG previews.
- Child-process boundary through `tools/generate_subject_mask.py`.
- Checked-in MODNet Photographic ONNX model copied into the published candidate.
- Original-resolution grayscale output attached to the existing raster-mask recipe.

## Runtime requirement

The candidate app remains framework-dependent on .NET 10 Desktop Runtime. Automatic masks also
need Python with `numpy`, `Pillow`, and `onnxruntime`; use `MISA_PYTHON` to point to the intended
interpreter. If the provider is missing, the UI reports the failure and leaves the recipe unchanged.

## Validation gate before expanding scope

Run the standalone bridge against a representative JPEG/PNG set, record provider and output-size
results, then evaluate subject/background quality. Replace the process provider with native/WinML
only after the acceptance set and RAW/HEIF preview path are defined.
