# mehigo Hair Manager — Community-made Automation Tool for Modular Avatar

[English](README.md) | [ภาษาไทย](README_TH.md)

**Version 1.2.1**

## Install with VCC

[![Add Repository to VCC](https://img.shields.io/badge/Add_Repository_to_VCC-2f81f7?style=for-the-badge)](https://mehigosleep.github.io/mehigo-hair-manager/)

Repository URL:

`https://mehigosleep.github.io/mehigo-hair-manager/vpm.json`

## Automatically build VRChat hair controls with Modular Avatar

**mehigo Hair Manager** is a community-made Unity Editor automation tool built for [Modular Avatar](https://modular-avatar.nadena.dev/). It generates Animator Controllers and Layers, Animation Clips, Expression Menus, Parameters, Material Swap controls, and the Modular Avatar components required to integrate them with a VRChat avatar.

Simple Mode guides you through selecting an avatar, adding hairstyles, creating Toggle and Radial BlendShape controls, setting up Hair Material Presets, previewing the menu, and generating the complete setup. Material Presets switch existing Material assets in renderer slots; the tool does not create materials or edit their colors.

> **Material switching only:** Prepare each Material variant in Unity first. mehigo Hair Manager creates the VRChat menu and animations that swap those existing Materials and restore the original Default set.

Modular Avatar is the required core dependency and performs the non-destructive integration at build time. mehigo Hair Manager automates creation of the control assets and configures the integration components; it does not replace Modular Avatar. Each avatar receives its own output folder, so multiple avatars and copied instances can be managed without overwriting one another.

> **Community project:** This is not an official Modular Avatar project and is not affiliated with or endorsed by bd_ or the Modular Avatar project.

Advanced Mode remains available for custom activation behavior, linked objects, compatibility adjustments, conflict review, and detailed setup control.

[คู่มือ Simple Mode ภาษาไทย](https://github.com/mehigosleep/mehigo-hair-manager/blob/main/docs/SIMPLE_MODE_GUIDE_TH.md) · [Simple Mode Guide in English](https://github.com/mehigosleep/mehigo-hair-manager/blob/main/docs/SIMPLE_MODE_GUIDE_EN.md) · [Simple Modeガイド 日本語](https://github.com/mehigosleep/mehigo-hair-manager/blob/main/docs/SIMPLE_MODE_GUIDE_JA.md) · [คู่มือฉบับเต็ม](https://github.com/mehigosleep/mehigo-hair-manager/blob/main/docs/USER_GUIDE_TH.md) · [Complete English Guide](https://github.com/mehigosleep/mehigo-hair-manager/blob/main/docs/USER_GUIDE_EN.md) · [Changelog](https://github.com/mehigosleep/mehigo-hair-manager/blob/main/CHANGELOG.md)

## Overview

### Simple Mode and real-time Menu Preview

![mehigo Hair Manager Simple Mode with real-time Menu Preview](https://raw.githubusercontent.com/mehigosleep/mehigo-hair-manager/main/docs/images/overview-simple-mode-editor.png)

> The v1.2.0 screenshot uses the previous **Hair Color** label. This control is Material Preset switching: every button swaps existing Material assets.

## Features

- Simple Mode with separate Avatar → Hair & Controls → Preview & Generate pages
- Advanced Mode with the complete editor controls from earlier versions
- Add multiple Hair Objects from the Hierarchy selection or by drag and drop
- Quick Toggle / Radial controls without manually selecting a Renderer first
- One-click Hair Material Preset setup with automatic default material scanning
- Switch existing Material assets per renderer slot, with automatic restoration of the original Default materials
- Bundled default icons for Hair menus, hairstyles, BlendShape controls, and Material Presets
- Default, Project Texture, or Scene Capture hairstyle icons in Simple Mode
- Multiple hairstyle selector menu
- Modular Avatar Merge Animator / Parameters / Menu Installer workflow
- Linked objects per hairstyle
- BlendShape Toggle controls
- BlendShape Radial Puppet controls
- Hair Material Presets
- Custom icons and Scene View icon capture
- Real-time radial Menu Preview while editing Hair Styles, with automatic Gesture Manager UI asset integration when installed
- Existing hair animator / wrapper compatibility options
- Conflict Scanner integrated into the Generate page
- Avatar Optimizer (AAO) compatibility detection
- Thai / English editor UI
- Editable saved setup configuration
- Avatar-scoped generated output to prevent different avatars from overwriting each other's assets

## Animator optimization

Version 1.1.0 enforces the stable Standard Animator layout for all generated BlendShape controls. The Performance tab and experimental optimization mode are temporarily hidden.

Avatar Optimizer (AAO) can still optimize the generated Standard controller later in the avatar build pipeline.

The experimental mehigo Direct BlendTree optimization remains disabled because avatar testing found cross-influence issues with some BlendShape/Radial configurations.

## Requirements

- Unity project configured for VRChat Avatars
- VRChat Avatars SDK `>=3.10.4 <3.11.0`
- Modular Avatar `>=1.14.0 <2.0.0-a`

Optional:

- Avatar Optimizer (AAO)
- Gesture Manager (for its familiar radial-menu UI assets in Menu Preview; not bundled)

v1.1.0 was tested with VRChat SDK 3.10.4. Test your avatar after changing SDK or package versions.

## Install with VCC

1. Add the Modular Avatar repository to VCC if it is not already installed: `https://vpm.nadena.dev/vpm.json`.
2. Add the mehigo repository to VCC: `https://mehigosleep.github.io/mehigo-hair-manager/vpm.json`.
3. Open your avatar project in VCC and add **mehigo Hair Manager**.
4. Open Unity and use **Tools > mehigo > Hair Manager**.

## Manual install

1. Copy the package's complete `Editor` folder, including `MehigoHairManager.cs` and `Icons`, into your Unity project's `Assets` folder.
2. Make sure Modular Avatar is installed.
3. Open Unity.
4. Use **Tools > mehigo > Hair Manager**.

Do not install the tool through both VCC and the manual method in the same project. Also remove older mehigo Hair Generator scripts that contain the same internal class definitions; otherwise Unity may report duplicate-class compilation errors.

## Basic workflow

1. Open Hair Manager in the default **Simple** mode.
2. On **Avatar**, select the avatar and continue to **Hair & Controls**.
3. Add Hair Objects, then add Toggle, Radial, or **Material Preset** controls from each Hair card when needed.
4. Continue to **Preview & Generate**, inspect Menu Preview, and press **Generate / Update Setup**.
5. Switch to **Advanced** only when manual compatibility, parameter, folder, or detailed control settings are required.
6. Test the generated menu and animations before uploading the avatar.

For the illustrated v1.2.0 workflow, see the [คู่มือ Simple Mode ภาษาไทย](https://github.com/mehigosleep/mehigo-hair-manager/blob/main/docs/SIMPLE_MODE_GUIDE_TH.md) or [Simple Mode Guide in English](https://github.com/mehigosleep/mehigo-hair-manager/blob/main/docs/SIMPLE_MODE_GUIDE_EN.md). Advanced options remain documented in the [คู่มือฉบับเต็ม](https://github.com/mehigosleep/mehigo-hair-manager/blob/main/docs/USER_GUIDE_TH.md) and [Complete English Guide](https://github.com/mehigosleep/mehigo-hair-manager/blob/main/docs/USER_GUIDE_EN.md).

The **Save Folder** is the shared base folder. mehigo automatically creates a stable `Avatar_<id>` subfolder for each avatar instance in the Scene, so even a copied instance of the same Prefab does not overwrite the first avatar's Controller, Animation Clips, Expression Menus, or captured Icons. A Prefab asset opened directly uses its asset GUID.

## Generated content

mehigo Hair Manager creates or updates the Animator Controller and Layers, Animation Clips, Expression Menu, Parameters, icons, configuration assets, and required Modular Avatar components. Modular Avatar then uses this generated setup to perform the non-destructive merge at build time. Generated runtime assets are kept inside an avatar-specific `Avatar_<id>` subfolder. Running Generate/Update again for the same avatar updates its own assets, while a different avatar receives a separate output folder.

Do not manually edit generated assets unless you understand that a later Generate/Update for the same avatar may overwrite them. Assets produced by older versions directly inside the base Save Folder are not deleted automatically.

## Modular Avatar credit

Made for use with [Modular Avatar](https://modular-avatar.nadena.dev/) by bd_. Modular Avatar is the required core dependency and is licensed under the [MIT License](https://github.com/bdunderscore/modular-avatar/blob/main/COPYING.md). See the [official source repository](https://github.com/bdunderscore/modular-avatar).

This community project is not an official Modular Avatar project and is not affiliated with or endorsed by bd_ or the Modular Avatar project. No Modular Avatar logo or restricted image assets are included.

## Optional Gesture Manager integration

When [Gesture Manager](https://github.com/BlackStartx/VRC-Gesture-Manager) is installed, mehigo Hair Manager can load UI assets from the user's installed package to provide its familiar radial-menu appearance in the editor-only Menu Preview. Gesture Manager is optional, is not bundled with mehigo Hair Manager, and does not affect the generated avatar setup. The built-in fallback Preview remains available when Gesture Manager is not installed.

Gesture Manager UI assets © 2019–2023 BlackStartx and are provided under the [MIT License](https://github.com/BlackStartx/VRC-Gesture-Manager/blob/master/LICENSE.md). mehigo Hair Manager is an independent project and is not affiliated with or endorsed by Gesture Manager or its developer.

See [Third-Party Notices](THIRD_PARTY_NOTICES.md) for the complete dependency and optional integration credits.

## License

Copyright (c) 2026 mehigosleep. All rights reserved. See [LICENSE.md](https://github.com/mehigosleep/mehigo-hair-manager/blob/main/LICENSE.md) and [THIRD_PARTY_NOTICES.md](https://github.com/mehigosleep/mehigo-hair-manager/blob/main/THIRD_PARTY_NOTICES.md).
