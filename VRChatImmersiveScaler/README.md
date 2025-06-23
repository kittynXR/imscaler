# VRChat Immersive Scaler for Unity

An NDMF-based Unity tool that provides the same powerful avatar scaling capabilities as the Blender Immersive Scaler addon, directly within Unity. This tool helps VRChat users properly scale their avatars to match real-world proportions and maintain immersion in VR, with automatic ViewPosition adjustment.

## Features

- **Automated Avatar Scaling**: One-click scaling to match your real height and proportions
- **VRChat IK Compatibility**: Maintains proper VRChat IK measurements and arm ratios
- **Customizable Proportions**:
  - Upper body vs lower body ratio
  - Arm and leg thickness
  - Thigh vs calf proportions
  - Hand and foot scaling options
- **Advanced Options**:
  - Keep head size mode
  - Scale by relative proportions
  - Custom VRChat arm ratio adjustment
- **Additional Tools**:
  - Finger spreading for better controller tracking
  - Hip bone adjustment
  - Center avatar at origin
  - Move avatar to floor

## Installation

1. Install required dependencies:
   - NDMF (Non-Destructive Modular Framework)
   - VRChat SDK3 Avatars
2. Download the latest release
3. Import the Unity package into your project
4. Add the Immersive Scaler component to your avatar

## Requirements

- Unity 2019.4 or newer (VRChat's supported version)
- Avatar must have a Humanoid rig configuration
- VRChat SDK3 Avatars (required)
- NDMF (required for build-time processing)

## Usage

### Basic Scaling

1. Select your VRChat avatar (with VRCAvatarDescriptor)
2. Add Component → VRChat → Immersive Scaler
3. Configure your desired settings
4. Build/upload your avatar - scaling is applied automatically!

### Preview Mode

- Click "Preview Scaling" to see changes in editor
- Click "Cancel Preview" to undo preview changes
- Actual scaling happens during avatar build

### Parameters

#### Main Settings
- **Target Height**: Your desired avatar height in meters
- **Upper Body %**: Percentage of height from eyes to feet that should be upper body
- **Custom Arm Ratio**: VRChat's arm span ratio (default: 0.4537)

#### Customization Options
- **Arm/Leg Thickness**: How much to preserve limb thickness during scaling
- **Upper Leg %**: Ratio of thigh to total leg length
- **Scale to Eyes**: Use eye height instead of top of head for measurements
- **Scale Hands/Feet**: Whether to scale extremities with limbs

#### Advanced Modes
- **Scale by Relative Proportions**: Use arm/leg ratio instead of upper body percentage
- **Keep Head Size**: Maintain head size by scaling the torso

### Getting Current Values

Use the "Get Current" buttons next to parameters to measure your avatar's existing proportions:
- Current height
- Current upper body percentage
- Current arm ratio
- Current thigh percentage

### Additional Tools

#### Finger Spreading
Spreads fingers apart for better Index controller finger tracking:
1. Enable "Finger Spreading" section
2. Choose whether to ignore thumb
3. Adjust spread factor (1.0 = default spread)
4. Click "Spread Fingers"

#### Hip Fix
Shrinks the hip bone to be closer to the spine, which can help with some animation issues:
1. Click "Shrink Hip Bone" in the Hip Fix section

## How It Works

The tool uses the same algorithms as the original Blender addon, now with NDMF integration:

1. **Measures** your avatar's current proportions
2. **Calculates** the necessary scaling to achieve VRChat-compatible proportions
3. **Applies** non-uniform scaling to different body parts during build
4. **Updates** VRChat's ViewPosition to match the scaled eye height
5. **Maintains** VRChat's IK system compatibility

The scaling preserves VRChat's head-to-hand distance measurement while adjusting your avatar to match your real proportions. Most importantly, it automatically calculates and updates the ViewPosition based on how the scaling affects your avatar's eye height.

## Tips

- Always make a backup of your avatar before scaling
- Test your avatar in VRChat after scaling
- Fine-tune the upper body percentage to match your real proportions
- Use "Scale to Eyes" for more accurate viewpoint positioning
- Adjust arm/leg thickness if limbs look too thin or thick after scaling

## Differences from Blender Version

- Works directly in Unity (no need to export/import)
- NDMF integration for non-destructive scaling
- Automatic ViewPosition adjustment
- Real-time preview in Unity's Scene view
- Component-based workflow instead of window
- Applied during avatar build/upload

## Known Limitations

- Blend shape adjustments may need manual tweaking
- Complex avatar setups might require additional adjustments
- Non-humanoid bones are not automatically scaled

## Support

For issues, feature requests, or questions:
- Create an issue on the GitHub repository

## Credits

VRChat Immersive Scaler Unity Tool
Developed to help VRChat users achieve proper real-world avatar proportions

Inspired by the [Immersive Scaler Blender addon](https://github.com/Pager07/immersive_scaler) - scaling algorithms reimplemented for Unity

## License

MIT License - See LICENSE file for details