# P0 lens correction handoff

Date: 2026-09-17. Status: candidate, not stable.

Recipe-v1 now carries `lens_correction.profile_id`, `profile_enabled`, `distortion`, and `vignetting`. The WPF panel lists the requested lens families and marks unverified variants. Bounded bilinear manual distortion and manual vignetting are applied by the shared preview and batch renderer; profile selection is metadata until the matching Lensfun/EXIF calibration is verified. TCA/defringe and native worker warp remain deferred.
