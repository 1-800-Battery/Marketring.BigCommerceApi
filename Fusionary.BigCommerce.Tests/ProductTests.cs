using System.Text.Json.Serialization;

namespace Fusionary.BigCommerce.Tests;

public class ProductTests : BcTestBase
{
    [Test]
    public async Task Can_Get_First_25_Products_Async()
    {
        var products = await GetProductsAsync(fetchAllPages: false);
        
        products.Count.Should().BeLessOrEqualTo(25);
        
        LogMessage($"Retrieved {products.Count} products");
        DumpObject(products);
        
        Assert.Pass();
    }
    
    [Test]
    [Explicit("This test fetches all products and can take a long time")]
    public async Task Can_Get_All_Products_Async()
    {
        var products = await GetProductsAsync(fetchAllPages: true);
        
        LogMessage($"Retrieved all {products.Count} products");
        
        Assert.Pass();
    }
    
    private async Task<List<BcProductFull>> GetProductsAsync(bool fetchAllPages)
    {
        var productApi = Services.GetRequiredService<BcApiProduct>();
        var cancellationToken = Cts.Token;

        var response = await productApi
            .Search()
            .ChannelId(1404663)
            .Availability(BcAvailability.Available)
            .Include(BcProductInclude.Variants, BcProductInclude.Images, BcProductInclude.CustomFields)
            .Limit(25)
            .Sort(BcProductSort.Name)
            .SendAsync(cancellationToken);

        if (!response)
        {
            DumpObject(response.Error);
            Assert.Fail($"Failed to get products: {response.Error}");
            return new List<BcProductFull>();
        }

        var (data, pagination) = response;
        LogMessage($"First page: {data.Count} products, Total available: {pagination.Total}");

        if (fetchAllPages && response.HasNextPage)
        {
            var remainingItems = await GetRemainingDataAsync(productApi, pagination, cancellationToken);
            data.AddRange(remainingItems);
            LogMessage($"Fetched all pages. Total items: {data.Count}");
        }

        return data;
    }

    public record BcSku
    {
        [JsonPropertyName("id")]
        public BcId Id { get; init; } = string.Empty;
        
        [JsonPropertyName("sku")]
        public string Sku { get; init; } = string.Empty;
    }

    [Test]
    public async Task Can_Get_All_Products_Skus_Async()
    {
        var productApi = Services.GetRequiredService<BcApiProduct>();

        var cancellationToken = Cts.Token;

        var response = await productApi
            .Search()
            .IncludeFields("sku")
            .SendAsync<BcSku>(cancellationToken);

        if (!response)
        {
            DumpObject(response.Error);
            Assert.Fail();
            return;
        }

        DumpObject(response.Data);

        Assert.Pass();
    }

    [Test]
    public async Task Can_Get_Product_By_Id_Async()
    {
        var bcProductApi = Services.GetRequiredService<BcApiProduct>();

        var cancellationToken = CancellationToken.None;

        // First, get any available product ID from the store
        var searchResponse = await bcProductApi
            .Search()
            .Limit(1)
            .SendAsync(cancellationToken);

        if (!searchResponse.Success || !searchResponse.HasData || searchResponse.Data.Count == 0)
        {
            Assert.Inconclusive("No products available in the store to test with");
            return;
        }

        var productId = searchResponse.Data[0].Id;

        var response = await bcProductApi
            .Get()
            .Include(BcProductInclude.Variants, BcProductInclude.Images, BcProductInclude.CustomFields)
            .SendAsync(productId, cancellationToken);

        var product = response.Data;

        DumpObject(response);

        product.Should().NotBeNull();

        var id           = product.Id;
        var name         = product.Name;
        var price        = product.Price;
        var customFields = product.CustomFields;

        var customValues = customFields is not null
            ? string.Join(", ", customFields.Select(x => $"{x.Name}:{x.Value}"))
            : default;

        LogMessage($"{id} | {name} | {price} | {customValues}");

        Assert.Pass();
    }

    private static async Task<List<BcProductFull>> GetRemainingDataAsync(
        BcApiProduct productApi,
        BcPagination pagination,
        CancellationToken cancellationToken = default
    )
    {
        var remainingItems = new List<BcProductFull>();

        BcResultPaged<BcProductFull>? nextPage = null;

        do
        {
            var nextPagination = nextPage?.Pagination ?? pagination;

            nextPage = await productApi
                .Search()
                .Next(nextPagination)
                .SendAsync(cancellationToken);

            if (nextPage.HasData)
            {
                remainingItems.AddRange(nextPage.Data);
            }
        } while (nextPage is { HasNextPage: true });

        return remainingItems;
    }
}