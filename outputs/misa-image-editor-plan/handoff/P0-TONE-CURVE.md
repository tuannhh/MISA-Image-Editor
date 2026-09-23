# P0 Tone Curve handoff

Date: 2026-09-17. Status: candidate, not stable.

The WPF Editor and batch renderer now apply a four-region parametric Tone Curve (Shadows, Darks, Lights, Highlights). Recipe-v1 stores it under `tone_curve`; Copy Settings can transfer it independently. Domain smoke covers serialization and partial recipe copy.

The implementation is deliberately parametric. A point-curve editor, per-channel curves, and Color Mixer/HSL remain later milestones.
