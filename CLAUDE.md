# CLAUDE.md

This file provides guidance to Claude Code (claude.ai/code) when working with code in this repository.

## Project Overview

This is Fusionary.BigCommerceApi - a .NET 8.0 library that provides a fluent, strongly-typed interface for the BigCommerce REST API. The library supports catalog operations (products, brands, categories), management operations (customers, orders, price lists), **Cart API operations**, and storefront GraphQL integration.

**Current Branch Status**: `sync-upstream-main` - This branch contains all upstream improvements and is ready for Cart API integration from the fork.

## Important Git Commit Rules

**NEVER include Claude as a co-author in git commits**. This means:
- Do NOT add "Co-Authored-By: Claude <noreply@anthropic.com>" to any commit messages
- Do NOT add any Claude-related authorship or attribution in commits
- Commits should only reflect the actual human contributors

## Development Commands

### Build
```bash
dotnet build
```

### Run Tests
```bash
dotnet test
```

**Important**: Tests are configured to exclude `[Explicit]` tests by default in CI workflows using `-- NUnit.ExplicitIncludes=false`

### Run a Single Test
```bash
dotnet test --filter "TestMethodName"
```

### Run Tests for a Specific Class
```bash
dotnet test --filter "ClassName"
```

### Run Tests Including Explicit Tests
```bash
dotnet test -- NUnit.ExplicitIncludes=true
```

### Package Generation
NuGet packages are automatically generated on build due to `<GeneratePackageOnBuild>true</GeneratePackageOnBuild>` in the project file.

## Core Architecture

### Main API Structure
- **`IBcApi`** - Main API interface providing HTTP operations and request methods
- **`BcApi`** - Main API implementation handling HTTP requests, rate limiting, and response processing
- **`IBigCommerceClient`** - HTTP client abstraction containing configuration and HttpClient
- **`BigCommerceClient`** - HTTP client implementation that configures the underlying HttpClient

### Operation Organization
The API is organized into main operation categories accessible through fluent methods:

1. **Catalog Operations** (`BcApiCatalog`)
   - Products, Brands, Categories, Category Trees
   - Product-related operations (Images, Metafields, Variants, Modifiers, etc.)
   - Summary operations

2. **Management Operations** (`BcApiManagement`) 
   - Customers and Customer Groups
   - Orders (with sub-operations for Products, Shipments, Shipping, Metafields)
   - Price Lists and Assignments
   - Pricing operations

3. **Cart Operations** (`BcApiCart`) - 🚧 **Integration In Progress**
   - **NOTE**: Cart API exists in fork's main branch and needs integration
   - Cart CRUD operations (Create, Read, Update, Delete)
   - Line item management (Add, Update, Delete items)
   - Customer association with carts
   - **Location**: Will be integrated into Management operations structure

4. **Storefront Operations** (`BcApiStorefront`)
   - Token management for storefront API access

5. **Webhooks Operations** (`BcApiWebhooks`)
   - Webhook CRUD operations

### Request Builder Pattern
The library uses a fluent builder pattern for constructing API requests:
- **`BcRequestBuilder`** - Abstract base class for request builders
- **`IBcRequestBuilder`** - Interface providing access to API, Filter, and Options
- **`BcFilter`** - Query string builder using fluent interface
- **`BcRequestOptions`** - Request configuration options

### Configuration
Configuration is handled through `BigCommerceConfig` with the following required fields:
- `StoreHash` - Store identifier
- `AccessToken` - API authentication token

Optional fields for Storefront GraphQL:
- `StorefrontUrl` - Storefront URL
- `StorefrontChannelId` - Channel ID
- `StorefrontAccessToken` - Storefront token

Configuration can be set via user secrets:
```bash
dotnet user-secrets set "BigCommerce:StoreHash" "12345"
dotnet user-secrets set "BigCommerce:AccessToken" "your-token"
```

## Key Directories

- **`Operations/`** - Contains all API operation implementations organized by category
  - **`Operations/Management/`** - Management operations including Orders, Customers, Price Lists
  - **`Operations/Catalog/`** - Product, Brand, Category operations  
  - **`Operations/Webhooks/`** - Webhook management operations
  - **🚧 `Operations/Cart/`** - Cart operations (to be integrated from main branch)
- **`Types/`** - BigCommerce API response and request models
- **`Utils/`** - Utility classes for HTTP handling, JSON serialization, and extensions
- **`Import/`** - CSV import/export functionality for products
- **`B2B/`** - Stub implementation for B2B operations (not fully implemented)

## Test Configuration

Tests require BigCommerce API credentials stored in user secrets. Run tests with:
```bash
dotnet user-secrets set "BigCommerce:StoreHash" "your-store-hash"
dotnet user-secrets set "BigCommerce:AccessToken" "your-access-token"
```

Tests use NUnit framework with FluentAssertions for readable assertions and Bogus for test data generation.

### Known Test Issues

**Product Test Hanging**: The `Can_Get_All_Products_Async` test can cause test runs to hang when the BigCommerce store has a large product catalog (e.g., 1.3M+ products). This test attempts to fetch ALL products page by page, which can take hours or timeout.

**Solution**: 
- The test has been split into two:
  - `Can_Get_First_25_Products_Async` - Default test that only fetches the first page (25 products)
  - `Can_Get_All_Products_Async` - Marked with `[Explicit]` attribute for manual runs only
- CI workflows use `-- NUnit.ExplicitIncludes=false` to skip explicit tests
- This prevents timeout issues in both local development and CI environments

## Code Quality Settings

The project uses comprehensive code quality settings:
- **TreatWarningsAsErrors**: true
- **EnableNETAnalyzers**: true
- **EnforceCodeStyleInBuild**: true
- Uses SonarAnalyzer, Microsoft.CodeAnalysis.NetAnalyzers, and Microsoft.VisualStudio.Threading.Analyzers

## Pattern Examples

### Fluent API Usage
```csharp
var response = await _bcApi
    .Products()
    .Search()
    .Availability(BcAvailability.Available)
    .Include(BcProductInclude.Variants, BcProductInclude.Images)
    .Limit(5)
    .Sort(BcProductSort.Name)
    .SendAsync(cancellationToken);
```

### Operation Extension Methods
Each operation group is accessed through extension methods on `IBcApi`:
- `.Products()` - Product operations
- `.Brands()` - Brand operations
- `.Categories()` - Category operations
- `.Orders()` - Order operations
- `.Customers()` - Customer operations

## Common Operation Patterns

- **Get Single**: `.Get().SendAsync(id)`
- **Get All/Search**: `.GetAll()` or `.Search()` with fluent filters
- **Create**: `.Create().SendAsync(model)`
- **Update**: `.Update().SendAsync(id, model)`
- **Delete**: `.Delete().SendAsync(id)`

All operations return `BcResult<TData, TMeta>` for consistent error handling and response parsing.

## Cart API Integration Notes

**Current Status**: Cart API functionality exists in the fork's main branch and needs integration.

### Cart API Commits to Integrate:
Key commits from main branch containing Cart functionality:
- `cf91329` - Update LineItemQuantity
- `1b25301` - Update Add Line item  
- `119ad0b` - Add Cart Operations to API
- Additional Cart-related commits and types

### Integration Pattern:
Cart API should follow upstream constructor patterns:
```csharp
// Target pattern for Cart operations:
public class BcApiCart(IBcApi api) : IBcApiOperation
{
    public BcApiCartCreate Create() => new(api);
    public BcApiCartGet Get() => new(api);
    public BcApiCartUpdate Update() => new(api);
    public BcApiCartDelete Delete() => new(api);
}
```

### Expected Cart API Surface:
- **Cart Management**: Create, read, update, delete carts
- **Line Items**: Add, update, delete line items
- **Customer Association**: Link carts to customers
- **Integration Point**: Likely through `BcApiManagement.Carts()` method