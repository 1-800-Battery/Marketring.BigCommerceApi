# Package Management Guide

## Overview

This repository publishes two different NuGet packages:

1. **Fusionary.BigCommerce** - Standard upstream-compatible package
2. **Marketring.BigCommerce** - Internal package with Cart API enhancements

## Publishing Workflow

### Automated Publishing (Recommended)

#### 1. Tag-Based Release
```bash
# Create and push a Marketring-specific tag
git tag marketring-v1.2.0
git push origin marketring-v1.2.0

# For pre-releases
git tag marketring-v1.2.0-preview.1
git push origin marketring-v1.2.0-preview.1
```

#### 2. Manual Release
1. Go to **Actions** → **Build and Publish Marketring.BigCommerce**
2. Click **Run workflow**
3. Enter version (e.g., `1.2.0` or `1.2.0-preview.1`)
4. Select if it's a pre-release
5. Click **Run workflow**

### Package Destinations

- **GitHub Packages**: `https://github.com/orgs/1-800-Battery/packages`
- **Package ID**: `Marketring.BigCommerce`
- **Repository**: `1-800-Battery/Marketring.BigCommerceApi`
- **Versions**: Follow semantic versioning (e.g., 1.2.0, 1.2.1-preview.1)

## Consuming Packages

### 1. Configure Package Source

Add to your project's `NuGet.Config` or global config:

```xml
<?xml version="1.0" encoding="utf-8"?>
<configuration>
  <packageSources>
    <add key="github-battery" value="https://nuget.pkg.github.com/1-800-Battery/index.json" />
  </packageSources>
</configuration>
```

### 2. Authentication Setup

#### Option A: Personal Access Token (Development)
1. Create GitHub Personal Access Token with `read:packages` scope
2. Add to `~/.nuget/NuGet/NuGet.Config`:

```xml
<packageSourceCredentials>
  <github-battery>
    <add key="Username" value="your-github-username" />
    <add key="ClearTextPassword" value="your-personal-access-token" />
  </github-battery>
</packageSourceCredentials>
```

#### Option B: GitHub Actions (CI/CD)
```yaml
- name: Setup .NET
  uses: actions/setup-dotnet@v4
  with:
    dotnet-version: 8.0.x
    source-url: https://nuget.pkg.github.com/1-800-Battery/index.json
  env:
    NUGET_AUTH_TOKEN: ${{ secrets.GITHUB_TOKEN }}
```

### 3. Install Package

```bash
# Install specific version
dotnet add package Marketring.BigCommerce --version 1.2.0

# Install latest version
dotnet add package Marketring.BigCommerce

# Install pre-release
dotnet add package Marketring.BigCommerce --version 1.2.0-preview.1 --prerelease
```

## Version Strategy

### Semantic Versioning
- **Major.Minor.Patch** (e.g., 1.2.0)
- **Major.Minor.Patch-Prerelease** (e.g., 1.2.0-preview.1)

### Tag Naming Convention
- **Production**: `marketring-v1.2.0`
- **Pre-release**: `marketring-v1.2.0-preview.1`
- **Beta**: `marketring-v1.2.0-beta.1`

### Version Bumping Guidelines
- **Major**: Breaking changes to Cart API or core functionality
- **Minor**: New Cart API features, new operations, backwards-compatible changes
- **Patch**: Bug fixes, performance improvements, documentation updates

## Package Comparison

| Feature | Fusionary.BigCommerce | Marketring.BigCommerce |
|---------|---------------------|----------------------|
| **Core API** | ✅ | ✅ |
| **Cart API** | ❌ | ✅ |
| **Package Source** | NuGet.org / GitHub (fusionary) | GitHub (1-800-Battery) |
| **Framework** | .NET 8.0 | .NET 8.0 |
| **Test Framework** | NUnit | NUnit |
| **C# Version** | C# 12 | C# 12 |

## Development Workflow

### Local Testing
```bash
# Build and test locally
dotnet build
dotnet test

# Create local package for testing
./build-marketring.sh
```

### Publishing Steps
1. **Development** → Test changes locally
2. **Staging** → Create preview release (`marketring-v1.2.0-preview.1`)
3. **Production** → Create stable release (`marketring-v1.2.0`)

### Files to Exclude from Upstream PRs
When contributing back to upstream, exclude these files:
- `.github/workflows/marketring-package.yml`
- `NuGet.Marketring.Config`
- `PACKAGE_MANAGEMENT.md`
- `build-marketring.sh`
- `BUILD_INSTRUCTIONS.md`

## Usage Example

### In Your Project

```csharp
// Same API as Fusionary.BigCommerce, plus Cart functionality
var config = new BigCommerceConfig
{
    StoreHash = "your-store-hash",
    AccessToken = "your-access-token"
};

var api = BigCommerceClient.Create(config);

// Standard API usage
var products = await api.Products().List().SendAsync();

// Enhanced Cart API (Marketring.BigCommerce only)
var cart = await api.Carts()
    .Cart()
    .Create()
    .SendAsync(new BcCartPost
    {
        LineItems = new[] { new BcLineItemPost { ProductId = 123, Quantity = 1 } }
    });
```

### Project File Configuration

```xml
<Project Sdk="Microsoft.NET.Sdk">
  <PropertyGroup>
    <TargetFramework>net8.0</TargetFramework>
  </PropertyGroup>
  
  <ItemGroup>
    <PackageReference Include="Marketring.BigCommerce" Version="1.2.0" />
  </ItemGroup>
</Project>
```

## Troubleshooting

### Common Issues

1. **Authentication Failed**
   - Verify GitHub Personal Access Token has `read:packages` scope
   - Check token is correctly configured in NuGet.Config

2. **Package Not Found**
   - Ensure package source is correctly configured
   - Verify package version exists in GitHub Packages

3. **Version Conflicts**
   - Use `--force` flag: `dotnet add package Marketring.BigCommerce --version 1.2.0 --force`
   - Clear NuGet cache: `dotnet nuget locals all --clear`

### Support
- **Issues**: Create issue in this repository
- **Package Registry**: Check https://github.com/orgs/1-800-Battery/packages
- **Workflow Status**: Monitor GitHub Actions for publishing status