const fs = require('fs');
const path = require('path');

// Read package.json to get version
const packageJson = JSON.parse(fs.readFileSync('VRChatImmersiveScaler/package.json', 'utf8'));

// Try to read existing index.json to preserve previous versions
let existingVersions = {};
if (fs.existsSync('index.json')) {
  try {
    const existingIndex = JSON.parse(fs.readFileSync('index.json', 'utf8'));
    if (existingIndex.packages?.['cat.kittyn.immersive_scaler']?.versions) {
      existingVersions = existingIndex.packages['cat.kittyn.immersive_scaler'].versions;
      console.log(`Found ${Object.keys(existingVersions).length} existing versions`);
    }
  } catch (error) {
    console.warn('Could not parse existing index.json:', error);
  }
}

// Add/update current version
existingVersions[packageJson.version] = {
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
  "url": `https://github.com/${process.env.GITHUB_REPOSITORY || 'kittynXR/imscaler'}/releases/download/v${packageJson.version}/cat.kittyn.immersive_scaler-${packageJson.version}.zip`,
  "licenseUrl": "https://github.com/kittynXR/imscaler/LICENSE"
};

// Create the listing structure with all versions
const listing = {
  "name": "Immersive Scaler VRChat Tools",
  "author": "kittyn cat",
  "url": "https://immersivescaler.kittyn.cat/index.json",
  "id": "cat.kittyn.vpm",
  "packages": {
    "cat.kittyn.immersive_scaler": {
      "versions": existingVersions
    }
  }
};

// Write index.json
fs.writeFileSync('index.json', JSON.stringify(listing, null, 2));
console.log(`Generated index.json with version ${packageJson.version} (total versions: ${Object.keys(existingVersions).length})`);