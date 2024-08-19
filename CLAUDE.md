# CLAUDE.md

This file provides guidance to Claude Code (claude.ai/code) when working with code in this repository.

## Project Overview

This is Marketring.BigCommerceApi (formerly Fusionary.BigCommerceApi) - a .NET 8.0 library that provides a fluent, strongly-typed interface for the BigCommerce REST API. The library supports catalog operations (products, brands, categories), management operations (customers, orders, price lists), **Cart API operations**, and storefront GraphQL integration.

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

## Test Safety Practices

### Order Test Cleanup
**IMPORTANT**: Order tests create real orders in the BigCommerce store that could be accidentally fulfilled. All order creation tests MUST implement cleanup and identification:

```csharp
public class OrderTests : BcTestBase
{
    private readonly List<int> _createdOrderIds = new();
    private IBcApi _bcApi = null!;

    [SetUp]
    public void SetUp() => _bcApi = Services.GetRequiredService<IBcApi>();

    [TearDown]
    public async Task TearDown()
    {
        // Clean up any orders created during tests
        foreach (var orderId in _createdOrderIds)
        {
            try
            {
                await _bcApi.Orders().Order().Update().SendAsync(orderId, 
                    new BcOrderPut { StatusId = BcOrderStatus.Cancelled }, 
                    CancellationToken.None);
            }
            catch (Exception ex)
            {
                Console.WriteLine($"Failed to cancel order {orderId}: {ex.Message}");
            }
        }
        _createdOrderIds.Clear();
    }

    private void TrackOrderForCleanup(int orderId)
    {
        _createdOrderIds.Add(orderId);
        Console.WriteLine($"📝 Tracking order {orderId} for cleanup");
    }
}
```

**Test Order Identification**:
All test orders MUST be clearly identified to prevent accidental fulfillment:
```csharp
var testOrder = new BcOrderPost
{
    StaffNotes = $"AUTOMATED TEST ORDER - Created by test suite at {DateTime.UtcNow:yyyy-MM-dd HH:mm:ss} UTC - DO NOT SHIP",
    ExternalSource = "NUnit Test Suite",
    ExternalOrderId = $"TEST-{Guid.NewGuid().ToString("N")[..8]}",
    // ... other order properties
};
```

**Safe Order Status Options for Cleanup**:
- `BcOrderStatus.Cancelled = 5` (Recommended for test cleanup)
- `BcOrderStatus.Completed = 10` (For testing completion workflows)
- `BcOrderStatus.Declined = 6` (For testing decline workflows)

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

3. **Cart Operations** (`BcApiCart`) - ✅ **Fully Integrated**
   - Cart CRUD operations (Create, Read, Update, Delete)
   - Line item management (Add, Update, Delete items)
   - Customer association with carts
   - **Location**: Available through `.Carts()` extension method on `IBcApi`

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
  - **`Operations/Cart/`** - Cart operations (Create, Get, Update, Delete, Line Items)
  - **`Operations/Webhooks/`** - Webhook management operations
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

**Customer Group Test Issues**: Customer group tests fail with "Error reading response" during JSON deserialization. This appears to be due to the store not having customer groups configured or discount rules that match the expected API response format.

**TODO - Customer Groups**: 
- Investigate if customer groups need to be created in the BigCommerce admin
- Check if discount rules need to be configured before testing
- All customer group tests are marked as `[Explicit]` until resolved
- Tests expect specific group/product IDs that may not exist in this store

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
- `.Carts()` - Cart operations
- `.Webhooks()` - Webhook operations

## Common Operation Patterns

- **Get Single**: `.Get().SendAsync(id)`
- **Get All/Search**: `.GetAll()` or `.Search()` with fluent filters
- **Create**: `.Create().SendAsync(model)`
- **Update**: `.Update().SendAsync(id, model)`
- **Delete**: `.Delete().SendAsync(id)`

All operations return `BcResult<TData, TMeta>` for consistent error handling and response parsing.

## Cart API Usage

**Current Status**: Cart API functionality is fully integrated and available.

### Cart API Operations:
- **`BcApiCartCreate`** - Create new carts with line items
- **`BcApiCartGet`** - Retrieve cart details and contents
- **`BcApiCartDelete`** - Delete carts
- **`BcApiCartUpdateCustomer`** - Update customer information on carts
- **`BcApiCartLineAdd`** - Add items to existing carts
- **`BcApiCartUpdateLine`** - Update line item quantities and options
- **`BcApiCartLineDelete`** - Remove items from carts

### Cart API Usage Pattern:
```csharp
// Access cart operations through the .Carts() extension method
var result = await _bcApi
    .Carts()
    .Cart()
    .Create()
    .SendAsync(cartData, cancellationToken);
```

### Common Cart Operations:
- **Create Cart**: `.Carts().Cart().Create().SendAsync(cartPost)`
- **Get Cart**: `.Carts().Cart().Get().SendAsync(cartId)`
- **Add Line Item**: `.Carts().Cart().AddLineItem().SendAsync(cartId, lineItem)`
- **Update Line Item**: `.Carts().Cart().UpdateLineItem().SendAsync(cartId, lineItemId, updateData)`
- **Delete Line Item**: `.Carts().Cart().DeleteLineItem().SendAsync(cartId, lineItemId)`
- **Delete Cart**: `.Carts().Cart().Delete().SendAsync(cartId)`

See `CART_API_USAGE.md` for detailed usage examples.