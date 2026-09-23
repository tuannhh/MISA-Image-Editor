# P0 brush mask update

The WPF Editor now has a Paint Mask dialog for previewable JPEG/PNG images. The dialog paints a grayscale mask with adjustable brush size and hardness, supports clearing the current strokes, and saves an immutable PNG under `%LOCALAPPDATA%\MisaImageEditor\masks`. The saved path is attached to the selected asset's recipe and uses the existing local exposure/invert pipeline.

The brush mask is created at the original image dimensions and is evaluated before crop/rotation, preserving the P0 coordinate convention. Brush undo history, pressure sensitivity, automatic subject/background detection, and mask refinement after cross-image paste remain later scope.
