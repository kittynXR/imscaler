# Migration Guide: From Window to NDMF Component

## What's Changed

The Immersive Scaler has been updated to work as an NDMF (Non-Destructive Modular Framework) component instead of an Editor Window. This provides better VRChat integration and ensures the ViewPosition is properly scaled.

## Key Improvements

1. **Non-Destructive**: Scaling is applied during avatar build/upload, not permanently to your project
2. **ViewPosition Scaling**: Automatically updates VRChat's ViewPosition to match the scaled avatar
3. **VRChat Integration**: Works seamlessly with VRChat's build pipeline
4. **Preview Support**: Can preview changes before building

## How to Use the New System

### 1. Remove Old Window-Based Files
Delete the old ImmersiveScalerWindow.cs if you have it in your project.

### 2. Add the Component
1. Select your VRChat avatar (must have VRCAvatarDescriptor)
2. Add Component → VRChat → Immersive Scaler
3. Configure your scaling parameters in the component

### 3. Configure Settings
The component has the same settings as the old window:
- Target Height
- Upper Body Percentage
- Arm/Leg Thickness
- etc.

### 4. Preview (Optional)
- Click "Preview Scaling" to see changes in the editor
- Click "Reset Preview" to undo preview changes
- Note: Preview is just for testing - actual scaling happens at build time

### 5. Build/Upload
Simply build and upload your avatar as normal. The scaling will be applied automatically during the build process.

## Important Notes

### ViewPosition Updates
The new system automatically calculates and updates the VRChat ViewPosition based on how the scaling affects eye height. This ensures your in-VR viewpoint matches your scaled avatar.

### Non-Destructive Workflow
- Your original avatar is never permanently modified
- Scaling only applies during the build process
- You can adjust settings and rebuild without accumulating changes

### Bone Scaling
The bone-by-bone scaling approach is preserved, but now includes:
- Automatic bone direction detection
- Better handling of different rig configurations
- Clamped values to prevent extreme scaling

## Troubleshooting

### "No VRCAvatarDescriptor found" Error
Make sure the component is on a GameObject that has a VRCAvatarDescriptor component in its parents.

### Scaling Not Applied
Ensure you have NDMF installed in your project. The scaling is applied during build, not in the editor (unless you use Preview).

### ViewPosition Issues
The system automatically calculates the correct ViewPosition. If it seems wrong:
1. Check that your avatar's eye bones are properly configured
2. Try using "Scale to Eyes" option for more accurate viewpoint
3. Use Preview to test before building

## Reverting to Old System

If you need to use the old window-based system:
1. Keep the old ImmersiveScalerWindow.cs
2. Don't add the ImmersiveScalerComponent
3. Use the window as before (VRChat menu → Immersive Scaler)

Note: The old system won't automatically update ViewPosition, so you'll need to adjust it manually after scaling.