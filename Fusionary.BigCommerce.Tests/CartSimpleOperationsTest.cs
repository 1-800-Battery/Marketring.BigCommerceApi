using FluentAssertions;
using Microsoft.Extensions.DependencyInjection;

namespace Fusionary.BigCommerce.Tests;

public class CartSimpleOperationsTest : BcTestBase
{
    private string? _createdCartId;

    [Test]
    public async Task Cart_Simple_Operations_Full_Lifecycle_Async()
    {
        var bc = Services.GetRequiredService<IBcApi>();
        var cancellationToken = CancellationToken.None;

        try
        {
            var simpleProductId = 23376; // 75PVP - validated product
            var secondProductId = 23379; // 85PVP - validated product

            // Create cart with simple product
            _createdCartId = await CreateCartWithSimpleProductAsync(bc, simpleProductId, cancellationToken);
            TestContext.WriteLine($"Created cart: {_createdCartId}");

            // Verify cart creation
            await VerifyCartCreationAsync(bc, _createdCartId, simpleProductId, cancellationToken);
            TestContext.WriteLine($"Verified cart creation with product {simpleProductId}");

            // Add second product
            await AddSecondSimpleProductAsync(bc, _createdCartId, secondProductId, cancellationToken);
            TestContext.WriteLine($"Added second product to cart");

            // Update quantity of first product
            await UpdateProductQuantityAsync(bc, _createdCartId, cancellationToken);
            TestContext.WriteLine($"Updated product quantity");

            // Delete one line item
            await DeleteLineItemAsync(bc, _createdCartId, cancellationToken);
            TestContext.WriteLine($"Deleted line item");

            // Verify final cart state
            await VerifyFinalCartStateAsync(bc, _createdCartId, cancellationToken);
            TestContext.WriteLine($"Verified final cart state");

            Assert.Pass("Simple cart operations completed successfully");
        }
        finally
        {
            await CleanupCartAsync(bc, cancellationToken);
        }
    }

    [Test]
    public async Task Cart_With_Product_Options_Test_Async()
    {
        var bc = Services.GetRequiredService<IBcApi>();
        var cancellationToken = CancellationToken.None;

        try
        {
            var productWithOptionsId = 114; // ALK-9VC - validated product with options

            // Get product details to understand its options
            var productResult = await bc.Catalog().Product().Get().SendAsync(productWithOptionsId, cancellationToken);
            if (!productResult.Success || !productResult.HasData)
            {
                Assert.Inconclusive($"Product {productWithOptionsId} not found for options testing");
                return;
            }

            TestContext.WriteLine($"Found product: {productResult.Data.Name} (ID: {productWithOptionsId})");

            // Create cart with product
            _createdCartId = await CreateCartWithSimpleProductAsync(bc, productWithOptionsId, cancellationToken);
            TestContext.WriteLine($"Created cart with product {productWithOptionsId}: {_createdCartId}");

            // Verify cart creation
            await VerifyCartCreationAsync(bc, _createdCartId, productWithOptionsId, cancellationToken);
            TestContext.WriteLine($"Verified cart creation with product {productWithOptionsId}");

            // Add a simple product alongside the options product
            await AddSecondSimpleProductAsync(bc, _createdCartId, 23376, cancellationToken);
            TestContext.WriteLine($"Added simple product to cart with options product");

            // Verify cart has 2 items
            var getResult = await bc.Carts().Cart().Get().SendAsync(_createdCartId, cancellationToken);
            getResult.Success.Should().BeTrue();
            getResult.Data.LineItems?.PhysicalItems?.Count.Should().Be(2);
            TestContext.WriteLine($"Verified cart has 2 items");

            Assert.Pass("Cart with product options test completed successfully");
        }
        finally
        {
            await CleanupCartAsync(bc, cancellationToken);
        }
    }

    private async Task<string> CreateCartWithSimpleProductAsync(IBcApi bc, int productId, CancellationToken cancellationToken)
    {
        var cartCreate = new BcCartPost
        {
            ChannelId = 1,
            LineItems = new List<BcCartLineItem>
            {
                new()
                {
                    ProductId = productId,
                    Quantity = 1
                }
            }
        };

        var cartResult = await bc.Carts().Cart().Create().SendAsync(cartCreate, cancellationToken);
        if (!cartResult.Success || !cartResult.HasData)
        {
            throw new Exception($"Failed to create cart: {cartResult.Error}");
        }

        return cartResult.Data.Id!;
    }

    private static async Task VerifyCartCreationAsync(IBcApi bc, string cartId, int expectedProductId, CancellationToken cancellationToken)
    {
        var getResult = await bc.Carts().Cart().Get().SendAsync(cartId, cancellationToken);
        
        getResult.Success.Should().BeTrue();
        getResult.HasData.Should().BeTrue();
        getResult.Data.LineItems?.PhysicalItems?.Count.Should().Be(1);
        
        var lineItem = getResult.Data.LineItems!.PhysicalItems![0];
        lineItem.ProductId.Should().Be(expectedProductId);
        lineItem.Quantity.Should().Be(1);
        lineItem.Options?.Count.Should().Be(0, "Simple product should have no options");
    }

    private async Task AddSecondSimpleProductAsync(IBcApi bc, string cartId, int productId, CancellationToken cancellationToken)
    {
        var lineItems = new BcCartLineItems
        {
            LineItems = new List<BcCartLineItem>
            {
                new()
                {
                    ProductId = productId,
                    Quantity = 2
                }
            }
        };

        var addResult = await bc.Carts().Cart().AddLineItem().SendAsync(cartId, lineItems, cancellationToken);
        addResult.Success.Should().BeTrue($"Failed to add line item: {addResult.Error}");

        // Verify cart now has 2 items
        var verifyResult = await bc.Carts().Cart().Get().SendAsync(cartId, cancellationToken);
        verifyResult.Data.LineItems!.PhysicalItems!.Count.Should().Be(2);
    }

    private async Task UpdateProductQuantityAsync(IBcApi bc, string cartId, CancellationToken cancellationToken)
    {
        // Get first line item
        var getResult = await bc.Carts().Cart().Get().SendAsync(cartId, cancellationToken);
        var firstLineItem = getResult.Data.LineItems!.PhysicalItems![0];
        
        // Create update structure as required by BigCommerce API
        var lineItem = new BcCartLineItem
        {
            Quantity = 3,
            ProductId = firstLineItem.ProductId
        };

        var updateData = new { line_item = lineItem };
        
        var updateResult = await bc.Carts().Cart().UpdateLineItem()
            .SendAsync(cartId, firstLineItem.Id!, updateData, cancellationToken);
        
        updateResult.Success.Should().BeTrue($"Update failed: {updateResult.Error}");

        // Verify the update
        var verifyResult = await bc.Carts().Cart().Get().SendAsync(cartId, cancellationToken);
        verifyResult.Success.Should().BeTrue();
        verifyResult.HasData.Should().BeTrue();
        
        var updatedItem = verifyResult.Data.LineItems!.PhysicalItems!
            .First(item => item.Id == firstLineItem.Id);
        updatedItem.Quantity.Should().Be(3);
    }

    private static async Task DeleteLineItemAsync(IBcApi bc, string cartId, CancellationToken cancellationToken)
    {
        // Get second line item to delete
        var getResult = await bc.Carts().Cart().Get().SendAsync(cartId, cancellationToken);
        var secondLineItem = getResult.Data.LineItems!.PhysicalItems![1];
        
        // Delete it
        var deleteResult = await bc.Carts().Cart().DeleteLineItem()
            .SendAsync(cartId, secondLineItem.Id!, cancellationToken);
        deleteResult.Success.Should().BeTrue();

        // Verify deletion
        var verifyResult = await bc.Carts().Cart().Get().SendAsync(cartId, cancellationToken);
        verifyResult.Data.LineItems!.PhysicalItems!.Count.Should().Be(1);
    }

    private static async Task VerifyFinalCartStateAsync(IBcApi bc, string cartId, CancellationToken cancellationToken)
    {
        var getResult = await bc.Carts().Cart().Get().SendAsync(cartId, cancellationToken);
        
        getResult.Success.Should().BeTrue();
        getResult.Data.LineItems!.PhysicalItems!.Count.Should().Be(1);
        
        var remainingItem = getResult.Data.LineItems.PhysicalItems[0];
        remainingItem.Quantity.Should().Be(3);
        
        TestContext.WriteLine($"Final cart contains 1 item with quantity {remainingItem.Quantity}");
    }

    private async Task CleanupCartAsync(IBcApi bc, CancellationToken cancellationToken)
    {
        try
        {
            if (!string.IsNullOrEmpty(_createdCartId))
            {
                var deleteCartResult = await bc.Carts().Cart().Delete().SendAsync(_createdCartId, cancellationToken);
                if (deleteCartResult.Success)
                {
                    TestContext.WriteLine($"Cleaned up cart: {_createdCartId}");
                }
                else
                {
                    TestContext.WriteLine($"Failed to delete cart {_createdCartId}: {deleteCartResult.Error}");
                }
            }
        }
        catch (Exception ex)
        {
            TestContext.WriteLine($"Cleanup error: {ex.Message}");
        }
    }
}