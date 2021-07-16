#!/usr/bin/env bash

# Git remote update && git pull
echo "Get newest changes from Repo"
git remote update && git pull

# Build WhMgrr.dll
echo "Building WhMgr..."
~/.dotnet/dotnet build || dotnet build

# Copy example configs
if [ "$1" == "examples" ] || [ "$1" == "example" ] || [ "$1" == "all" ]; then
    echo "Copying examples"
    cp -R "examples/alerts" "bin/"
    cp -R "examples/filters" "bin/"
    cp -R "examples/alarms" "bin/"
    cp -R "examples/discords" "bin/"
fi
if [ "$1" == "geofences" ] || [ "$1" == "geofence" ] || [ "$1" == "all" ]; then
    echo "Copying geofences..."
    cp -R "geofences" "bin/"
fi

# Copy locale translation files
echo "Copying locale translation files... "
mkdir -p "bin/static"

echo "Copying latest master file..."
mkdir -p "bin/static/data"
cp "static/data/masterfile.json" "bin/static/data/masterfile.json"
cp "static/data/cpMultipliers.json" "bin/static/data/cpMultipliers.json"

echo "Update Complete"
