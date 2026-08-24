# Simple Build Guide — mehigo Hair Manager 1.3.0

[ภาษาไทย](SIMPLE_MODE_GUIDE_TH.md) · [日本語](SIMPLE_MODE_GUIDE_JA.md) · [Project Page](../README.md)

> This guide covers **Simple Build** in version 1.3.0: creating a hairstyle menu, adding BlendShape controls and Hair Material Presets, checking Menu Preview, and generating the setup.

> **mehigo Hair Manager is a community-made automation tool built for [Modular Avatar](https://modular-avatar.nadena.dev/).** It generates Animator Controllers and Layers, Animation Clips, Expression Menus, Parameters, Material Swap controls, and the required Modular Avatar components. Modular Avatar is the required core dependency and performs the non-destructive integration at build time. This is not an official Modular Avatar project and is not affiliated with or endorsed by its maintainers.

## Requirements

- Unity 2022.3 with a VRChat Avatars project
- Recommended VRChat Avatars SDK: `3.10.4`
- Recommended Modular Avatar: `1.14.0`
- An avatar with a **VRC Avatar Descriptor**
- Hair Objects placed under the selected avatar in the Hierarchy

[Gesture Manager](https://github.com/BlackStartx/VRC-Gesture-Manager) is optional. When installed, Menu Preview can load its familiar UI assets from the user's installed package. Gesture Manager is not bundled with mehigo Hair Manager and does not affect generated avatar assets. Gesture Manager UI assets © 2019–2023 BlackStartx — [MIT License](https://github.com/BlackStartx/VRC-Gesture-Manager/blob/master/LICENSE.md).

## Workflow overview

Simple Build separates the workflow into three pages:

1. **Avatar** — select the avatar
2. **Hair & Controls** — add hairstyles, BlendShape controls, and Material Presets
3. **Preview & Generate** — inspect the menu and generate the setup

The tool automatically detects the Avatar Descriptor, Hair Root or wrapper, renderers, BlendShapes, materials, and output folder. It creates the Parameters, Animator Layers, and generated files, so you do not need to configure them manually.

## 1. Open Hair Manager

In Unity, select **Tools > mehigo > Hair Manager**.

![Open Hair Manager from the Tools menu](images/simple-build-v1.3.0/01-open-hair-manager.png)

Hair Manager opens directly in **Simple Build**. You can switch between **ไทย / ENG / 日本語** at any time without losing your settings.

## 2. Avatar page

Before an avatar is selected, the **Avatar** field is empty and **Next** is disabled.

![Avatar page before selecting an avatar](images/simple-build-v1.3.0/02-select-avatar-empty.png)

Drag the avatar root from the Hierarchy into the **Avatar** field, or use the object picker on the right. The tool finds its VRC Avatar Descriptor and prepares its output folder automatically.

When the Descriptor is found, the page displays **Ready** and enables **Next**.

![Avatar selected and ready](images/simple-build-v1.3.0/03-select-avatar-ready.png)

### Menu Options

- **Menu Name** — the name displayed for the root menu in VRChat. The default is `Hair Style`.
- **Remember Selected Hair** — saves the hairstyle selected by the avatar user.

### Load Existing Setup for This Avatar

Use this button to edit a setup previously generated for the selected avatar. The saved configuration is loaded so the hairstyle list does not need to be rebuilt.

> If no VRC Avatar Descriptor is found, select the avatar root GameObject instead of one of its children.

## 3. Hair & Controls page

The page provides three ways to add hairstyles. Its left side is the hairstyle list; its right side is the editor for the selected hairstyle. Each side scrolls independently.

![Hair and Controls page before adding hair](images/simple-build-v1.3.0/04-add-hair-empty.png)

### Add hairstyles

- Select one or more Hair Objects in the Hierarchy and press **+ Add Selected Hair**.
- Drag one or more Hair Objects directly into the **Hairstyles** list.
- Press **+ Empty Hair** and assign its Hair Object later.

Each Hair Object must be under the selected avatar. The tool names the button from the object, scans its materials, and detects how the hairstyle should be enabled or disabled.

## 4. Configure the selected hairstyle

Click a row in the **Hairstyles** list to edit it on the right. The selected hairstyle editor contains:

- **Button Name** — the hairstyle button label in the menu
- **Hair Object** — the root object of the hairstyle
- **Hairstyle Icon** — the hairstyle button icon
- The detected visibility behavior
- **Add BlendShape Controls** buttons for Toggle and Radial controls
- A separate **Material Presets** section with its add button and explanation

![Selected hairstyle editor after adding a hairstyle](images/simple-build-v1.3.0/05-selected-hairstyle-editor.png)

Removing a hairstyle does not delete the original GameObject from the Hierarchy.

## 5. Choose a hairstyle icon

**Hairstyle Icon** supports three modes.

![The three hairstyle icon modes](images/simple-build-v1.3.0/06-hairstyle-icon-modes.png)

### Default

Uses the hairstyle icon bundled with mehigo Hair Manager.

### Custom Texture

Select a Texture2D from the Project. A square image is recommended; enable alpha transparency when a transparent background is required.

### Capture From Scene

Create a 256 × 256 icon from the current Scene View camera:

1. Frame the avatar in Scene View.
2. Select **Capture From Scene**.
3. Press **Preview / Capture**.
4. Press **Refresh Preview** after changing the camera angle.
5. Press **Capture & Use** to save and apply the image.

![Scene Capture window and captured hairstyle icon](images/simple-build-v1.3.0/07-scene-capture.png)

Captured icons are saved inside the selected avatar's output folder, so another avatar does not overwrite them.

## 6. Add Toggle and Radial controls

### Toggle

Press **+ Toggle**, then choose a BlendShape. Renderers and BlendShapes under the Hair Object are collected automatically.

Toggle is suitable for two-state actions such as:

- Showing or hiding cat ears
- Showing or hiding a ribbon
- Switching instantly between short and long hair

### Radial

Press **+ Radial** and choose a BlendShape from the same list. Radial controls are suitable for continuous values from 0 to 100.

![Choose a BlendShape for a new control](images/simple-build-v1.3.0/08-blendshape-picker.png)

After adding a control, its button name can be edited. Each entry displays its **Toggle** or **Radial** type together with the source renderer and BlendShape.

![Added Toggle and Radial controls](images/simple-build-v1.3.0/09-toggle-radial-controls.png)

The `X` button removes only that control from the setup.

## 7. Add Hair Material Presets

Press **+ Material Preset** to add a button that switches the hairstyle to another set of existing Material assets.

> Material Presets do not create a Material or change shader color values. They replace the Material assigned to each selected Renderer/Slot. The tool automatically saves the currently assigned materials as a separate **Default** button.

Under **Material Presets**:

1. Rename `Material 1` to a useful menu label such as `Pink`, `White`, or `Black`.
2. Replace the material only in the renderer slots that should change.
3. When the hairstyle uses multiple material slots, every detected slot is listed.
4. Press `X` to remove that Material Preset.

![Add a Material Preset and assign existing materials](images/simple-build-v1.3.0/10-material-preset.png)

The Default materials are not shown as editable fields because they are captured automatically. **Default** appears as a separate button in the Hair Materials submenu in Preview and in the generated menu.

## 8. Use multiple hairstyles

You can add multiple hairstyles. Click a row to edit it and drag rows to define their menu order. Scrolling the list does not move the selected hairstyle editor, and scrolling the editor does not move the whole page.

![Multiple hairstyles and drag-to-reorder list](images/simple-build-v1.3.0/11-multiple-hairstyles.png)

Every hairstyle has its own icon, Toggle controls, Radial controls, and Material Presets. The selected hairstyle remains visible in the editor on the right.

### Detection Fixes / More Options

This section is normally left closed. Open it only when the detected wrapper or visibility behavior does not match the hairstyle package. It can be used to:

- Control the Hair Object directly
- Control an existing wrapper
- Leave visibility to another system
- Add linked objects such as accessories or ears that should activate with the hairstyle

## 9. Preview & Generate page

When the configuration is complete, press **Next** to open page 3. The page summarizes the number of hairstyles, controls, and Material Presets.

![Preview and Generate summary](images/simple-build-v1.3.0/13-preview-generate.png)

If required information is missing, Generate is disabled and a message identifies the problem. The fix button returns to and selects the affected hairstyle.

## 10. Inspect Menu Preview

Press **Open Menu Preview** to inspect the current menu without creating or modifying assets.

### Root menu

The first preview level shows all hairstyles in list order.

![Hair Style root Menu Preview](images/simple-build-v1.3.0/14-menu-preview-root.png)

Custom captured icons appear immediately in the preview.

![Hair Style root Menu Preview with captured icons](images/simple-build-v1.3.0/14-menu-preview-root-custom-icons.png)

### Hairstyle submenu

Selecting a hairstyle displays its Use Hair button, Toggle controls, Radial controls, and Hair Materials submenu.

![Controls inside the first hairstyle](images/simple-build-v1.3.0/15-menu-preview-hair-one.png)

![Controls inside the second hairstyle](images/simple-build-v1.3.0/16-menu-preview-hair-two.png)

- Click a Toggle to simulate its state.
- Click a Radial control to open its preview slider.
- Preview interactions do not modify the avatar in the Scene.
- A page control is added automatically when the menu exceeds one page.

### Hair Materials submenu

The Hair Materials submenu contains **Default** and every added Material Preset. Selecting a button swaps the assigned Material assets; it does not generate a color. All presets use the same bundled Material Preset icon so they are visually recognized as one group.

![Hair Materials submenu with Default and added Material Presets](images/simple-build-v1.3.0/17-menu-preview-materials.png)

## 11. Generate or update the setup

After checking Preview, press **Generate / Update Setup**. mehigo Hair Manager:

1. Validates the configuration and scans for conflicts.
2. Creates or updates the Animator Controller and Animation Clips.
3. Creates the Expression Menu and Parameters.
4. Creates `mehigo Hair Selector` under the avatar.
5. Creates or configures the Modular Avatar Merge Animator, Parameters, and Menu Installer components used at build time.
6. Saves the configuration for later editing.

![Generated setup and Modular Avatar components](images/simple-build-v1.3.0/18-generated-setup.png)

Generated files are stored under an avatar-specific `Avatar_<id>` folder. Updating the same avatar updates only its files, while another avatar receives a separate folder and does not overwrite them. Modular Avatar remains responsible for the non-destructive merge during the avatar build.

If a conflict is detected, generation pauses and the **Conflict Scanner** on the same page shows the details before generation continues.

## 12. Edit an existing setup

1. Open Hair Manager.
2. Select the original avatar on the Avatar page.
3. Press **Load Existing Setup for This Avatar**.
4. Select a hairstyle and edit its settings, controls, or Material Presets.
5. Check Menu Preview.
6. Press **Generate / Update Setup** again.

Do not edit generated Animator, Animation, or Menu assets directly because a later update can overwrite those changes.

## Checklist before uploading the avatar

- Every hairstyle enables and disables correctly.
- Toggle and Radial controls affect only their intended hair BlendShapes.
- Default restores the original materials.
- Every Material Preset uses the correct Material assets and slots.
- Menu Preview contains every button in the correct order.
- Test the avatar in Play Mode or an avatar testing tool before Build/Upload.
