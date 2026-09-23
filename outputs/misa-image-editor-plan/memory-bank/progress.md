# Progress

P0 continuation update 2026-09-21: EXIF orientation normalization now runs in the shared WPF preview/export loader and has C# raster smoke coverage. The runtime now has rawpy/pillow-heif/onnxruntime/lensfunpy installed and `MISA_PYTHON` is set for the current user.
Release gate rerun: Auto Straighten now estimates a near-horizontal edge correction and passes synthetic C# smoke. The CLR startup error was traced to an Export event during XAML initialization; UI-ready guards fix it, and both publish/ZIP startup smokes produced a clean error log. Release build 0 warning/0 error; domain smoke passed; Python 21/21 passed; native CTest 1/1 passed; preview bridge passed for Sony ARW, Canon CR3, Nikon NEF and Canon HIF; the extracted candidate's RAW and MODNet bridges passed. Candidate SHA-256 `32671F8A24877EA04C65A1FD7C6F4CD4E1EB562670E9B49B5DE3FCB5081E4AC5`.

Vietnamese/English update (22/09/2026): added persisted in-app language selection through `LocalizationService`, localized the main Library/Editor layout and collection/copy/brush dialogs, and added bilingual active-document summaries. Build 0 warning/0 error, domain smoke and Python 22/22 passed. A title-resource parse error caught by the first publish smoke was fixed; the replacement publish and fresh-ZIP starts were alive after five seconds with only the log-clearing newline. A follow-up `AppearanceService` now supplies Light/Dark/System palettes, defaults to Light, applies the theme to dialogs, and persists the choice. The replacement publish and fresh-ZIP starts were alive after five seconds with only the log-clearing newline. Current candidate SHA-256 `168A0C4F3854E07961CC6C1107B69B6B3344E188BDEFDCAD1EE9A90BB6C1F64C`.
P0 technical decision update: Python 22/22 includes display-RGB ΔE76 baseline plus actual Sony ARW/Nikon NEF EXIF; Nikon Z6 + NIKKOR Z 24-70mm f/4 S resolves to exact Lensfun and the Sony A77 20mm F2.8 remains explicitly unmatched. Fresh Sony/Canon/Nikon/HIF benchmarks range from 10.79 to 13.02 MP/s with 1270.79–1335.57 MB peak working set. `handoff/P0-TECHNICAL-DECISION.md` records pass/conditional status for P0-01 through P0-07 and the evidence required before stable/P3 claims.
P0 release gate is repeatable via `tools/run_p0_release_gate.ps1`; latest run passed all 13 steps and wrote `fixtures/p0-release-gate.json`.
P0-01 Debug/x64 verification also passed: WPF Debug build had 0 warnings/errors and native C++ Debug CTest passed 1/1 from the VS x64 developer environment.
Cập nhật 17/09/2026.

| Hạng mục | Trạng thái | Bằng chứng |
|---|---|---|
| Phạm vi và ưu tiên | Documented | PLAN.md |
| Kiến trúc/roadmap | Proposed | ARCHITECTURE.md, ROADMAP.md |
| Nghiên cứu lens/dependency | Researched | LENS-AND-DEPENDENCIES.md, nguồn liên kết |
| Memory bank/handoff | Documented | Các file trong bộ kế hoạch |
| P0 | Complete (technical prototype) | 13-step repeatable release gate passes; Python 22/22, Debug/Release WPF, native Debug/Release CTest, representative Sony/Canon/Nikon RAW + HEIC/HEIF, display-RGB ΔE baseline, real RAW EXIF/Lensfun resolver, offline mask, benchmark and P0 technical decision handoff. Full camera/lens/AI-quality matrix remains P3–P7 work; candidate is not stable. |
| P1–P7 | Not started | Chưa có triển khai |
| Kiểm thử ứng dụng | P0 smoke passed | 20/20 Python tests incl. recipe-v1 raster-mask contract/invert, HEIC 8/10-bit probe + Canon HIF camera decode/render + 10 RAW decode/render + raster mask + MODNet mask inference, manifest hash verification, C# catalog smoke, runner exit 0, WPF Release build 0 warning/0 error, native ctest 1/1, synthetic/camera benchmark, executable startup smoke passed; CUA connector không expose native app tree để click-through UI |
| Stable release | None | Prototype không phải stable; chưa có tag, commit hay installer |

Hoàn tất tài liệu không đồng nghĩa hoàn tất phần mềm. Các kiểm tra cấu trúc/link của tài liệu không được ghi thành kiểm thử ứng dụng.
