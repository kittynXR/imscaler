# CLAUDE.md

This file provides guidance to Claude Code (claude.ai/code) when working with code in this repository.

## Project Overview

VRChat Immersive Scaler is a Unity Editor tool that scales VRChat avatars to match real-world proportions while maintaining VR immersion. It's implemented as a Unity package using NDMF (Non-Destructive Modular Framework) for build-time processing.

## Architecture

### Core Components

1. **Runtime Component** (`Runtime/ImmersiveScalerComponent.cs`): MonoBehaviour that stores scaling configuration on avatars
2. **Editor Integration** (`Editor/ImmersiveScalerComponentEditor.cs`): Custom Unity inspector UI
3. **NDMF Plugin** (`Editor/ImmersiveScalerPlugin.cs`): Integrates with VRChat's build pipeline, applies scaling during avatar upload
4. **Scaling Core** (`Editor/ImmersiveScalerCore.cs`): Contains the actual scaling algorithms and transforms

### Key Design Patterns

- **Non-Destructive**: All scaling is applied during build time via NDMF, original avatar is preserved
- **Component-Based**: Users add `ImmersiveScalerComponent` to avatars to configure scaling
- **Build Pipeline Integration**: Scaling happens automatically during VRChat avatar upload

## Development Guidelines

### Unity-Specific Considerations

- This is a Unity package, not a standalone application
- No traditional build/test commands - functionality is tested within Unity Editor
- Assembly definitions (`.asmdef` files) separate Editor and Runtime code
- Requires Unity 2019.4+ (VRChat's version)

### Dependencies

- `nadena.dev.ndmf`: ^1.0.0 - Required for build-time processing
- `com.vrchat.avatars`: ^3.0.0 - VRChat SDK for avatar descriptors

### Code Organization

- **Editor/** - All build-time and editor-only functionality
  - Must use `#if UNITY_EDITOR` guards when referencing from Runtime
  - Contains UI, scaling logic, and NDMF integration
- **Runtime/** - Only the component that stores configuration
  - Should be minimal - just data storage
  - Cannot reference Editor code

### Important Classes

- `ScalingParameters`: Data class containing all scaling options
- `ImmersiveScalerUtilities`: Static helper methods for transform calculations
- `VRCBoneMapper`: Maps Unity humanoid bones to VRChat bone names

### ViewPosition Handling

The tool automatically adjusts VRChat's ViewPosition after scaling:
1. Measures original eye height before scaling
2. Applies scaling transformations
3. Calculates new eye height
4. Updates VRCAvatarDescriptor.ViewPosition to maintain correct viewpoint

### Testing Workflow

1. Add component to test avatar in Unity scene
2. Use "Preview Scaling" button to test in editor
3. Check Scene view for visual results
4. Use "Reset Preview" to undo
5. Upload avatar to VRChat for final testing