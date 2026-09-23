# P0 crop handoff

Date: 2026-09-17. Status: candidate, not stable.

The Editor now has aspect-ratio crop choices (`original`, `1:1`, `4:3`, `3:2`, `16:9`) and manual rotation from -45 to 45 degrees. The recipe stores normalized crop bounds plus `angle` and `aspect`. Preview and batch JPEG/PNG export use the same geometry path, and Copy Settings can transfer the crop group.

Automatic horizon straighten is still deferred. It requires a tested horizon detector and acceptance set before being enabled.
