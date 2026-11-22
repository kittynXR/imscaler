# Migration Guide: From Window to NDMF Component

## What's Changed

Immersive Scaler is maintained as an NDMF component for Unity 2022 and Unity 6. The legacy window remains available for manual/destructive testing, but the component workflow is the supported path for VRChat avatar builds.

## Key Improvements

1. Non-destructive scaling is applied during avatar build/upload.
2. ViewPosition is updated from the measured final local eye position while preserving the user's original descriptor offset.
3. Measurement renderer overrides can ignore hidden props, accessories, or meshes with bad bounds.
4. Mesh outlier filtering and bone-based floor fallback reduce bad hidden-collider style measurements.
5. The build pass runs after Modular Avatar when it is installed, without requiring Modular Avatar as a package dependency.

## Supported Stack

- Unity 2022.3.22f1 or Unity 6000.0.67f1
- VRChat SDK Avatars 3.10.x (3.10.3 or later) for Unity 2022, or 3.11.x (starting with 3.11.0-beta.1) for Unity 6
- NDMF >=1.13.0 <2.0.0-a

## How to Use the Component

1. Select your VRChat avatar with a `VRCAvatarDescriptor`.
2. Add Component -> VRChat -> Immersive Scaler.
3. Click "Get Current" to populate values from the avatar.
4. Enable measurement renderer overrides if props or bad mesh bounds affect the displayed stats.
5. Use Preview Scaling to inspect changes in the editor.
6. Build/upload normally; NDMF applies the scaling during the avatar build.

## Important Notes

### ViewPosition Updates

Build-time processing reads the current descriptor ViewPosition at build start. It no longer treats the serialized `originalViewPosition` fields on old components as long-lived source data; those legacy fields are kept only for compatibility.

### Non-Destructive Workflow

- Your source avatar is not permanently modified by the component workflow.
- Scaling applies during build.
- Preview changes restore transforms and ViewPosition when cancelled.

### Legacy Window

The Tools window is still available for legacy/manual workflows. It can make destructive changes to the scene avatar, so duplicate the avatar first if you use it outside preview mode.

## Troubleshooting

### "No VRCAvatarDescriptor found" Error

Make sure the component is on the avatar or on a child under a GameObject with a `VRCAvatarDescriptor`.

### Scaling Not Applied

Ensure NDMF is installed and the avatar build is using the SDK build pipeline. The component workflow applies at build time, not while idle in the editor.

### Measurement Looks Wrong

Use measurement renderer overrides for separate body/head meshes, hidden accessories, or imported props with incorrect bounds. If the floor is still wrong, try the bone-based floor calculation option.
