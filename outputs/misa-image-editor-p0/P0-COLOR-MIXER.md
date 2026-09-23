# P0 Color Mixer update

The Editor now includes a basic HSL Color Mixer with eight color ranges: red, orange, yellow, green, aqua, blue, purple, and magenta. Each range has Hue, Saturation, and Luminance controls from -100 to 100.

Color Mixer values are stored under `color_mixer.channels` in recipe-v1, applied to JPEG/PNG preview and batch export, and can be selected in Copy Settings. The implementation uses a deterministic RGB-to-HSL conversion and nearest color-range selection; point color sampling, range visualization, and advanced blending remain later work.
