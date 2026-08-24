# Changelog

## 1.3.0 - 2026-08-24

- Replaced the former Simple / Advanced mode switch with a single guided workflow named **Simple Build**.
- Removed Advanced Mode from the user interface while preserving existing setup loading and the generation features used by Simple Build.
- Redesigned Hair & Controls as a master-detail workspace: a dedicated hairstyle list on the left and the selected hairstyle editor on the right.
- Kept the two-column Hair & Controls layout at the minimum supported window size.
- Added independent scrolling for the hairstyle list and selected hairstyle editor so the page header and Back/Next navigation remain stable.
- Added drag-and-drop creation directly inside the hairstyle list, including multi-object drops from the Hierarchy, and removed the separate large drop zone.
- Added drag reordering, persistent selection, and predictable selection behavior after adding, moving, or removing hairstyles.
- Renamed **Add Controls** to **Add BlendShape Controls** in Thai, English, and Japanese to clarify that Toggle and Radial create BlendShape controls.
- Moved the Material Preset action and explanation into the dedicated Material Presets section.
- Integrated Conflict Scanner and conflict details into Preview & Generate so conflict review no longer depends on Advanced Mode.
- Preserved compatibility with existing `1.2.x` avatar setup data and generated assets.

## 1.2.1 - 2026-08-24

- Added complete Japanese localization for the Editor, Menu Preview, and Scene View Capture workflow.
- Added and revised the Japanese Simple Mode guide using terminology aligned with the Unity Japanese manual.
- Added Google Search Console site-verification metadata to all landing-page languages.
- Added a complete Thai landing page with language navigation, localized metadata, and sitemap `hreflang` entries.
- Added a complete Thai project README and language links between the English and Thai versions.
- Added complete credit and third-party notices for the optional Gesture Manager UI asset integration, clarifying that its assets are loaded only from the user's installed package and are not bundled or included in generated avatar assets.
- Clarified that mehigo Hair Manager is a community-made automation tool that generates Animator Controllers and Layers, Animation Clips, Expression Menus, Parameters, Material Swap controls, and the required Modular Avatar component setup.
- Clarified the responsibility boundary: mehigo Hair Manager creates and configures the control system, while Modular Avatar performs the non-destructive integration at build time.
- Reframed the project description as a community-made automation tool built for Modular Avatar and added clear credit for Modular Avatar as the required core dependency and non-destructive integration layer.
- Added an explicit notice that this is not an official Modular Avatar project and is not affiliated with or endorsed by its maintainers.
- Renamed the misleading Hair Color wording across the editor, generated menus, website, and guides to Hair Material Presets / Hair Materials.
- Clarified that Material Presets switch existing Material assets assigned to Renderer slots; they do not create Material variants or edit shader colors.

## 1.2.0 - 2026-08-20

- Added a new Simple Mode as the default editor experience while preserving the complete Advanced Mode workflow.
- Added a persistent Simple / Advanced mode switch using Unity Editor preferences.
- Added a guided three-step Simple workflow for selecting an avatar, adding hair and controls, previewing, and generating.
- Split Simple Mode into separate Avatar, Hair & Controls, and Preview & Generate pages with Back/Next navigation.
- Simple Hair cards now expand one hairstyle at a time, and validation can return directly to the affected card.
- Added multi-object Hair creation from the current Hierarchy selection.
- Added drag-and-drop Hair Object creation.
- Added automatic Avatar Descriptor, activation target, existing animator preservation, and default material detection for newly added Hair Objects.
- Added Quick Toggle and Quick Radial controls with a combined Renderer / BlendShape picker.
- Added automatic control naming, Saved defaults, and 100 maximum values for Quick controls.
- Added one-click Hair Material Preset creation that snapshots the default materials automatically.
- Clarified that Material Presets switch existing Material assets rather than creating or editing colors, and that the first preset also creates a Default button from the hairstyle's current materials.
- Added simplified Hair cards with reordering, optional icons, activation summaries, control summaries, and material editing.
- Added an escape hatch for manual activation detection and Linked Object settings without exposing those fields during the normal Simple workflow.
- Added Simple Mode Menu Preview and Generate actions with validation and conflict-review handoff.
- Added bundled default icons for the root Hair menu, hairstyles, Toggle BlendShapes, Radial BlendShapes, and Material Preset controls.
- Restored the full Default / Custom Texture / Scene Capture hairstyle icon selector in Simple Mode.
- Menu Preview and generated VRChat menus now use the same bundled icon fallbacks.
- Every Hair Material Preset entry uses the bundled Material Preset icon in both Preview and generated submenus.
- Material Preset-only setups now open a hairstyle submenu correctly even when no BlendShape controls exist.

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
