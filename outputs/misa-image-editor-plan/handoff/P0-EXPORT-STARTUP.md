# P0 export/startup handoff

Date: 2026-09-17. Status: candidate, not stable.

## Delivered

The WPF shell has JPEG/PNG export for an active JPEG/PNG preview. It supports a chosen output file, optional dimensions with aspect-ratio preservation, and a text watermark with opacity and position. Recipe-v1 now validates the watermark position and optional dimensions.

The shell now captures runtime diagnostics at `%LOCALAPPDATA%\MisaImageEditor\startup-error.log` and shows the path for dispatcher exceptions. This addresses the generic CLR dialog reported during launch and makes any remaining machine-specific exception actionable.

## Verification

- .NET Release build: 0 warnings, 0 errors.
- Domain catalog/recipe smoke: passed.
- Native CTest: `image_worker_smoke`, 1/1 passed.
- Published folder startup: process alive for 3 seconds.
- Extracted ZIP startup: process alive for 3 seconds.
- Candidate ZIP SHA-256: see `outputs/misa-image-editor-p0/artifacts/P0-CANDIDATE-SHA256.txt`.

## Next owner

Run the candidate on the target Windows machine, reproduce the original action, and attach `%LOCALAPPDATA%\MisaImageEditor\startup-error.log` if the dialog returns. Continue with native RAW/HEIF preview and IPC only after this candidate passes the target-machine smoke.
