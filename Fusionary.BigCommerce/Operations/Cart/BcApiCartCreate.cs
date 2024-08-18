namespace Fusionary.BigCommerce.Operations;

public class BcApiCartCreate(IBcApi api) : BcRequestBuilder(api), IBcApiOperation
{
    public Task<BcResultData<BcCartResponseFull>> SendAsync(
        BcCartPost cart,
        CancellationToken cancellationToken = default
    )
    {
        // Add default includes for comprehensive cart data
        this.Add("include", "line_items.physical_items.options,shipping_address,shipping_lines");
        return SendAsync<BcCartResponseFull>(cart, cancellationToken);
    }

    public async Task<BcResultData<T>> SendAsync<T>(object cart, CancellationToken cancellationToken = default)
    {
        // Add default includes for comprehensive cart data if not already set
        if (!Filter.QueryString.Contains("include"))
            this.Add("include", "line_items.physical_items.options,shipping_address,shipping_lines");
            
        return await Api.PostDataAsync<T>(
            BcEndpoint.CartV3(),
            cart,
            Filter,
            Options,
            cancellationToken
        );
    }
}