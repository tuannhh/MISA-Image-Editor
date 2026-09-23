# P0 export and startup verification

Updated 17/09/2026.

The P0 desktop shell now includes JPEG/PNG export for the active JPEG/PNG preview. The export dialog supports a target folder/file, optional width and height (zero keeps the original dimension; one supplied dimension preserves the aspect ratio), and a text watermark with opacity and one of five positions: `top_left`, `top_right`, `center`, `bottom_left`, or `bottom_right`. These settings are persisted in the recipe as `watermark.position`, `watermark.width`, and `watermark.height`.

The Windows shell also records otherwise unhandled WPF/Task exceptions to:

`%LOCALAPPDATA%\MisaImageEditor\startup-error.log`

This prevents the generic CLR crash dialog from hiding the actionable exception when the application encounters a runtime problem. A dispatcher exception is shown with the log path and remains handled so the user can save the diagnostic information.

Verification completed after the change:

- .NET Release solution build: 0 warnings, 0 errors.
- C# catalog/recipe smoke: passed.
- Native CTest `image_worker_smoke`: 1/1 passed.
- Published candidate process smoke: process stayed alive for 3 seconds.
- Extracted ZIP candidate process smoke: process stayed alive for 3 seconds.

This is still a P0 candidate. RAW/HEIF preview and production IPC/native codec integration remain deferred.
