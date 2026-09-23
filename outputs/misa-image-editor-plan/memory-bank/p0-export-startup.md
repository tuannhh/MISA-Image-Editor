# P0 export and startup memory

Updated 2026-09-17.

- WPF P0 now has JPEG/PNG export from the active preview.
- Export accepts optional width/height, preserving aspect ratio when only one is supplied.
- Export accepts text watermark, opacity, and five positions; the recipe stores `watermark.position`, `watermark.width`, and `watermark.height`.
- `App.xaml.cs` catches dispatcher, unhandled-domain, and unobserved-task exceptions. The full exception is appended to `%LOCALAPPDATA%\MisaImageEditor\startup-error.log` and dispatcher failures show the log path.
- Release solution build, domain smoke, native CTest, publish-folder startup smoke, and extracted-ZIP startup smoke passed after this change.
- Candidate ZIP SHA-256 is recorded in `outputs/misa-image-editor-p0/artifacts/P0-CANDIDATE-SHA256.txt`.
- Copy Settings now presents a field-group dialog (Basic, Crop/Transform, Mask, Watermark/resize); Paste applies the selected groups to all selected photos while preserving unselected destination fields.
- Export selected now writes recipe-rendered JPEG/PNG files to a chosen parent plus optional relative child folder, with collision-safe names and deferred RAW/HEIF skip counts.
- Crop geometry now supports original/1:1/4:3/3:2/16:9 plus -45..45 degree manual rotation, persisted in recipe-v1 and applied to preview and batch export; automatic straighten remains gated on a tested model.
- Tone Curve now has four parametric regions (Shadows, Darks, Lights, Highlights), persists under `tone_curve`, renders in preview/batch export, and is selectable in Copy Settings; point-curve and HSL remain later scope.
- Color Mixer now has eight HSL ranges with Hue/Saturation/Luminance controls, persists under `color_mixer.channels`, renders in preview/batch export, and is selectable in Copy Settings; point sampling/range visualization remain later scope.
- Lens Correction now stores profile metadata, applies bounded bilinear manual distortion and manual vignetting in JPEG/PNG/bridge preview and batch export, and transfers through Copy Settings; calibrated profile coefficients, TCA/defringe, and native worker integration remain gated on verified Lensfun/EXIF data.
- Manual brush mask is now available for previewable JPEG/PNG: size and hardness are painted into an original-resolution grayscale PNG under LocalAppData and attached to the existing raster-mask recipe path; undo/pressure remain later scope.
- Automatic subject mask now has a process bridge for previewable JPEG/PNG. The WPF shell launches `tools/generate_subject_mask.py` with the bundled MODNet Photographic ONNX model, then attaches the generated grayscale PNG to the same raster-mask recipe path. The provider is intentionally replaceable and requires a Python environment with NumPy, Pillow, and ONNX Runtime; RAW/HEIF and quality acceptance remain deferred.
- JSON presets now save the recipe snapshot plus the exact Copy Settings field keys. Applying a preset uses the same non-destructive field-selective copy path as clipboard paste and can target the current multi-selection; preset save/load/delete is covered by the C# domain smoke.
- RAW and camera HEIF catalog items now generate a cached JPEG preview on selection through `tools/generate_preview.py`. The bridge uses rawpy/LibRaw or pillow-heif in the configured Python environment, feeds the same WPF recipe/mask/export path, and never edits the original. The representative Sony ARW, Canon CR3, Nikon NEF, and Canon HIF matrix passed; production RAW color/export and user-camera coverage remain deferred.
- This remains a candidate handoff; native IPC, installer, brush undo/pressure, production RAW color/export, and automatic subject/background quality are deferred even though cached RAW/HEIF preview is now available through the process bridge.
