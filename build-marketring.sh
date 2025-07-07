#!/bin/bash
# Build Marketring.BigCommerce package
# Usage: ./build-marketring.sh

echo "Building Marketring.BigCommerce package..."

# Clean and build in Release mode
dotnet clean -c Release
dotnet build -c Release

# Pack with Marketring package name
dotnet pack Fusionary.BigCommerce/Fusionary.BigCommerce.csproj \
  -p:PackageId=Marketring.BigCommerce \
  -p:Title="Marketring.BigCommerce" \
  -p:Description="BigCommerce API client with Cart API (Marketring Internal Build)" \
  -p:Authors="Marketring" \
  -c Release \
  --no-build

if [ $? -eq 0 ]; then
    echo "✅ Package built successfully!"
    echo "📦 Package location:"
    ls -la Fusionary.BigCommerce/bin/Release/Marketring.BigCommerce.*.nupkg
else
    echo "❌ Build failed!"
    exit 1
fi