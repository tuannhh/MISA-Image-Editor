# P0 batch copy/paste update

The WPF Editor now opens a Copy Settings dialog before placing a recipe on the internal clipboard. The user can independently select Basic adjustments, Crop/Transform, Mask/local adjustments, and Watermark/resize settings. Paste applies the selected groups to every currently selected destination photo and leaves other destination fields intact.

The domain smoke covers a mixed selection (Basic exposure, Mask, Watermark) and verifies that an unselected destination field (Basic contrast) survives the paste.

The dialog is intentionally local to the P0 process; a saved preset browser and cross-session preset files remain later work.
