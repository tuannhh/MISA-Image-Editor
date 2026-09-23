# P0 technical report

Ngày kiểm tra: 21/09/2026. Đây là prototype, chưa phải bản Windows stable.

## English summary

**Checked on 2026-09-21. This is a P0 technical prototype, not a stable Windows release.** The latest repeated release gate passed 13 steps: Python regression 22/22, dependency and codec/model/lens/colour checks, a clean .NET Release build, C# domain smoke, and native CTest. The published candidate passed clean-log startup smoke from both its publish folder and extracted ZIP.

The candidate is suitable for evaluating the Library → edit → selective copy/paste or preset → batch export workflow. JPEG/PNG is the best-tested interactive path. RAW, HEIF/HEIC, Lensfun resolution, and offline MODNet masks are technical prototype paths with representative fixtures, not a claim of camera-wide colour or AI-quality acceptance. The four requested lens variants without exact Lensfun entries, the user camera matrix, native IPC, installer, and stable-release acceptance remain open.

The application now includes a Vietnamese/English selector. It stores the chosen language in `%LOCALAPPDATA%\MisaImageEditor\language.json`; `LocalizationService` uses WPF bindings for the Library, Editor, and P0 dialogs without a new runtime dependency. The bilingual documentation policy is in `../misa-image-editor-plan/BILINGUAL-DOCUMENTATION.md`.

## Bằng chứng

- python -m unittest discover -s tests -v: 22/22 test qua (exit code 0), gồm reopen catalog/recipe, last-import semantics, fixture hash verification, recipe-v1 raster-mask contract, float RGB, EXIF orientation, lens EXIF resolver cho JPEG và RAW thật, HEIC 8/10-bit codec probe, một camera HIF thật, mười fixture RAW, raster local-mask application với subject/background invert, offline MODNet mask inference và display-RGB color baseline.
- python tools/run_p0.py: exit code 0; tạo catalog tạm, import session, target collection, logic B idempotent, recipe snapshot và JPEG có watermark.
- python tools/benchmark_p0.py: synthetic 12 MP đạt 1.000 s, 12.00 MP/s, peak working set 756.70 MB; Canon PowerShot V1 HIF 22.118 MP đạt 2.5945 s, 8.53 MP/s, 1337.55 MB; Canon EOS M50 CRAW/CR3 24.216 MP đạt 2.2695 s, 10.67 MP/s, 1272.14 MB trên máy hiện tại. Đây là baseline latency/RSS tham khảo, chưa phải cam kết máy mục tiêu và dao động theo tải máy; JSON lưu trong `fixtures/benchmark-*.json`.
- Matrix benchmark bổ sung: Sony SLT-A77 ARW 24.241 MP, 2.4945 s, 9.72 MP/s, 1271.02 MB; Nikon Z6 NEF 24.499 MP, 2.7184 s, 9.01 MP/s, 1283.81 MB; Canon EOS R CR3 30.326 MP, 2.8231 s, 10.74 MP/s, 1573.24 MB. Các số này là đo decode/render rawpy trên máy hiện tại, chưa phải ngưỡng phát hành.
- `tools/verify_raw_fixtures.py`: mười RAW (4 NEF, 2 CR2 upstream + 2 ARW, 2 CR3, 1 NRW public CC0) đều probe/decode/render thành công; hash và preview nằm trong `fixtures/raw-fixture-verification.json`.
- `fixtures/manifest.json`: PNG/JPEG, synthetic HEIC 8-bit + HEIF 10-bit, một Canon PowerShot V1 HIF camera fixture và mười RAW external vectors có SHA-256; HEIF/HIF camera đang `partial_fixture` (một model), HEIC `.heic` matrix vẫn thiếu, ARW/CR3/NEF/NRW `partial_fixture` theo model public đã ghi.
- `tools/verify_camera_heif_fixtures.py`: Canon HIF probe/decode/render thành công, 3840×5760, 10-bit, SHA-256/provenance/preview nằm trong `fixtures/camera-heif-verification.json`.
- `tools/verify_mask_model.py`: MODNet Photographic ONNX load/inference thành công qua AzureExecutionProvider + CPU fallback; hash/license, mask preview và số liệu nằm trong `fixtures/mask-model-verification.json`.
- `tools/verify_color_baseline.py`: deterministic sRGB display pipeline baseline pass; PNG mean ΔE76 0.0000, JPEG source 0.5040, HEIC source 0.6086 và P0 JPEG export 0.5984. Báo cáo ở `fixtures/color-baseline-verification.json`; không phải chứng nhận màu RAW camera hoặc Adobe equivalence.
- Benchmark 21/09/2026: Sony A77 ARW 24.241 MP trong 1.8736 s (12.94 MP/s, 1271.60 MB); Canon M50 CR3 24.216 MP trong 1.8594 s (13.02 MP/s, 1270.79 MB); Nikon Z6 NEF 24.499 MP trong 2.1593 s (11.35 MP/s, 1284.07 MB); Canon V1 HIF 22.118 MP trong 2.0502 s (10.79 MP/s, 1335.57 MB). Đây là local baseline Windows 10 Pro x64, không phải SLA máy mục tiêu.
- Môi trường Python bundled đã cài đủ các binding ghim trong `requirements-p0.txt` và dependency của ONNX Runtime; `pip check` báo không có dependency hỏng.
- `tools/verify_dependencies.py` ghi phiên bản Python/binding/dotnet/CMake/MSVC/Git và ONNX providers vào `fixtures/dependency-verification.json`.
- `tools/lensfun_coverage.py`: 9/13 requested lens variants have exact Lensfun entries; missing entries are Sony 18-135 f/4G, 70-200 GM II, 16-35 GM II and 100-400 GM II. Lensfun has a nearest but different Sony E 18-135mm f/3.5-5.6 OSS entry; it is recorded as a nearest match, not silently used as the requested profile.
- Raw lens metadata spike: ExifRead reads public Sony ARW and Nikon NEF EXIF. Nikon Z6 + NIKKOR Z 24-70mm f/4 S resolves to exact Lensfun calibration; Sony A77 + 20mm F2.8 is explicitly unmatched. `requirements-p0.txt` and the installed runtime are synchronized, including ExifRead 3.5.1.
- `dotnet clean` followed by `dotnet build windows/MisaImageEditor.slnx --configuration Release`: clean Release build exit code 0, 0 warning, 0 error, including the domain smoke project.
- CMake/MSVC native worker Release build: exit code 0; `ctest` 1/1 passed (`image_worker_smoke`).
- C# `JsonCatalogStore` smoke: target, last import (including an empty later import), idempotent membership, SHA-256 fingerprint update and recipe preservation across re-import/source change survive close/reopen; exit code 0.
- C# recipe serialization smoke: `schema_version`/`process_version` and the Basic/Crop/Watermark/Mask field names match `windows/contracts/recipe-v1.json`; raster mask path/invert survives serialization and camelCase drift is rejected.
- WPF P0 mask panel now lets the user choose/clear a raster mask, paint an adjustable brush mask, invoke the offline subject-mask bridge, invert it for background work, and set local exposure; the same recipe state is applied to the preview. Stroke undo, pressure, and automatic refinement remain future work.
- WPF Library now has `Show last import` and `Show all catalog` views backed by the persisted `LastImportPaths` list; all recognized files are retained in that session, including RAW/HEIF items whose preview status is deferred. Filtering does not rewrite recipes or collections.
- Domain smoke also verifies that re-importing an existing asset without a recipe payload preserves its saved recipe.
- WPF startup smoke: executable stays responsive with window title `MISA Image Editor - P0`; an initialization null-reference was fixed before this result.
- Framework-dependent Windows P0 candidate was published to `artifacts/p0-candidate/app`, launched from the publish directory, and stayed responsive; ZIP artifact SHA-256 is recorded in `artifacts/P0-CANDIDATE-SHA256.txt`.
- WPF Basic preview now applies exposure/contrast/highlights/shadows/whites/blacks/saturation to the active JPEG/PNG preview in memory; recipe remains persisted separately and source files are untouched.
- WPF, Python float, and native Basic paths now share the same pre-tone luminance rule for saturation, reducing preview/worker drift at the P0 contract boundary.
- Float render/export accepts normalized rectangle masks and raster mask paths with optional invert, so an offline subject model output can be complemented into a background mask in the same local-adjustment graph without changing the source file.
- WPF folder import allowlist recognizes Canon `.HIF` alongside `.HEIC`/`.HEIF`; deferred RAW/camera-HEIF items now generate a cached JPEG preview on selection through the local rawpy/LibRaw or pillow-heif bridge, then use the same recipe and JPEG/PNG export path.
- `windows/contracts/recipe-v1.json` parses successfully with Python's JSON parser and declares both rectangle/raster mask forms.
- `outputs/misa-image-editor-plan/ADR-0001-platform-and-pipeline.md` records the P0 platform decision, JSON catalog boundary, native worker boundary and follow-up gates.
- JPEG/PNG được probe bằng Pillow với format/kích thước/orientation.
- Một ARW, CR3 và NRW public sample đã probe/decode/render pass qua rawpy; chúng chỉ là `partial_fixture`, không suy diễn sang các model Sony/Canon/Nikon người dùng sẽ chốt. Hai NEF và hai CR2 upstream cũng pass. Một Canon PowerShot V1 HIF thật đã probe/decode/render pass qua pillow-heif; đây mới là một model trong ma trận HEIF.
- Native worker contract đã có validation recipe và đủ bảy trường Basic RGBA8 pixel path; `ctest` smoke kiểm tra alpha được giữ nguyên và input invalid bị từ chối.

## Môi trường hiện tại

| Thành phần | Kết quả |
|---|---|
| Python 3.12.14 | Có |
| Pillow, NumPy | Có |
| .NET SDK | Có: .NET 10.0.401; WPF Release build 0 warning/0 error |
| CMake | Có: CMake 4.4.3 |
| C++ compiler | Có MSVC 19.44.35229.0 (Build Tools 17.14.41) |
| RawPy/LibRaw binding | Có: rawpy 0.27.1; 2 NEF + 2 CR2 upstream và 1 ARW + 1 CR3 + 1 NRW public fixture decode/render pass |
| libheif/Pillow HEIF plugin | Có: pillow-heif 1.7.0; HEIC 8-bit, synthetic HEIF 10-bit và Canon PowerShot V1 HIF 10-bit probe/decode/render pass |
| ONNX Runtime/WinML | Có onnxruntime 1.30.0; AzureExecutionProvider/CPUExecutionProvider; MODNet Photographic ONNX 25.97 MB load/inference pass; quality/background-matrix chưa nghiệm thu |
| Lensfun database | Có lensfunpy 1.18.0; requested matrix coverage recorded in `fixtures/lensfun-coverage.json` |

## Cổng P0

P0-01 (recipe/catalog/smoke path) có prototype; WPF desktop shell, C# recipe contract và native worker contract đã build/test trên Windows. P0-02 hiện đã decode/render được 4 NEF, 2 CR2, 2 ARW, 2 CR3 và 1 NRW fixture public/upstream; P0-03 đã có float RGB preview/export, EXIF normalization, HEIC 8-bit + synthetic HEIF 10-bit codec path, một Canon PowerShot V1 HIF camera path, RAW decoder path và display-RGB ΔE baseline; P0-04 có resolver metadata RAW thật và Lensfun exact coverage 9/13 requested variants; P0-05 đã load/inference được MODNet Photographic ONNX offline và WPF đã có raster-mask panel/preview; P0-06 có benchmark synthetic và bốn camera fixture; P0-07 đã chốt nền tảng/đường đi pixel trong ADR-0001. P0 technical prototype complete theo cổng đã duyệt. HEIF/HEIC camera matrix đầy đủ, matrix model đầy đủ, 4 lens variants và quality acceptance của AI còn là điều kiện P3–P7; không phải lý do gọi candidate hiện tại là stable.

## Giới hạn kiểm thử UI

Executable startup đã được kiểm tra bằng process smoke trên Windows. Connector Computer Use của phiên này không expose native app tree, nên chưa có click-through tự động cho dialog chọn folder và các nút UI; cần chạy lại trên máy/phiên có native UI bridge ở cổng Windows UI.

## Bước tiếp theo

1. Thu thêm file RAW/HEIC thực từ máy người dùng kèm model/firmware/kiểu nén; Canon PowerShot V1 HIF đã là fixture camera thật đầu tiên, nhưng chưa đủ ma trận Sony/Canon/Nikon. Chạy rawpy/libheif và hoàn thiện license review cho bộ fixture public hiện có.
2. Đánh giá chất lượng MODNet trên bộ ảnh người dùng, bổ sung ảnh nền/subject đại diện và quyết định có cần model general-scene khác; giữ hash/license/provider trong manifest.
3. Chuyển recipe contract từ smoke sang worker C++/IPC và chuẩn bị migration SQLite theo ADR-0001.
4. Đo trên Windows mục tiêu rồi cập nhật memory bank và handoff.
## P0 export and startup update

- JPEG/PNG export now supports optional width/height with aspect-ratio preservation and text watermark opacity/position; settings persist in recipe-v1.
- The WPF shell writes unhandled exception details to `%LOCALAPPDATA%\\MisaImageEditor\\startup-error.log` and reports the path for dispatcher failures.
- After the update: .NET Release build 0 warnings/0 errors, domain smoke passed, native CTest 1/1 passed, and both publish-folder and extracted-ZIP startup smoke passed.
- Candidate ZIP SHA-256 is recorded in `artifacts/P0-CANDIDATE-SHA256.txt`.
- Copy Settings now lets the user choose Basic, Crop/Transform, Mask, and Watermark/resize groups before pasting to all selected photos; C# smoke verifies partial copy preserves unselected destination fields.
- Export selected now supports a chosen parent plus validated relative child folder, JPEG/PNG output, per-photo persisted recipes, collision-safe filenames, and explicit deferred RAW/HEIF skip counts.
- Crop geometry now supports five aspect choices and manual -45..45 degree rotation in recipe, preview, and batch export; automatic horizon straighten remains explicitly deferred.
- Tone Curve now supports four parametric regions in recipe, preview, batch export, and Copy Settings; point-curve and HSL Color Mixer remain deferred.
- Color Mixer now supports eight HSL ranges with Hue/Saturation/Luminance in recipe, preview, batch export, and Copy Settings; point sampling and range visualization remain deferred.
- Lens Correction now persists requested profile metadata and applies bounded bilinear manual distortion plus manual vignetting in the shared preview/export path; calibrated Lensfun coefficients, TCA/defringe, and native worker integration remain explicitly deferred.
- Manual brush mask now paints adjustable-size/hardness grayscale PNGs at original image dimensions and attaches them to the existing raster-mask recipe path; undo/pressure remain deferred.
- Automatic subject mask now has a WPF-to-Python process bridge for JPEG/PNG. The candidate ships the MODNet Photographic ONNX model and bridge script; the user supplies Python with NumPy, Pillow, and ONNX Runtime (or sets `MISA_PYTHON`). The generated grayscale mask is attached to the existing raster-mask recipe path. General-scene quality acceptance remains deferred.
- Preview bridge verification passed for Sony ARW, Canon CR3, Nikon NEF, and Canon HIF; generated JPEG previews are recorded in `fixtures/preview-bridge-verification.json` with normalized output orientation (`1`). This validates the process boundary and representative decode path, not production RAW color fidelity or all camera variants.
- JSON presets now save and reload the recipe snapshot plus selected field/group keys; applying a preset to multiple selected photos preserves fields omitted by the preset. Domain smoke covers the round trip and deletion.
- Release gate after this update: standalone bridge inference passed with AzureExecutionProvider + CPU fallback and produced an original-resolution 1067x1600 PNG; the same script/model passed from the extracted candidate ZIP.
- Manual Lens Correction smoke now verifies the shared bounded bilinear radial warp preserves BGRA alpha while changing the raster for a nonzero distortion amount.
- P0-i18n/appearance update 22/09/2026: `LocalizationService` adds a persisted Vietnamese/English WPF selector for Library, Editor, and P0 dialogs. `AppearanceService` adds Light, Dark, and Windows-system palettes; Light is the default for black text on a bright background, and the selected mode is persisted. Release build remained 0 warning/0 error; C# domain smoke and Python 22/22 passed. The first published build exposed a Window-title StaticResource XAML parse error; it was fixed before the published/ZIP smoke that stayed alive for five seconds with a clean (newline-only) startup log.
- Updated candidate ZIP SHA-256: `168A0C4F3854E07961CC6C1107B69B6B3344E188BDEFDCAD1EE9A90BB6C1F64C`.
- Lensfun coverage was recalculated from the installed database: 9/13 requested variants have exact entries; Sony 18-135 f/4G, 70-200 GM II, 16-35 GM II, and 100-400 GM II remain unverified.
- The WPF loader now normalizes EXIF orientation before its shared preview/export path; C# smoke verifies the 90-degree orientation raster mapping and the RAW/HEIF bridge verifies output orientation `1`.
- Crop now has an Auto Straighten button. It estimates a small correction from the strongest near-horizontal edges, persists it through the existing non-destructive crop recipe, and is covered by a synthetic 5.7-degree C# smoke case. Real-world horizon quality acceptance remains open.
- The reported generic CLR startup dialog was reproduced from `%LOCALAPPDATA%\MisaImageEditor\startup-error.log`: `ExportOptionsChanged` ran before XAML had initialized the watermark controls. Event handlers now wait for the initialized UI state. A clean-log startup smoke stayed alive for five seconds with zero new exception bytes.
- `tools/run_p0_release_gate.ps1` replays the dependency check, Python regression/catalog/RAW/HEIF/mask/lens/preview/color gates, clean .NET build/domain smoke and native CTest; it writes `fixtures/p0-release-gate.json` on success.
- Latest `p0-release-gate.json` passed all 13 steps on 21/09/2026, including a clean .NET build and Python 22/22 regression.
- P0-01 Debug/x64 verification: .NET Debug build completed with 0 warning/0 error; native C++ Debug build and CTest 1/1 passed when invoked from the normalized Visual Studio x64 developer environment.
