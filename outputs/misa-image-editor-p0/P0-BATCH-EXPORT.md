# P0 batch export update

The WPF Editor now has `Export selected...`. It asks for a parent folder, accepts an optional relative child subfolder, exports every selected previewable JPEG/PNG using that photo's persisted recipe, and skips deferred RAW/HEIF items with a count. JPEG/PNG output can be selected; output names use the source stem plus `-edited`, with collision-safe suffixes.

The same batch path applies Basic adjustments, raster mask local exposure/invert, resize, and watermark position/opacity. Relative subfolder validation rejects rooted paths, `..`, invalid path segments, and paths that escape the selected parent.

RAW/HEIF batch export remains deferred until native decode/IPC integration is available.
