# P0 technical decision handoff

Date: 2026-09-21
Status: **P0 technical prototype complete; candidate only, not a stable release**

## Evidence by P0 gate

| Gate | Result | Evidence |
|---|---|---|
| P0-01 Windows prototype | Pass | .NET 10 WPF Debug and Release builds, C# domain smoke, native Debug/Release CTest and clean-log publish/ZIP startup smoke. |
| P0-02 representative codecs | Pass | Public Sony ARW, Canon CR3, Nikon NEF/NRW fixtures decode through RawPy/LibRaw; deterministic HEIC 8-bit and HEIF 10-bit decode; a Canon PowerShot V1 HIF camera sample decodes. The user camera matrix remains a P3 acceptance input. |
| P0-03 float preview/export | Pass for display RGB baseline | `fixtures/color-baseline-verification.json`: PNG ΔE76 mean 0.0000; JPEG source 0.5040; HEIC source 0.6086; P0 JPEG export 0.5984. EXIF orientation is normalized. This is not a RAW camera-colour certification. |
| P0-04 lens resolve | Technical pass | ExifRead reads public Sony ARW and Nikon NEF metadata. Nikon Z6 + NIKKOR Z 24-70mm f/4 S resolves to an exact Lensfun profile; Sony A77 + 20mm F2.8 is explicitly unmatched. Lensfun resolves 9 of 13 requested profiles exactly; the four unresolved requested variants do not silently fall back. Real user lens EXIF remains required. |
| P0-05 offline mask spike | Technical pass | MODNet ONNX loads with Azure/CPU providers and produces a raster mask; WPF can attach, invert and locally expose it. Quality acceptance for people/background scenes is still open. |
| P0-06 latency/RAM | Pass as local baseline | Current Windows 10 Pro x64 run: Sony A77 ARW 12.94 MP/s / 1271.60 MB; Canon M50 CR3 13.02 MP/s / 1270.79 MB; Nikon Z6 NEF 11.35 MP/s / 1284.07 MB; Canon V1 HIF 10.79 MP/s / 1335.57 MB. No target-SLA claim. |
| P0-07 platform decision | Complete | Keep Windows WPF shell, versioned recipe/catalog, Python codec/model bridges as replaceable P0 adapters, and retain the native worker boundary for later IPC. |

## Decision

The P0 technical prototype is complete and the platform decision is accepted. The next work can start at P1/P2 without changing the platform. The current ZIP is a reviewable candidate, not a stable handoff: it lacks the user camera/lens matrix, calibrated profiles for four requested lenses, AI quality acceptance, native IPC and an installer.

`outputs/misa-image-editor-p0/tools/run_p0_release_gate.ps1` is the repeatable P0 regression entry point; it records successful steps in `fixtures/p0-release-gate.json`.
The latest gate report passed all 13 steps on 2026-09-21.

## Required evidence before P3 or a stable release claim

1. Sample RAW/HEIC files from the actual Sony, Canon and Nikon models, with firmware and compression mode.
2. Lens chart/calibration source for the four unresolved requested variants, or an explicitly approved replacement profile.
3. A subject/background image set and accepted quality/time limits for the automatic mask.
4. Target-machine measurements and installer/upgrade/rollback tests.
