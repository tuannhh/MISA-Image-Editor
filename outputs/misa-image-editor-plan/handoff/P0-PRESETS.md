# Handoff: P0 JSON presets

The WPF shell can save a selected Copy Settings snapshot as a MISA JSON preset and apply it to the
current multi-selection. The preset stores schema version, name, recipe, and selected field/group
keys. Application uses the same `RecipeCopy` path as clipboard paste, preserving unselected fields.

The domain smoke verifies save/load, selected-field application, listing, and deletion. Presets are
stored by default under `%LOCALAPPDATA%\\MisaImageEditor\\presets`; the dialogs also support a chosen
JSON path. Mask paths remain references and are not re-projected when a preset moves between machines.

Remaining work for a later milestone is preset management UI (rename/delete/list), import conflict
handling, and mask re-projection on a different image geometry.
