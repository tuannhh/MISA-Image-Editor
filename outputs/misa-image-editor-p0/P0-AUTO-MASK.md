# P0 automatic subject mask

The WPF Mask panel now exposes an offline `Auto subject mask...` action for JPEG/PNG previews.

The action starts `tools/generate_subject_mask.py` as a child process and passes the checked-in
`models/modnet_photographic.onnx` model. The child process uses Pillow, NumPy, and ONNX Runtime,
then writes an original-resolution grayscale PNG into `%LOCALAPPDATA%\\MisaImageEditor\\masks`.
The resulting path is attached to the existing raster-mask recipe contract, so invert/background
and local exposure continue to use the same mask graph.

The desktop process stays independent of Python and ONNX Runtime. The current candidate therefore
needs a Python interpreter with `numpy`, `Pillow`, and `onnxruntime` available. Set `MISA_PYTHON`
to the full `python.exe` path when Python is not on `PATH`; the UI reports a provider failure when
the runtime is unavailable. The model is shipped with the candidate app.

This is an integration bridge, not a quality acceptance claim. MODNet quality still needs a general
scene/background evaluation set, and RAW/HEIF automatic masks remain deferred until those previews
are available in the WPF path. A future native/WinML worker can replace this process without changing
the recipe schema.
