#!/bin/bash

# 1. Define Names
APP_NAME="BusinessSuite"
PUBLISH_DIR="bin/Release/net8.0/osx-x64/publish"
BUNDLE_DIR="publish/$APP_NAME.app"
CONTENTS_DIR="$BUNDLE_DIR/Contents"

# 2. Create the Structure
mkdir -p "$CONTENTS_DIR/MacOS"
mkdir -p "$CONTENTS_DIR/Resources"

# 3. Copy the Executable and Info.plist
cp "$PUBLISH_DIR/$APP_NAME" "$CONTENTS_DIR/MacOS/"
cp "Info.plist" "$CONTENTS_DIR/"
# If you have an icon, uncomment the line below:
# cp "icon.icns" "$CONTENTS_DIR/Resources/"

# 4. Critical: Make the file executable
chmod +x "$CONTENTS_DIR/MacOS/$APP_NAME"

echo "Professional .app bundle created at $BUNDLE_DIR"