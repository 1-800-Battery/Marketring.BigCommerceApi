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

    [Test]
    public async Task Can_Get_Cart_Async()
    {
        var bc = Services.GetRequiredService<IBcApi>();

        var cancellationToken = CancellationToken.None;

        var result = await bc
            .Carts()
            .Cart().Get().SendAsync("472abc00-7343-4e5a-9c31-d4f0276093d9", cancellationToken);

        DumpObject(result);

        Assert.That(result, Is.Not.Null);
        Assert.That(result.Success, Is.True);
    }

    [Test]
    public async Task Can_Get_CartRedirects_Async()
    {
        var bc = Services.GetRequiredService<IBcApi>();

        var cancellationToken = CancellationToken.None;
        var empty = new BcCartRedirectQueryParms();
        var result = await bc
            .Carts()
            .Cart().GetCartRedirects().SendAsync("fe057e11-8e99-4e6b-9b0c-347474af8b20", empty, cancellationToken);

        DumpObject(result);

        Assert.That(result, Is.Not.Null);
        Assert.That(result.Success, Is.True);
    }

    [Test]
    public async Task Can_Delete_Cart_Line_Async()
    {
        var bc = Services.GetRequiredService<IBcApi>();

        var cancellationToken = CancellationToken.None;

        var result = await bc
            .Carts()
            .Cart().DeleteLineItem().SendAsync("472abc00-7343-4e5a-9c31-d4f0276093d9", "794e2dce-ac8c-4e71-b586-978afe423381", cancellationToken);

        DumpObject(result);
         
        Assert.Pass();
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
