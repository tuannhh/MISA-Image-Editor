# P0 batch copy/paste handoff

Date: 2026-09-17. Status: candidate, not stable.

The Editor Copy Settings action now opens a field-group dialog. Basic, Crop/Transform, Mask/local adjustment, and Watermark/resize groups can be selected independently. Paste applies the selected groups to all selected destination photos. The domain smoke verifies partial application and preservation of an unselected destination field.

Remaining scope: preset rename/delete management, crop/transform editing controls beyond the current geometry controls, and native RAW/HEIF recipe application are not yet implemented. JSON preset save/apply is now available through the WPF shell and field-selective domain contract.
