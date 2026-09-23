# P0 preset files

The desktop editor now saves a selected recipe snapshot as a MISA JSON preset and applies it to
the current multi-selection. The preset stores the recipe schema version, a human-readable name,
and the exact field/group keys selected in Copy Settings. Applying a preset uses the same
field-level `RecipeCopy` path as clipboard paste, so fields omitted from the preset remain unchanged
on each destination image.

Presets are saved by default under `%LOCALAPPDATA%\\MisaImageEditor\\presets`, while the Save and
Open dialogs also allow a user-selected JSON location. The stored recipe is immutable after save;
editing the source image later does not mutate the preset. The domain smoke covers save/load,
selected-field application, listing, and deletion.

Mask paths are intentionally stored as recipe references. When a preset is moved to another machine,
the referenced mask file must be available at the same path or be chosen again; automatic mask
re-projection is a later mask milestone.
