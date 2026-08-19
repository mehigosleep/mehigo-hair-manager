# mehigo Hair Manager 1.1.0 User Guide

[ภาษาไทย](USER_GUIDE_TH.md) | [English](USER_GUIDE_EN.md) | [Project page](../README.md)

> For Unity 2022.3 and mehigo Hair Manager 1.1.0

mehigo Hair Manager builds hairstyle menus for VRChat avatars, including linked objects, BlendShapes, material presets, and icons. It uses Modular Avatar so the original FX Controller, Expression Parameters, and Expressions Menu are not edited directly.

## What's new in version 1.1.0

- English is now the default editor language; use **ไทย / ENG** to switch.
- The window now has three tabs: **Avatar Info**, **Hair Styles**, and **Generate**.
- **Conflict Scanner** moved to the Generate page.
- Added a real-time radial Preview for hairstyles, BlendShapes, and material presets.
- The Performance tab and experimental mode are temporarily hidden; stable Standard Animator generation is always used.
- Generated files are separated into a stable `Avatar_<id>` folder for each avatar instance.
- Supported VRChat Avatars SDK range is `>=3.10.4 <3.11.0`.

## Requirements

- Unity 2022.3
- A VRChat Avatars project
- VRChat Avatars SDK `>=3.10.4 <3.11.0`
- Modular Avatar `>=1.14.0 <2.0.0-a`

Optional: Avatar Optimizer (AAO) for build-time optimization and Gesture Manager for familiar Preview visuals. mehigo uses its built-in fallback when Gesture Manager is unavailable.

> Version 1.1.0 was tested with VRChat SDK 3.10.4. Retest the avatar after changing SDK or package versions.

## 1. Installation

### Install through VCC (recommended)

1. Add the Modular Avatar repository: `https://vpm.nadena.dev/vpm.json`
2. Add the mehigo repository: `https://mehigosleep.github.io/mehigo-hair-manager/vpm.json`
3. Open the project's **Manage Project** page and install Modular Avatar and mehigo Hair Manager.
4. Open Unity and wait for compilation to finish.

### Manual installation

1. Install the VRChat Avatars SDK and Modular Avatar.
2. Copy `Editor/MehigoHairManager.cs` below the project's `Assets` folder, preferably inside an `Editor` folder.
3. Do not keep an older mehigo Hair Generator script that declares the same classes.

## 2. Prepare the avatar

1. Back up or commit the project.
2. Place the avatar and every hairstyle in the Scene.
3. Confirm that the avatar has a **VRC Avatar Descriptor** and position each hairstyle correctly.
4. Identify accessories that should change with a hairstyle, such as ears, ribbons, or wrapper objects.

![Avatar and hairstyle objects in the Scene](images/01-project-avatar.png)

## 3. Open Hair Manager

Choose **Tools > mehigo > Hair Manager**.

![Opening Hair Manager from the Tools menu](images/02-open-hair-manager.png)

Use **ไทย / ENG** at the upper right to switch languages. English is the default in 1.1.0.

## 4. Configure Avatar Info

1. Assign the avatar GameObject or prefab to **Prefab / Avatar**.
2. Confirm the detected **Avatar Descriptor**.
3. Set **Root Menu Name**, for example `Hair Style`.
4. Enable **Save Selected Hair** if the selected hairstyle should persist.
5. For an existing setup, click **Load Existing Setup** before editing.

![Avatar Info settings](images/03-avatar-info.png)

**Advanced Settings** contains the Save Folder. It is a shared base folder; 1.1.0 automatically creates a separate `Avatar_<id>` folder for each avatar instance, including copied instances of the same prefab.

## 5. Add hairstyles

Open **Hair Styles**, then either:

- Select a hair object in the Hierarchy and click **Add Selected**.
- Click **+ Add Hair** and assign the object to **Hair Object**.

![Hair Styles before adding a hairstyle](images/04-add-hair.png)

Configure each entry:

- **Menu Button Name**: label shown in the VRChat menu
- **Button Icon**: the button image; Default uses VRChat's standard appearance
- **Hair Object**: root of this hairstyle
- Arrow buttons: reorder the menu
- `X`: remove the entry from the setup without deleting the source GameObject

![A configured hairstyle entry](images/05-hair-settings.png)

### Compatibility and activation detection

Click **Auto Detect** or **Re-Detect** so mehigo can inspect the Hair Root, parent objects, renderers, and animators.

- **Preserve Existing Animator** keeps the hair's existing animator behavior.
- **Auto Detect Activation** selects a suitable activation method.
- **Control Hair Root** is appropriate when disabling Hair Object hides the complete hairstyle.
- **Existing Wrapper** is appropriate when a parent/wrapper contains the whole set and pieces remain after disabling the root.

Review the recommendation below Detected Mode and test the relevant GameObjects in the Hierarchy before generating.

## 6. Linked Objects

Use Linked Objects for items that must follow a hairstyle but are outside its Hair Root, such as animal ears, bows, or ornaments.

1. Expand **Linked Objects**.
2. Click `+`.
3. Assign the required GameObject.

![Adding cat ears as a Linked Object](images/06-linked-objects.png)

The Conflict Scanner includes these properties. Avoid assigning one object to entries that require contradictory states.

## 7. BlendShape buttons

1. Expand **BlendShape Buttons** and click **+ Add**.
2. Set **Button Name**.
3. Select **Control Type**.
4. Assign the Skinned Mesh Renderer and BlendShape.

- **Toggle** switches between 0 and **ON Value**.
- **Radial Puppet** creates a Float parameter from 0–1 and maps it up to **Radial Max Value**.
- **Saved** persists the control value when the avatar reloads.

![Toggle and Radial Puppet examples](images/07-blendshape-controls.png)

Version 1.1.0 always generates the stable Standard Animator layout. Experimental Direct BlendTree generation is hidden.

## 8. Hair Color / Material Presets

1. Click **Scan Materials**.
2. Click **Create Default Material Preset** to snapshot every material slot below the Hair Root.
3. Click **+ Add Material Preset**.
4. Set its name/icon and replace only the material slots that should differ.

![Starting a Material Preset setup](images/08-material-preset-start.png)

![Default and Color 1 Material Presets](images/09-material-presets.png)

Do not reorder renderers or material slots after setup. If the hair hierarchy changes, scan and review the presets again.

## 9. Add more hairstyles

Repeat Hair Object, compatibility, Linked Object, BlendShape, and material setup for every style. The status row shows current Hair and BlendShape totals.

![A setup containing two hairstyles](images/10-multiple-hairstyles.png)

## 10. Inspect the Real-Time Menu Preview

On **Hair Styles**, click **Open Real-Time Menu Preview**. It mirrors the current menu structure without creating or modifying assets.

![Root Preview with two hairstyles](images/11-menu-preview-root.png)

Open a hairstyle to inspect material and BlendShape submenus. Badges distinguish Toggle, Radial, and submenu items; use **Back** to return.

![Hairstyle control submenu Preview](images/12-menu-preview-controls.png)

Preview interactions are visual only and do not change the avatar. The Preview opens only from the Hair Styles page.

## 11. Scan for conflicts and generate

Open **Generate** and verify the selected Avatar and Hair count in **Preflight**.

![Generate page before a conflict scan](images/13-generate-preflight.png)

1. Click **Scan Animator / MA Conflicts** after changing Hair, Wrapper, Linked Objects, or BlendShapes.
2. If conflicts are reported, inspect Animator Controllers or Modular Avatar Merge Animators that target the same properties mehigo will animate.
3. When the scan passes—or after you understand all warnings—click **Generate / Update mehigo Setup**.
4. Click **Save Config** to save the current editable configuration when needed.

![Passed Conflict Scanner and ready to generate](images/14-conflict-scan-passed.png)

## 12. Generated content

mehigo creates the Animator Controller, Animation Clips, Expression Menu, Parameters, icons, configuration, and required Modular Avatar components without directly overwriting the original avatar controller/menu/parameters.

- Save Folder is the shared base folder.
- Runtime assets are stored in an `Avatar_<id>` folder for each avatar instance.
- Generating the same avatar again updates that avatar's files.
- A prefab asset opened directly uses its asset GUID as identity.
- Older files stored directly in the base folder are not removed automatically.
- A legacy `mehigo_HairSelector_v4.controller` is migrated to `mehigo_HairSelector.controller` while preserving its GUID and references.

Edit the setup through Hair Manager because a later Generate/Update may overwrite generated assets edited by hand.

## 13. Edit an existing setup

1. Select the original avatar in Avatar Info.
2. Click **Load Existing Setup**, or select the generated **mehigo Hair Selector** component.
3. Edit the entries and inspect Preview again.
4. Scan for conflicts, then Generate / Update.

## 14. Pre-upload checklist

- Test every hairstyle button and Back navigation.
- Confirm that only the intended hairstyle is active.
- Test every Linked Object, BlendShape Toggle/Radial, and material preset.
- Reload the avatar and verify Saved controls.
- Confirm that existing FX/MA animations do not fight for the same properties.
- Use VRChat SDK Build & Test before the final upload.

## Troubleshooting

### Avatar Descriptor is not detected

Select the root GameObject containing VRC Avatar Descriptor, or assign the descriptor in Avatar Info.

### Pieces remain after a hairstyle is disabled

Run Re-Detect and use the parent containing the complete set as Existing Wrapper. Also review Linked Objects.

### Another animator controls the BlendShape or object

Run Conflict Scanner and resolve Controllers/Merge Animators targeting the same path and property before generating.

### Preview does not use Gesture Manager visuals

Gesture Manager is optional. The built-in fallback Preview still works and does not change generated output.

### Two avatars appear to overwrite generated assets

Version 1.1.0 separates stable `Avatar_<id>` folders. Confirm that each avatar instance retains its identity and that generated files were not moved manually.

### Uninstalling

Back up first. Remove confirmed mehigo setup objects/components and the unused avatar's `Avatar_<id>` assets, then remove the package through VCC. Do not delete the entire shared base folder if other avatars use it.
