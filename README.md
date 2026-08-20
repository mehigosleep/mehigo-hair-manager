# mehigo Hair Manager

**Version 1.1.0**

Unity Editor tool for managing multiple hairstyle setups on VRChat avatars with Modular Avatar. The tool generates separate Animator, menu, parameter, and configuration assets without directly editing the avatar's original FX controller, expression menu, or expression parameters.

[คู่มือภาษาไทย](https://github.com/mehigosleep/mehigo-hair-manager/blob/main/docs/USER_GUIDE_TH.md) · [English User Guide](https://github.com/mehigosleep/mehigo-hair-manager/blob/main/docs/USER_GUIDE_EN.md) · [Changelog](https://github.com/mehigosleep/mehigo-hair-manager/blob/main/CHANGELOG.md)

![mehigo Hair Manager with a VRChat avatar](https://raw.githubusercontent.com/mehigosleep/mehigo-hair-manager/main/docs/images/01-project-avatar.png)

## Features

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
- Gesture Manager (for in-game-style Preview icons and colors)

v1.1.0 was tested with VRChat SDK 3.10.4. Test your avatar after changing SDK or package versions.

## Install with VCC

1. Add the Modular Avatar repository to VCC if it is not already installed: `https://vpm.nadena.dev/vpm.json`.
2. Add the mehigo repository to VCC: `https://mehigosleep.github.io/mehigo-hair-manager/vpm.json`.
3. Open your avatar project in VCC and add **mehigo Hair Manager**.
4. Open Unity and use **Tools > mehigo > Hair Manager**.

## Manual install

1. Copy `Editor/MehigoHairManager.cs` into your Unity project's `Assets` folder (inside an Editor folder is recommended).
2. Make sure Modular Avatar is installed.
3. Open Unity.
4. Use **Tools > mehigo > Hair Manager**.

Do not install the tool through both VCC and the manual method in the same project. Also remove older mehigo Hair Generator scripts that contain the same internal class definitions; otherwise Unity may report duplicate-class compilation errors.

## Basic workflow

1. Select your avatar or prefab.
2. Add hairstyle entries.
3. Configure activation, linked objects, BlendShapes, material presets, and icons.
4. Run the Conflict Scanner on the Generate page when needed.
5. Generate or update the setup.
6. Test the generated menu and animations before uploading the avatar.

For the complete illustrated workflow, see the [คู่มือภาษาไทย](https://github.com/mehigosleep/mehigo-hair-manager/blob/main/docs/USER_GUIDE_TH.md) or [English User Guide](https://github.com/mehigosleep/mehigo-hair-manager/blob/main/docs/USER_GUIDE_EN.md).

The **Save Folder** is the shared base folder. mehigo automatically creates a stable `Avatar_<id>` subfolder for each avatar instance in the Scene, so even a copied instance of the same Prefab does not overwrite the first avatar's Controller, Animation Clips, Expression Menus, or captured Icons. A Prefab asset opened directly uses its asset GUID.

## Generated content

mehigo creates generated Animator, animation, menu, parameter, and configuration assets used by Modular Avatar. Generated runtime assets are kept inside an avatar-specific `Avatar_<id>` subfolder. Running Generate/Update again for the same avatar updates its own assets, while a different avatar receives a separate output folder.

Do not manually edit generated assets unless you understand that a later Generate/Update for the same avatar may overwrite them. Assets produced by older versions directly inside the base Save Folder are not deleted automatically.

## License

Copyright (c) 2026 mehigosleep. All rights reserved. See [LICENSE.md](https://github.com/mehigosleep/mehigo-hair-manager/blob/main/LICENSE.md).
