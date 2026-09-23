# P0 brush mask handoff

Date: 2026-09-17. Status: candidate, not stable.

`Paint mask with brush...` opens a WPF dialog for JPEG/PNG previews. Brush size and hardness are adjustable, strokes are saved as an original-resolution grayscale PNG in `%LOCALAPPDATA%\MisaImageEditor\masks`, and the path is attached to the selected asset recipe. Existing invert and local exposure controls continue to apply.

Offline automatic subject masks are now available through the separate MODNet process bridge; background complement, pressure input, stroke undo, and mask re-projection after copying to a different image remain deferred.
