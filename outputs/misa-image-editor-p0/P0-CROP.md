# P0 crop and geometry update

The WPF Editor now exposes a JPEG/PNG crop geometry control with `original`, `1:1`, `4:3`, `3:2`, and `16:9` aspect choices plus a manual rotation angle from -45 to 45 degrees. The selected geometry is stored in recipe-v1 as `crop.left/top/right/bottom`, `crop.aspect`, and `crop.angle`.

The same geometry is applied to the WPF preview and batch export. Local raster masks are evaluated on the source frame before the crop/rotation transform so the mask remains aligned with the original image in this P0 path. Copy Settings already includes the crop group.

Automatic horizon detection is intentionally gated until a tested horizon model and acceptance images are available; the UI labels that limitation instead of pretending the current manual angle is automatic straighten.
