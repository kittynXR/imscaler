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
- Targets Unity 2022.3.22f1 exclusively for the maintained package

### Dependencies

- `nadena.dev.ndmf`: >=1.13.0 <2.0.0-a - Required for build-time processing
- `com.vrchat.avatars`: >=3.10.3 <3.11.0-a - VRChat SDK for avatar descriptors
- Modular Avatar is optional; the NDMF pass declares ordering after `nadena.dev.modular-avatar` without requiring its runtime assembly.

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
1. Reads the descriptor ViewPosition at preview/build start
2. Converts that world-distance vector to avatar-local coordinates with `InverseTransformVector` and captures the local offset from the eyes
3. Applies scaling transformations and additional tools, including the hip adjustment
4. Measures the final local eye position
5. Converts the final eye-plus-offset back with `TransformVector` for `VRCAvatarDescriptor.ViewPosition`

`ViewPosition` is a world-distance vector relative to the avatar root, not an unscaled local point. The world viewpoint is `root.position + ViewPosition`. Use `ImmersiveScalerViewpoint` for conversion and visualization. Reconcile after every operation that can move the head, including when main/height scaling is skipped.

The Debug foldout provides read-only measurements, the last preview trace, and the latest Scaler build-pass trace in the current Editor session. Capture actual calculations through the core's scoped `DiagnosticLog`; do not duplicate the scaling algorithm in the diagnostic UI. Keep diagnostic state editor-only and preserve preview reports after cancellation.

EditMode regression tests are in `VRChatImmersiveScaler/Tests/Editor`. Add `cat.kittyn.immersive-scaler` to a validation project's manifest `testables` list to run them. The fixtures construct synthetic humanoids and cover root-scale/ViewPosition conversion, intentional offsets, hip-only processing, inactive-bone restoration, avatar switching, NDMF processing, and read-only diagnostic capture.

### Testing Workflow

1. Add component to test avatar in Unity scene
2. Use "Preview Scaling" button to test in editor
3. Check Scene view for visual results
4. Use "Reset Preview" to undo
5. Upload avatar to VRChat for final testing
