# Marketring.BigCommerceApi

BigCommerce API client for .NET 8, derived from Fusionary.BigCommerceApi.

Version 1.3.0 adds typed order transaction diagnostics and refund history reads. See [CHANGELOG.md](CHANGELOG.md) for release and compatibility notes.

```sh
dotnet add package Marketring.BigCommerce --version 1.3.0 --source https://nuget.pkg.github.com/1-800-Battery/index.json
```

## Installation

Add env variables to your project:

```
dotnet user-secrets set "BigCommerce:StoreHash" "12345"
dotnet user-secrets set "BigCommerce:AccessToken" "1845633E-S3CR3TS-8364B7FBE3A2"
dotnet user-secrets set "BigCommerce:StorefrontUrl" "https://mystore.bigcommerce.com"
dotnet user-secrets set "BigCommerce:StorefrontChannelId" "1"
dotnet user-secrets set "BigCommerce:StorefrontAccessToken" "1845633E-S3CR3TS-8364B7FBE3A2"
```

or in appSettings.json:

```json
{
  "BigCommerce": {
    "Host": "https://api.bigcommerce.com",
    "StoreHash": "12345",
    "AccessToken": "1845633E-S3CR3TS-8364B7FBE3A2",
    "StorefrontUrl": "https://mystore.bigcommerce.com",
    "StorefrontChannelId": "1",
    "StorefrontAccessToken": "1845633E-S3CR3TS-8364B7FBE3A2"
  }
}
```

**Storefront** config values are only needed for Storefront GraphQL APIs.

-----

Register BigCommerceApi in your Startup.cs:

```csharp
services.AddBigCommerce(configuration);
```

## Usage

```csharp
public class BigCommerceProductDemo {

    private readonly IBcApi _bcApi;

    public BigCommerceProductDemo(IBcApi bcApi) {
        _bcApi = bcApi;
    }

    public async Task DisplayFiveProductsAsync(CancellationToken cancellationToken = default)
    {
        var response = await _bcApi
            .Products()
            .Search()
            .Availability(BcAvailability.Available)
            .Include(BcProductInclude.Variants, BcProductInclude.Images, BcProductInclude.CustomFields)
            .Limit(5)
            .Sort(BcProductSort.Name)
            .SendAsync(cancellationToken);

        if (response.HasError)
        {
            var error = response.Error;

            Console.WriteLine($"Error: {error.Status} | {error.Title} | {error.Type}");

            if (error.HasErrorDetails)
            {
                foreach (var (key, value) in error.ErrorDetails)
                {
                    Console.WriteLine($"{key}: {value}");
                }
            }
            
            return;
        }

        var (products, pagination) = response;

        Console.WriteLine($"Page {pagination.CurrentPage} of {pagination.TotalPages} ({pagination.Total})");

        foreach (var product in products)
        {
            var id    = product.Id;
            var name  = product.Name;
            var price = product.Price;

            Console.WriteLine($"{id} | {name} | {price}");
        }
    }
}
```

```csharp
var config = new BigCommerceConfig {
   StoreHash = "12345",
   AccessToken = "1845633E-S3CR3TS-8364B7FBE3A2",
   // Only needed for Storefront GraphQL API
   ChannelId = 1,
   StorefrontUrl = "https://mystore.bigcommerce.com"
});

IBigCommerceApi apiClient = BigCommerceClient.Create(config);

// Raw API
var response = await apiClient
    .GetAsync(
        "v3/catalog/products", 
        QueryString.Create("limit", "10"), 
        cancellationToken);

// Fluent API
var bc = BcApi.Create(apiClient);

var response = await bc
    .Products()
    .Get()
    .Include(BcProductInclude.Variants, BcProductInclude.Images, BcProductInclude.CustomFields)
    .SendAsync(199, cancellationToken);
    
    
// Storefront GraphQL Client
var graphQL = services.GetRequiredService<IBcStorefrontGraphQL>();

        var request = new GraphQLRequest(@"
query paginateProducts {
  site {
    search {
      searchProducts(
        filters: {
        	productAttributes: [
            {
              attribute: ""Color"", values: ""Red""
            }
          ]
        }
      ) {
        products {
          edges {
            node {
              name
              sku
            }
          }
        }
      }
    }
  }
}
");
        var response = await graphQL
            .SendQueryAsync<dynamic>(request, cancellationToken);
            
        Console.WriteLine(JsonSerializer.Serialize(response.Data));

```

## Reading order payment diagnostics

```csharp
var transactions = await bc.Orders().OrderTransactions().Get()
    .Page(1).Limit(50).SendDiagnosticAsync(orderId, cancellationToken);

var refunds = await bc.Orders().OrderRefunds().Get()
    .SendAsync(orderId, cancellationToken);
```

`SendDiagnosticAsync` returns a `BcResultPaged<BcOrderTransactionDiagnostic>` with decimal amounts, string payment method and gateway IDs, related transaction IDs, and transaction outcome fields. Use `HasNextPage` and `.Page(...)` to request another page. Existing `SendAsync` calls still return `BcOrderTransaction` with the original contract.

The refund GET returns recorded refunds with typed items and payments, including provider ID, declined status, and decline code and message when present. Both reads use nullable decimal amounts and tolerate quoted numeric money and missing values. The SDK omits raw response bodies and unfiltered HTTP error text for these reads. Select the fields you need before logging or exposing results. Payment tokens, card results and provider messages can contain sensitive information.

Current order status is a snapshot and does not establish when a status changed or who changed it. This release adds no order status history, staff or system logs, shipment reads, app or account inventory, or gateway settings. Use the existing `.Webhooks().Get()` and `.Webhooks().GetAll()` operations for webhook reads.

## Publishing new version of the NuGet package

### Automated Release Process

The repository has automated workflows that handle package building, publishing, and GitHub release creation.

#### Option 1: Automated Release via Tag (Recommended)

When you push a tag with the `marketring-` prefix to the `main` branch, the workflow automatically:
1. Builds the NuGet package
2. Publishes to GitHub Packages
3. Creates a GitHub release with release notes
4. Attaches the NuGet package to the release

**Steps:**
1. Find the last tag used in the repository (e.g., `marketring-v1.2.2`)
2. Create and push a new tag with the `marketring-` prefix:
   ```bash
   git tag marketring-v1.3.0
   git push origin marketring-v1.3.0
   ```
3. The workflow will automatically trigger and create the release

**Tag Format:**
- Must start with `marketring-v` prefix
- Follow semantic versioning: `marketring-v1.3.0` or `marketring-v1.3.0-preview.1`
- Examples:
  - `marketring-v1.3.0` (stable release)
  - `marketring-v1.3.0-preview.1` (pre-release)

#### Option 2: Manual Workflow Dispatch

Alternatively, you can manually trigger the workflow:
1. Go to **Actions** → **Build and Publish Marketring.BigCommerce**
2. Click **Run workflow**
3. Enter the version number (e.g., `1.3.0`)
4. Select if it's a pre-release
5. Click **Run workflow**

**Note:** Manual dispatch does not automatically create a GitHub release. Use Option 1 for full automation including release creation.
