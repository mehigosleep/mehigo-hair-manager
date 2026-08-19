# Changelog

## 1.1.0 - 2026-08-19

- Changed the default editor language from Thai to English.
- Temporarily hidden the Performance tab.
- Removed the Compatibility tab and moved the Conflict Scanner into the Generate page.
- Added a real-time radial Menu Preview window for Hair Styles, BlendShapes, and Material Presets.
- Added optional Gesture Manager UI asset integration using the installed package's Resources, with a built-in fallback.
- Reworked Menu Preview into Gesture Manager-style radial slices with hover states, Back navigation, type badges, and a center ring.
- Menu Preview now opens only from the Hair Styles page.
- Enforced the stable Standard Animator generation mode.
- Improved Thai localization while preserving Unity and VRChat terminology in English.
- Added stable avatar-instance-scoped output folders so generated Controllers, Animation Clips, Expression Menus, and Icons do not overwrite one another, including copied instances of the same Prefab.
- Updated the supported VRChat Avatars SDK range to `>=3.10.4 <3.11.0` after avatar testing.

## 1.0.1 - 2026-08-18

- Removed the internal v4 suffix from the generated Animator Controller filename.
- Existing `mehigo_HairSelector_v4.controller` assets are migrated to `mehigo_HairSelector.controller` while preserving their GUID and references.

## 1.0.0 - 2026-08-18

Initial public version of **mehigo Hair Manager**.

- Hairstyle selector generation through Modular Avatar
- Linked object support
- Toggle and Radial Puppet BlendShape controls
- Material presets
- Icon capture and preview
- Existing animator/wrapper compatibility controls
- Conflict scanner
- Performance analyzer
- Avatar Optimizer compatibility detection
- Stable Standard animator generation and AAO handoff mode
- Thai / English UI
