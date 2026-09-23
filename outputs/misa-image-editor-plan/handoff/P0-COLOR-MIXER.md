# P0 Color Mixer handoff

Date: 2026-09-17. Status: candidate, not stable.

The WPF Editor and batch renderer now support eight HSL color ranges (red, orange, yellow, green, aqua, blue, purple, magenta), each with Hue, Saturation, and Luminance adjustments. Recipe-v1 stores these in `color_mixer.channels`, and Copy Settings can transfer the complete mixer group. Domain smoke covers serialization, catalog reopen, and partial recipe copy.

Point Color sampling, range visualization, and more advanced channel blending remain future scope.
