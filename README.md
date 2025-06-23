# VRChat Immersive Scaler

A Unity tool for properly scaling VRChat avatars to match real-world proportions while maintaining VR immersion. Features NDMF integration for non-destructive avatar modifications.

[![VCC Compatible](https://img.shields.io/badge/VCC-Compatible-green)](https://vcc.docs.vrchat.com)
[![Unity 2019.4+](https://img.shields.io/badge/Unity-2019.4+-blue)](https://unity.com)
[![NDMF Compatible](https://img.shields.io/badge/NDMF-Compatible-purple)](https://github.com/bdunderscore/ndmf)

## 🚀 Quick Install

Add to VCC with one click:

<a href="vcc://vpm/addRepo?url=https://kittynxr.github.io/imscaler/index.json">
  <img src="https://img.shields.io/badge/Add%20to-VCC-blue?style=for-the-badge&logo=data:image/svg+xml;base64,PHN2ZyB3aWR0aD0iMjQiIGhlaWdodD0iMjQiIHZpZXdCb3g9IjAgMCAyNCAyNCIgZmlsbD0ibm9uZSIgeG1sbnM9Imh0dHA6Ly93d3cudzMub3JnLzIwMDAvc3ZnIj4KPHBhdGggZD0iTTEyIDJDNi40OCAyIDIgNi40OCAyIDEyQzIgMTcuNTIgNi40OCAyMiAxMiAyMkMxNy41MiAyMiAyMiAxNy41MiAyMiAxMkMyMiA2LjQ4IDE3LjUyIDIgMTIgMlpNMTYgMTNIMTNWMTZDMTMgMTYuNTUgMTIuNTUgMTcgMTIgMTdDMTEuNDUgMTcgMTEgMTYuNTUgMTEgMTZWMTNIOEM3LjQ1IDEzIDcgMTIuNTUgNyAxMkM3IDExLjQ1IDcuNDUgMTEgOCAxMUgxMVY4QzExIDcuNDUgMTEuNDUgNyAxMiA3QzEyLjU1IDcgMTMgNy40NSAxMyA4VjExSDE2QzE2LjU1IDExIDE3IDExLjQ1IDE3IDEyQzE3IDEyLjU1IDE2LjU1IDEzIDE2IDEzWiIgZmlsbD0id2hpdGUiLz4KPC9zdmc+" alt="Add to VCC">
</a>

Or manually add this URL to VCC:
```
https://kittynxr.github.io/imscaler/index.json
```

## 📋 Features

### Core Scaling Features
- ✅ **Automated Avatar Scaling** - One-click scaling to match your real height
- ✅ **VRChat IK Support** - Compatible with both legacy and IK 2.0 systems
- ✅ **Automatic ViewPosition** - Maintains correct eye position after scaling
- ✅ **Non-Destructive** - Unity version uses NDMF for build-time processing

### Customization Options
- 🎯 Upper body vs lower body ratio adjustment
- 🎯 Arm and leg thickness control
- 🎯 Thigh vs calf proportion tuning
- 🎯 Hand and foot scaling options
- 🎯 Custom VRChat arm ratio (IK arm length)

### Advanced Features
- 🔧 Multiple measurement methods (eye height, total height, various arm measurements)
- 🔧 Finger spreading for better controller tracking
- 🔧 Hip bone adjustment tool
- 🔧 Center avatar at origin
- 🔧 Automatic floor placement

## 📋 Requirements

- Unity 2019.4 or newer (VRChat's supported version)
- VRChat SDK3 Avatars
- NDMF (installed automatically via VCC)
- Avatar with Humanoid rig configuration

## 🛠️ Usage

### Basic Setup
1. Add the **Immersive Scaler** component to your avatar
2. Click "Get Current" to populate values from your avatar
3. Adjust settings as needed
4. Use "Preview Scaling" to see changes
5. Upload your avatar - scaling is applied automatically!

### Two Ways to Use

#### Method 1: NDMF Component (Recommended)
- Add the Immersive Scaler component to your avatar
- Non-destructive - original avatar is preserved
- Scaling applied during upload/build

#### Method 2: Tools Menu
- Go to Tools → Immersive Avatar Scaler
- Make permanent changes to your avatar
- Useful for testing and debugging

[📖 Full Documentation](VRChatImmersiveScaler/README.md)

## 🔧 Troubleshooting

### Common Issues

**Avatar shrinks unexpectedly**
- Click "Get Current" before making adjustments
- Ensure you're using the correct measurement methods

**Preview doesn't match final result**
- Make sure all bones are properly mapped
- Check that your avatar has a valid Humanoid configuration

**VCC can't find the package**
- Ensure you've added the repository URL correctly
- Try refreshing your package list in VCC

[📖 Full Troubleshooting Guide](VRChatImmersiveScaler/TROUBLESHOOTING.md)

## 🤝 Contributing

Contributions are welcome! Please read our contributing guidelines before submitting PRs.

1. Fork the repository
2. Create your feature branch (`git checkout -b feature/AmazingFeature`)
3. Commit your changes (`git commit -m 'Add some AmazingFeature'`)
4. Push to the branch (`git push origin feature/AmazingFeature`)
5. Open a Pull Request

## 📜 License

This project is licensed under the MIT License - see the LICENSE file for details.

## 🙏 Acknowledgments

- NDMF framework by bd_ for non-destructive Unity modifications
- VRChat community for testing and feedback
- All contributors to the Immersive Scaler project

## 📞 Support

- **Issues**: [GitHub Issues](https://github.com/kittynXR/imscaler/issues)
- **Discord**: [Join our Discord](https://discord.gg/yourdiscord)
- **Documentation**: [Wiki](https://github.com/kittynXR/imscaler/wiki)

---

Made with ❤️ for the VRChat community