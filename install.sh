# Download .NET Core 2.1 installer
echo "Downloading .NET Core 2.1 installer..."
wget https://dotnetwebsite.azurewebsites.net/download/dotnet-core/scripts/v1/dotnet-install.sh

# Make installer executable
echo "Setting executable permissions..."
chmod +x dotnet-install.sh

# Install .NET Core 2.1.0
echo "Launching .NET Core installer..."
./dotnet-install.sh --version 2.1.803

# Delete .NET Core 2.1.0 installer
echo "Deleting .NET Core installer..."
rm dotnet-install.sh

# Clone repository
echo "Cloning repository..."
git clone https://github.com/versx/WhMgr -b netcore

# Change directory into cloned repository
echo "Changing directory..."
cd WhMgr

# Build WhMgr.dll
echo "Building WhMgr..."
~/.dotnet/dotnet build

# Copy example config
echo "Copying example files..."
cp -R examples/Alerts bin/debug/netcoreapp2.1/Alerts/
cp -R examples/Filters bin/debug/netcoreapp2.1/Filters/
cp -R examples/Geofences bin/debug/netcoreapp2.1/Geofences/
cp -R static/ bin/debug/netcoreapp2.1/static/
cp alarms.example.json bin/debug/netcoreapp2.1/alarms.json
cp config.example.json bin/debug/netcoreapp2.1/config.json

echo "Changing directory to build folder..."
cd bin/debug/netcoreapp2.1