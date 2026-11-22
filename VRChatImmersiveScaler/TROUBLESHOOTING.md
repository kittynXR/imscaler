# Immersive Scaler Unity - Troubleshooting Guide

## Common Issues and Solutions

### Legs Being Squished to Comically Small Size

This is often caused by incorrect scale calculations or bone orientation issues. Here's how to debug:

1. **Enable Debug Logging**
   - Add `KITTYN_IMMERSIVE_SCALER_DEBUG` to Unity's scripting define symbols when you need detailed scale logs
   - Check the Unity Console for messages from "ImmersiveScaler"
   - Look for extreme values, warnings about invalid measurements, or scale ratios that were clamped

2. **Check Bone Directions**
   - The tool automatically detects bone directions
   - Debug logging can show detected directions for each bone
   - Most humanoid rigs have bones extending along local Y axis
   - Some rigs may use different axes

3. **Try Different Settings**
   - Start with default values:
     - Upper Body %: 44%
     - Arm Thickness: 50%
     - Leg Thickness: 50%
     - Upper Leg %: 53%
   - Use the "Reset Scales" button before each test
   - Try enabling "Skip Main Rescale" in Debug Options to isolate the issue

4. **Test Step by Step**
   - Enable debug options one at a time:
     - First try with "Skip Main Rescale" checked
     - Then try with only "Skip Height Scaling" checked
     - This helps identify which step causes the issue

### Avatar Not Scaling Correctly

1. **Verify Avatar Setup**
   - Ensure avatar has Humanoid rig type
   - Check that all required bones are mapped in the Avatar configuration
   - Verify bone hierarchy is standard (no extra parent transforms)

2. **Check Initial State**
   - Click "Reset Scales" to ensure all bones start at scale (1,1,1)
   - Some imported avatars may have non-uniform scales already applied

3. **Review Console Output**
   - Look for messages about missing bones
   - Look for warnings about invalid current/target height, NaN values, or clamped ratios
   - Check calculated values for reasonableness:
     - LegScaleRatio should typically be between 0.5 and 2.0
     - Thickness values should be between 0.1 and 1.0

### Arms or Hands Scaling Incorrectly

1. **Hand Scaling Issues**
   - Toggle "Scale Hands" option
   - Some avatars have complex hand hierarchies that may not scale correctly
   - Try manually adjusting hand scales after main scaling

2. **Arm Direction Detection**
   - Check console logs for arm bone directions
   - Arms typically extend along local X or Z axis (sideways)
   - Incorrect detection will cause wrong scaling axis

### Height Not Matching Target

1. **Scale to Eyes vs Top**
   - Toggle "Scale to Eyes" option
   - Eye height is more accurate for VRChat viewpoint
   - Top of head includes hair/accessories which may throw off calculations

2. **Check Mesh Bounds**
   - Tool prefers humanoid body meshes (legs/hips) for floor/height measurements and ignores obvious outlier bounds
   - If a prop is still affecting measurements, enable "Measurement Overrides" on the component and assign only your body/head meshes
   - If you still have issues, enable "Use bone-based floor calculation" (Debug Options) and/or fix incorrect `SkinnedMeshRenderer` bounds on your avatar meshes
   - Props/weapons with large bounds can still cause problems if they are merged into the main body mesh

### Debug Information

When reporting issues, please include:
1. Console log output, especially warnings and any `KITTYN_IMMERSIVE_SCALER_DEBUG` logs
2. Avatar source (where it's from, how it was imported)
3. Current measurement values shown in the tool
4. Which settings you're using
5. Screenshots of the avatar before and after scaling

### Advanced Debugging

1. **Manual Scale Testing**
   - Select individual bones in Scene view
   - Note their local axes (red=X, green=Y, blue=Z)
   - Manually scale along different axes to see which extends the bone

2. **Hierarchy Issues**
   - Check for extra GameObjects between humanoid bones
   - Ensure no negative scales in parent transforms
   - Verify world scale of avatar root is (1,1,1)

3. **Testing with Simple Avatar**
   - Try the tool on a Unity default humanoid
   - If it works there, the issue is avatar-specific
   - Compare bone setups between working and non-working avatars

## Quick Fixes

- **Reset Everything**: Use "Reset Scales" button, then set avatar root scale to (1,1,1)
- **Conservative Settings**: Start with all thickness at 100%, all other values at defaults
- **Isolate Issues**: Use debug checkboxes to skip steps and find which one causes problems
- **Check VRChat Setup**: Ensure VRCAvatarDescriptor viewpoint is set correctly

## Getting Help

If you continue to have issues:
1. Save your Unity project
2. Take screenshots of the tool settings and console output
3. Note your Unity version and VRChat SDK version. Use SDK 3.10.x with Unity 2022 or SDK 3.11.x with Unity 6. If installing would downgrade the SDK, refresh your VPM listings and select Immersive Scaler 1.6.2 or later.
4. Create an issue on GitHub with all this information
