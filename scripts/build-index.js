const fs = require('fs');
const path = require('path');

// Read package.json to get version
const packageJson = JSON.parse(fs.readFileSync('VRChatImmersiveScaler/package.json', 'utf8'));

// Create the listing structure - EXACT format that works
const listing = {
  "name": "Immersive Scaler VRChat Tools",
  "author": "kittyn cat",
  "url": "https://immersive_scaler.kittyn.cat/index.json",
  "id": "cat.kittyn.vpm",
  "packages": {
    "cat.kittyn.immersive_scaler": {
      "versions": {
        [packageJson.version]: {
          "name": "cat.kittyn.immersive_scaler",
          "displayName": "VRChat Immersive Scaler",
          "version": packageJson.version,
          "unity": "2022.3",
          "description": "A Unity Editor tool for properly scaling VRChat avatars to match real-world proportions while maintaining VR immersion.",
          "author": {
            "name": "kittyn cat",
            "email": "",
            "url": "https://github.com/kittynXR"
          },
          "url": `https://github.com/${process.env.GITHUB_REPOSITORY}/releases/download/v${packageJson.version}/cat.kittyn.immersive_scaler-${packageJson.version}.zip`,
          "licenseUrl": "https://github.com/kittynXR/imscaler/LICENSE"
        }
      }
    }
  }
};

// Write index.json
fs.writeFileSync('index.json', JSON.stringify(listing, null, 2));
console.log('Generated index.json with version', packageJson.version);