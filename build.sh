#!/bin/bash

# VRChat Immersive Scaler Build Script
# Automates version bumping, package creation, and GitHub release

set -e  # Exit on error

# Colors for output
RED='\033[0;31m'
GREEN='\033[0;32m'
YELLOW='\033[1;33m'
NC='\033[0m' # No Color

# Function to print colored output
print_info() {
    echo -e "${GREEN}[INFO]${NC} $1"
}

print_error() {
    echo -e "${RED}[ERROR]${NC} $1"
}

print_warning() {
    echo -e "${YELLOW}[WARNING]${NC} $1"
}

# Check if we're in the right directory
if [ ! -f "VRChatImmersiveScaler/package.json" ]; then
    print_error "Must run from project root directory"
    exit 1
fi

# Check for uncommitted changes
if [ -n "$(git status --porcelain)" ]; then
    print_error "Uncommitted changes detected. Please commit or stash them first."
    exit 1
fi

# Get current version
CURRENT_VERSION=$(grep -o '"version": "[^"]*"' VRChatImmersiveScaler/package.json | cut -d'"' -f4)
print_info "Current version: $CURRENT_VERSION"

# Parse version components
IFS='.' read -r MAJOR MINOR PATCH <<< "$CURRENT_VERSION"

# Determine version bump type
if [ "$1" == "major" ]; then
    MAJOR=$((MAJOR + 1))
    MINOR=0
    PATCH=0
elif [ "$1" == "minor" ]; then
    MINOR=$((MINOR + 1))
    PATCH=0
elif [ "$1" == "patch" ] || [ -z "$1" ]; then
    PATCH=$((PATCH + 1))
else
    print_error "Invalid version bump type. Use: major, minor, patch (default: patch)"
    exit 1
fi

NEW_VERSION="$MAJOR.$MINOR.$PATCH"
print_info "New version will be: $NEW_VERSION"

# Confirm with user
read -p "Continue with release v$NEW_VERSION? (y/n) " -n 1 -r
echo
if [[ ! $REPLY =~ ^[Yy]$ ]]; then
    print_warning "Release cancelled"
    exit 0
fi

# Update package.json
print_info "Updating package.json..."
sed -i "s/\"version\": \"$CURRENT_VERSION\"/\"version\": \"$NEW_VERSION\"/" VRChatImmersiveScaler/package.json
sed -i "s/download\/v$CURRENT_VERSION\/dev.catnet.immersivescaler-$CURRENT_VERSION.zip/download\/v$NEW_VERSION\/dev.catnet.immersivescaler-$NEW_VERSION.zip/" VRChatImmersiveScaler/package.json

# Create the zip package
print_info "Creating release package..."
cd VRChatImmersiveScaler
zip -r ../cat.kittyn.immersive_scaler-$NEW_VERSION.zip . -x "*.meta" -x ".git/*" -x ".DS_Store"
cd ..

# Commit changes
print_info "Committing changes..."
git add VRChatImmersiveScaler/package.json
git add cat.kittyn.immersive_scaler-$NEW_VERSION.zip
git commit -m "Release v$NEW_VERSION

- Bump version from $CURRENT_VERSION to $NEW_VERSION

🤖 Generated with build.sh"

# Create tag
print_info "Creating git tag..."
git tag v$NEW_VERSION

# Push to GitHub
print_info "Pushing to GitHub..."
git push origin main --tags

# Create GitHub release
print_info "Creating GitHub release..."
gh release create v$NEW_VERSION \
    --title "Release v$NEW_VERSION" \
    --notes "## VRChat Immersive Scaler v$NEW_VERSION

### Installation
1. Download \`cat.kittyn.immersive_scaler-$NEW_VERSION.zip\` below
2. Import into Unity using VRChat Creator Companion or Package Manager

### What's Changed
- Version bump to v$NEW_VERSION

See [README](https://github.com/kittynXR/imscaler#readme) for usage instructions." \
    cat.kittyn.immersive_scaler-$NEW_VERSION.zip

print_info "Release v$NEW_VERSION completed successfully!"
print_info "GitHub Release: https://github.com/kittynXR/imscaler/releases/tag/v$NEW_VERSION"