namespace Fusionary.BigCommerce.Tests;

public class OrderTests : BcTestBase
{
    private readonly List<int> _createdOrderIds = new();
    private IBcApi _bcApi = null!;

    [SetUp]
    public void SetUp()
    {
        _bcApi = Services.GetRequiredService<IBcApi>();
    }

    [TearDown]
    public async Task TearDownAsync()
    {
        // Clean up any orders created during tests
        foreach (var orderId in _createdOrderIds)
        {
            try
            {
                await _bcApi.Orders().Order().Update().SendAsync(orderId, new BcOrderPut 
                { 
                    StatusId = BcOrderStatus.Cancelled 
                }, CancellationToken.None);
                Console.WriteLine($"Cancelled test order {orderId}");
            }
            catch (Exception ex)
            {
                // Log but don't fail the test cleanup
                Console.WriteLine($"Failed to cancel order {orderId}: {ex.Message}");
            }
        }
        _createdOrderIds.Clear();
        
        // Call the base TearDown method
        base.TearDown();
    }

    /// <summary>
    /// Helper method to track created orders for automatic cleanup
    /// </summary>
    private void TrackOrderForCleanup(int orderId)
    {
        _createdOrderIds.Add(orderId);
        Console.WriteLine($"Tracking order {orderId} for cleanup");
    }
    [Test]
    public async Task Can_Create_Order_Metafields_Async()
    {
        var orderMetafieldsApi = Services.GetRequiredService<BcApiOrderMetafields>();

        var cancellationToken = CancellationToken.None;

        var result = await orderMetafieldsApi
            .Create()
            .SendAsync(
                100,
                BcPermissionSet.Read,
                Faker.Hacker.Noun(),
                [
                    new BcMetafieldItem { Key = Faker.Hacker.Noun(), Value = Faker.Hacker.Phrase() },
                    new BcMetafieldItem { Key = Faker.Hacker.Noun(), Value = Faker.Hacker.Phrase() }
                ],
                cancellationToken
            );

        DumpObject(result);

        Assert.Pass();
    }

    [Test]
    [Explicit("Requires valid order ID, address ID, and product ID which are not available in CI")]
    public async Task Can_Create_Order_Shipment_Async()
    {
        var bc = Services.GetRequiredService<IBcApi>();

        var cancellationToken = CancellationToken.None;

        var result = await bc
            .Orders()
            .OrderShipments()
            .Create()
            .SendAsync(
                108,
                new BcOrderShipmentsPost
                {
                    TrackingNumber = Faker.Random.AlphaNumeric(10),
                    ShippingProvider = "shipperhq",
                    OrderAddressId = 9,
                    Items = new List<BcOrderShipmentsItem> { new() { OrderProductId = 9, Quantity = 1 } }
                },
                cancellationToken
            );

        DumpObject(result);

        result.Success.Should().BeTrue();

        Assert.Pass();
    }

    [Test]
    public async Task Can_Create_Sample_Order_Async()
    {
        var createOrdersApi = Services.GetRequiredService<BcApiOrdersCreate>();

        var cancellationToken = CancellationToken.None;

        var newOrder = new BcOrderPost
        {
            BillingAddress = new BcBillingAddressBase
            {
                FirstName = Faker.Name.FirstName(),
                LastName = Faker.Name.LastName(),
                Street1 = Faker.Address.StreetAddress(),
                City = Faker.Address.City(),
                State = Faker.Address.State(),
                Zip = Faker.Address.ZipCode(),
                CountryIso2 = "US",
                Email = Faker.Internet.Email(),
                Phone = Faker.Phone.PhoneNumber(),
                Company = Faker.Company.CompanyName()
            },
            Products = new List<BcOrderCatalogProductPost>
            {
                new() { Quantity = Faker.Random.Int(1, 3), ProductId = 114 } // Changed from 0-10 to 1-3 to avoid zero quantities
            },
            StaffNotes = $"AUTOMATED TEST ORDER - Created by test suite at {DateTime.UtcNow:yyyy-MM-dd HH:mm:ss} UTC - DO NOT SHIP",
            ExternalSource = "NUnit Test Suite",
            ExternalOrderId = $"TEST-{Guid.NewGuid().ToString("N")[..8]}"
        };

        var result = await createOrdersApi.SendAsync(newOrder, cancellationToken);

        result.Success.Should().BeTrue();

        // Track the created order for cleanup
        if (result.Success && result.Data?.Id != null)
        {
            TrackOrderForCleanup(result.Data.Id);
        }
    }

    [Test]
    public async Task Can_Update_Sample_Order_Async()
    {
        var bc = Services.GetRequiredService<IBcApi>();

        var cancellationToken = CancellationToken.None;

        var orderToUpdate = new BcOrderPut
        {
            StatusId = BcOrderStatus.Completed
        };

        var result = await bc.Management().Orders().Order().Update().SendAsync<BcOrderResponseFull>(150, orderToUpdate, cancellationToken);

        DumpObject(result);

        Assert.That(result, Is.Not.Null);
        Assert.That(result.Success, Is.True);
    }

    #region Cart Test Helpers
    
    private async Task<string> CreateTestCartAsync(IBcApi bc, CancellationToken cancellationToken, params (int productId, int quantity)[] products)
    {
        var lineItems = products.Length > 0 
            ? products.Select(p => new BcCartLineItem { ProductId = p.productId, Quantity = p.quantity }).ToList()
            : new List<BcCartLineItem> { new() { ProductId = 23376, Quantity = 1 } }; // Default product

        var cartCreate = new BcCartPost
        {
            ChannelId = 1,
            LineItems = lineItems
        };

        var cartResult = await bc.Carts().Cart().Create().SendAsync(cartCreate, cancellationToken);
        Assert.That(cartResult.Success, Is.True, $"Failed to create cart: {cartResult.Error}");
        Assert.That(cartResult.HasData, Is.True);
        
        TestContext.WriteLine($"Created test cart with ID: {cartResult.Data.Id}");
        return cartResult.Data.Id!;
    }

    private static async Task CleanupTestCartAsync(IBcApi bc, string? cartId, CancellationToken cancellationToken)
    {
        if (string.IsNullOrEmpty(cartId)) return;

        try
        {
            var deleteResult = await bc.Carts().Cart().Delete().SendAsync(cartId, cancellationToken);
            if (deleteResult.Success)
            {
                TestContext.WriteLine($"Cleaned up cart: {cartId}");
            }
            else
            {
                TestContext.WriteLine($"Failed to delete cart {cartId}: {deleteResult.Error}");
            }
        }
        catch (Exception ex)
        {
            TestContext.WriteLine($"Cleanup error for cart {cartId}: {ex.Message}");
        }
    }

    private async Task<T> WithTestCartAsync<T>(IBcApi bc, Func<string, Task<T>> testAction, CancellationToken cancellationToken, params (int productId, int quantity)[] products)
    {
        string? cartId = null;
        try
        {
            cartId = await CreateTestCartAsync(bc, cancellationToken, products);
            return await testAction(cartId);
        }
        finally
        {
            await CleanupTestCartAsync(bc, cartId, cancellationToken);
        }
    }
    
    #endregion

    [Test]
    public async Task Can_Get_Cart_Async()
    {
        var bc = Services.GetRequiredService<IBcApi>();
        var cancellationToken = CancellationToken.None;

        await WithTestCartAsync(bc, async cartId =>
        {
            // Test getting the cart
            var result = await bc.Carts().Cart().Get().SendAsync(cartId, cancellationToken);

            DumpObject(result);

            Assert.That(result, Is.Not.Null);
            Assert.That(result.Success, Is.True);
            Assert.That(result.HasData, Is.True);
            Assert.That(result.Data.Id, Is.EqualTo(cartId));
            
            return result;
        }, cancellationToken);
    }

    [Test]
    public async Task Can_Get_CartRedirects_Async()
    {
        var bc = Services.GetRequiredService<IBcApi>();
        var cancellationToken = CancellationToken.None;

        await WithTestCartAsync(bc, async cartId =>
        {
            // Test getting cart redirects
            var queryParams = new BcCartRedirectQueryParms
            {
                QueryParameters = new BcCartRedirectQueryParms.QueryParams
                {
                    Key1 = "test_param_1",
                    Key2 = "test_param_2"
                }
            };

            var result = await bc
                .Carts()
                .Cart().GetCartRedirects().SendAsync(cartId, queryParams, cancellationToken);

            DumpObject(result);

            Assert.That(result, Is.Not.Null);
            Assert.That(result.Success, Is.True);
            
            if (result.HasData)
            {
                Assert.That(result.Data.CartUrl, Is.Not.Null.Or.Empty, "Cart URL should be populated");
                Assert.That(result.Data.CheckoutUrl, Is.Not.Null.Or.Empty, "Checkout URL should be populated");
                Assert.That(result.Data.EmbeddedCheckoutUrl, Is.Not.Null.Or.Empty, "Embedded checkout URL should be populated");
                
                TestContext.WriteLine($"Cart URL: {result.Data.CartUrl}");
                TestContext.WriteLine($"Checkout URL: {result.Data.CheckoutUrl}");
                TestContext.WriteLine($"Embedded Checkout URL: {result.Data.EmbeddedCheckoutUrl}");
            }
            
            return result;
        }, cancellationToken);
    }

    [Test]
    public async Task Can_Delete_Cart_Line_Async()
    {
        var bc = Services.GetRequiredService<IBcApi>();
        var cancellationToken = CancellationToken.None;

        // Create cart with two products
        var products = new[]
        {
            (productId: 23376, quantity: 1), // 75PVP
            (productId: 23379, quantity: 2)  // 85PVP
        };

        await WithTestCartAsync(bc, async cartId =>
        {
            // Verify cart has 2 items
            var getResult = await bc.Carts().Cart().Get().SendAsync(cartId, cancellationToken);
            Assert.That(getResult.Data.LineItems?.PhysicalItems?.Count, Is.EqualTo(2));
            
            // Get the first line item ID to delete
            var lineItemToDelete = getResult.Data.LineItems!.PhysicalItems![0].Id;

            // Delete the line item
            var result = await bc
                .Carts()
                .Cart().DeleteLineItem().SendAsync(cartId, lineItemToDelete!, cancellationToken);

            DumpObject(result);
            
            Assert.That(result.Success, Is.True);
            
            // Verify cart now has 1 item
            var verifyResult = await bc.Carts().Cart().Get().SendAsync(cartId, cancellationToken);
            Assert.That(verifyResult.Data.LineItems?.PhysicalItems?.Count, Is.EqualTo(1));
            
            TestContext.WriteLine($"Successfully deleted line item from cart");
            
            return result;
        }, cancellationToken, products);
    }

    [Test]
    public async Task Can_Get_All_Orders_Async()
    {
        var bc = Services.GetRequiredService<IBcApi>();

        var cancellationToken = CancellationToken.None;

        var result = await bc
            .Orders()
            .Order()
            .Search()
            .Limit(1)
            .MinDateCreated(DateTime.Today.AddDays(-14))
            .Sort(BcOrderSort.DateCreated)
            .SendAsync(cancellationToken);

        DumpObject(result);

        result.Success.Should().BeTrue();

        if (result.HasData)
        {
            foreach (var order in result.Data)
            {
                DumpObject(order);
            }
        }

        Assert.Pass();
    }

    [Test]
    public async Task Can_Get_Order_Metafields_Async()
    {
        var bc = Services.GetRequiredService<IBcApi>();

        var cancellationToken = CancellationToken.None;

        var result = await bc
            .Orders()
            .OrderMetafields()
            .GetAll()
            .Limit(10)
            .SendAsync(100, cancellationToken);

        if (result)
        {
            foreach (var metafield in result.Data)
            {
                DumpObject(metafield);

                await bc.Orders().OrderMetafields().Delete().SendAsync(100, metafield.Id, cancellationToken);
            }
        }

        Assert.Pass();
    }

    [Test]
    public async Task Can_Get_Order_Shipping_Async()
    {
        var bc = Services.GetRequiredService<IBcApi>();

        var cancellationToken = CancellationToken.None;

        var result = await bc
            .Orders()
            .OrderShipping()
            .Get()
            .SendAsync(106, cancellationToken);

        DumpObject(result);

        result.Success.Should().BeTrue();

        Assert.Pass();
    }

    [Test]
    public async Task Can_Get_Order_With_Consignments_Async()
    {
        var bc = Services.GetRequiredService<IBcApi>();

        var cancellationToken = CancellationToken.None;

        var result = await bc
            .Orders()
            .Order()
            .GetWithConsignments()
            .SendAsync(106, cancellationToken);

        DumpObject(result);

        result.Success.Should().BeTrue();

        Assert.Pass();
    }

    [Test]
    public void Can_SerializeOrder()
    {
        var order = new BcOrderPost
        {
            BillingAddress =
                new BcBillingAddressBase { Company = Faker.Company.CompanyName(), Zip = Faker.Address.ZipCode() },
            Products = new List<BcOrderCatalogProductPost>
            {
                new() { ProductId = 1, Quantity = 1, PriceExTax = 5.00m },
                new()
                {
                    ProductId = 2,
                    Quantity = 2,
                    ProductOptions = new List<BcProductOptions> { new() { Id = 27, Value = "Red" } }
                }
            }
        };

        DumpObject(order);

        Assert.Pass();
    }
}
