# mehigo Hair Manager

**Version 1.0.1**

Unity Editor tool for managing multiple hairstyle setups on VRChat avatars with a non-destructive Modular Avatar workflow.

## Features

- Multiple hairstyle selector menu
- Automatic VRChat Avatar Descriptor detection
- Modular Avatar Merge Animator / Parameters / Menu Installer workflow
- Linked objects per hairstyle
- BlendShape Toggle controls
- BlendShape Radial Puppet controls
- Hair Material Presets
- Custom icons and Scene View icon capture
- Existing hair animator / wrapper compatibility options
- Conflict Scanner
- Performance Analyzer
- Avatar Optimizer (AAO) compatibility detection
- Thai / English editor UI
- Editable saved setup configuration

## Animator optimization

Version 1.0.0 uses the stable Standard animator layout for generated BlendShape controls.

If Avatar Optimizer Trace and Optimize is detected, **Let AAO Handle It** is recommended. mehigo generates the stable Standard controller and AAO can optimize it during the avatar build pipeline.

The experimental mehigo Direct BlendTree optimization is disabled in v1.0.0 because avatar testing found cross-influence issues with some BlendShape/Radial configurations.

## Requirements

- Unity project configured for VRChat Avatars
- VRChat Avatars SDK
- Modular Avatar

Optional:

- Avatar Optimizer (AAO)

v1.0.0 was developed against a project using VRChat SDK 3.7.5. Test your avatar after changing SDK or package versions.

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

Do not keep older mehigo Hair Generator scripts in the project at the same time if they contain the same internal class definitions.

## Basic workflow

1. Select your avatar or prefab.
2. Add hairstyle entries.
3. Configure activation, linked objects, BlendShapes, material presets, and icons.
4. Run Compatibility / Performance checks when needed.
5. Generate or update the setup.
6. Test the generated menu and animations before uploading the avatar.

## Generated content

mehigo creates generated Animator, animation, menu, parameter, and configuration assets used by Modular Avatar. Do not manually edit generated assets unless you understand that a later Generate/Update may overwrite them.

## License

Copyright (c) 2026 mehigosleep. All rights reserved. See `LICENSE.md`.
