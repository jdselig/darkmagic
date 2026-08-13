# Changelog

All notable DarkMagic package changes are recorded here. Versions follow semantic versioning.

## [3.11.2] - 2026-08-13

### Fixed

- DarkMagic now detects TMP font materials that resolve to Unity's internal error shader instead of allowing magenta text blocks.
- The font smoke test now verifies real working materials and shaders, not only glyph geometry.

### Added

- A beginner-friendly **Tools → DarkMagic → Setup UI** command and first-install prompt for importing Unity's official TMP Essential Resources.
- A pre-build check that explains how to fix a missing or broken U font shader.
- Runtime guidance that fails clearly instead of silently rendering broken UI.

### Changed

- DarkMagic continues to ship its own dynamic font assets, while their materials use Unity's maintained TMP shaders.
- A working `UConfig.FontAsset` or legacy `UConfig.Font` remains an optional advanced path and skips the setup prompt.
- The bundled Liberation material now stores the scale ratios expected by Unity's current TMP shader instead of becoming dirty on first validation.

## [3.11.1] - 2026-08-13

### Changed

- Unity support now targets Unity 6.5 and newer.
- The Config sample compiles in its own assembly to exercise real consumer namespace boundaries.
- `UConfigUser` discovery now works outside `Assembly-CSharp` and in non-development players.
- `I.GetButton`, `I.GetAxis`, and `I.GetAxisRaw` now honor their documented custom mapping dictionaries on every input backend.
- Stale U config examples now use the current `BodyFontSize` setting.
- Dynamic Liberation font tests no longer persist generated kerning data into the package asset.

### Compatibility

- Verified with imported V, S, W, I, U, and typed Stats config in Unity 6000.5.8f1.
- Verified in standalone macOS Mono and IL2CPP players.

## [3.11.0] - 2026-08-13

### Added

- EditMode tests for Stats, StatBlock, V owner lifetime, state registries, and Flow entries.
- PlayMode smoke tests for zero-config TMP rendering and floating outcomes.
- One-command Unity validation script under `Scripts~/validate.sh`.
- Release metadata/tag consistency check under `Scripts~/check-release.sh`.
- Student-facing module, Stats, battle, and testing guides under `Documentation~/`.
- `Stat.Buff`, `Stat.Debuff`, `Stat.ModifyEquipment`, and `StatBlock.AddEquipment`.
- Minimal package TMP settings fallback for projects that have not imported TMP Essential Resources.
- Linker preservation for the TMP fallback used by IL2CPP builds.
- Explicit uGUI/TextMeshPro package dependency for truly blank Unity projects.
- MIT license and tag-triggered GitHub Release publishing.

### Changed

- Normal stat `Current` now always uses Base plus temporary and equipment modifiers.
- `StatBlock.SetBase` now uses normal Stat behavior and raises `OnBaseChanged`.
- Permanent base changes no longer overwrite the configured `LevelUp` delta.
- StatBlock indexing is rebuilt when its serialized list count changes.
- Duplicate insertion of the same Stat instance is ignored.
- `U.Flow` explicitly records entry kind, including select entries with null reference payloads.
- `U.TargetMode.All` now returns filtered targets without requiring a camera or creating UI.
- Target rules no longer retain temporary `AllowAll` mutations, and sprite markers keep their configured color.
- Floating outcome internals moved to focused `U.Outcome.cs` without changing the public API.
- README shortened into a quick-start map with focused documentation links.
- Package description and sample descriptions now include the current modules.
- Dynamic Liberation font asset source reference corrected.
- Samples now compile together without obsolete Unity object-search APIs or duplicate U config types.

### Compatibility

- Verified with Unity 6000.5.8f1.
- Verified in standalone macOS Mono and IL2CPP players.
- Preserves Unity 6.3/6.4/6.5+ internal owner-key compatibility.

## [3.10.12]

- Fixed non-resource `Stat.Current` so temporary/equipment changes are visible.
- Corrected resource and compound-assignment examples.

## [3.10.11]

- Replaced Unity 6.5-incompatible internal `Object.GetInstanceID()` calls with runtime compatibility keys.

## [3.10.0]

- Added Stat, StatBlock, typed StatBlock support, Stats inspectors, resource behavior, thresholds, and LevelUp helpers through the 3.10.x patch series.

## [3.9.0]

- Added `U.Target` single/multi target selection and configurable target markers through the 3.9.x patch series.

## [3.8.9]

- Added `U.PopOutcome` floating combat text with anchor/collider positioning.

## [3.8.2]

- Expanded U with code-first UI helpers, styling, displays, menus, and transitions.
