# BigCommerce Cart API Usage Examples

This document provides usage examples for the Cart API functionality integrated into the Fusionary.BigCommerceApi library.

## Overview

The Cart API provides operations for managing shopping carts in BigCommerce stores, including creating carts, adding/updating/deleting line items, and updating customer information.

## Available Operations

### Cart Management
- `BcApiCartCreate` - Create a new cart
- `BcApiCartGet` - Retrieve cart details
- `BcApiCartDelete` - Delete a cart
- `BcApiCartUpdateCustomer` - Update cart customer information

### Line Item Management
- `BcApiCartLineAdd` - Add items to cart
- `BcApiCartUpdateLine` - Update existing line items
- `BcApiCartDeleteItem` - Remove items from cart

## Usage Examples

### Initialize the API Client

```csharp
var config = new BigCommerceConfig
{
    StoreHash = "your-store-hash",
    AccessToken = "your-access-token"
};

var api = BigCommerceClient.Create(config);
```

### Create a New Cart

```csharp
// Create a cart with a product
var cartPost = new BcCartPost
{
    LineItems = new[]
    {
        new BcLineItemPost
        {
            ProductId = 123,
            Quantity = 2
        }
    }
};

var createResult = await api.Carts()
    .Cart()
    .Create()
    .SendAsync(cartPost);

if (createResult.Success)
{
    var cart = createResult.Data;
    Console.WriteLine($"Cart created with ID: {cart.Id}");
}
```

### Get Cart Details

```csharp
var cartId = "abc123";
var getResult = await api.Carts()
    .Cart()
    .Get()
    .SendAsync(cartId);

if (getResult.Success)
{
    var cart = getResult.Data;
    Console.WriteLine($"Cart has {cart.LineItems.Count} items");
}
```

### Add Items to Cart

```csharp
var cartId = "abc123";
var lineItem = new BcLineItemPost
{
    ProductId = 456,
    Quantity = 1,
    VariantId = 789 // Optional
};

var addResult = await api.Carts()
    .Cart()
    .AddLineItem()
    .SendAsync(cartId, lineItem);

if (addResult.Success)
{
    Console.WriteLine("Item added to cart successfully");
}
```

### Update Line Item Quantity

```csharp
var cartId = "abc123";
var lineItemId = "def456";
var updateData = new { quantity = 5 };

var updateResult = await api.Carts()
    .Cart()
    .UpdateLineItem()
    .SendAsync(cartId, lineItemId, updateData);

if (updateResult.Success)
{
    Console.WriteLine("Line item updated successfully");
}
```

### Remove Item from Cart

```csharp
var cartId = "abc123";
var lineItemId = "def456";

var deleteResult = await api.Carts()
    .Cart()
    .DeleteLineItem()
    .SendAsync(cartId, lineItemId);

if (deleteResult.Success)
{
    if (deleteResult.StatusCode == HttpStatusCode.NoContent)
    {
        Console.WriteLine("Cart is now empty");
    }
    else
    {
        Console.WriteLine("Item removed from cart");
    }
}
```

### Update Cart Customer Information

```csharp
var cartId = "abc123";
var cartUpdate = new BcCartPost
{
    CustomerId = 12345,
    CustomerMessage = "Please gift wrap this order"
};

var updateResult = await api.Carts()
    .Cart()
    .UpdateCustomer()
    .SendAsync(cartId, cartUpdate);

if (updateResult.Success)
{
    Console.WriteLine("Cart customer information updated");
}
```

### Delete Cart

```csharp
var cartId = "abc123";

var deleteResult = await api.Carts()
    .Cart()
    .Delete()
    .SendAsync(cartId);

if (deleteResult.Success)
{
    Console.WriteLine("Cart deleted successfully");
}
```

## Error Handling

All operations return a `BcResult` or `BcResultData<T>` object with success/failure information:

```csharp
var result = await api.Carts().Cart().Get().SendAsync(cartId);

if (!result.Success)
{
    Console.WriteLine($"Error: {result.Error?.Title}");
    Console.WriteLine($"Status: {result.StatusCode}");
}
```

## Request Filtering and Options

You can add query parameters and custom headers using the fluent API:

```csharp
var result = await api.Carts()
    .Cart()
    .Get()
    .Add("include", "line_items.physical_items.options")
    .WithHeader("X-Custom-Header", "value")
    .SendAsync(cartId);
```

## Rate Limiting

The API client automatically includes rate limit information in responses:

```csharp
var result = await api.Carts().Cart().Get().SendAsync(cartId);
Console.WriteLine($"Requests remaining: {result.RateLimits.RequestsLeft}");
Console.WriteLine($"Reset time: {result.RateLimits.TimeResetMs}ms");
```