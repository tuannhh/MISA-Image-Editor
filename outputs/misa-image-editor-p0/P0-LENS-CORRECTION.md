# P0 lens correction update

The Editor now stores lens correction settings in recipe-v1 and exposes the requested lens families in the UI. The selected profile ID, profile-enabled flag, manual distortion value, and manual vignetting value survive catalog reopen, recipe copy, and batch export.

P0 applies bounded bilinear manual distortion and manual vignetting in the shared JPEG/PNG/bridge preview and batch pipeline. Profile calibration is reported explicitly and is not silently substituted when Lensfun coverage is missing; calibrated coefficients, TCA/defringe, and native worker integration remain later work.
