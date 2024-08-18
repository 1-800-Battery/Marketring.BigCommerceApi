namespace Fusionary.BigCommerce.Operations;

public class BcApiCartGet(IBcApi api) : BcRequestBuilder(api), IBcApiOperation
{
    public Task<BcResultData<BcCartResponseFull>> SendAsync(
        string cartId,
        CancellationToken cancellationToken = default
    )
    {
        // Add default includes for comprehensive cart data
        this.Add("include", "line_items.physical_items.options,shipping_address,shipping_lines");
        return SendAsync<BcCartResponseFull>(cartId, cancellationToken);
    }

    public async Task<BcResultData<T>> SendAsync<T>(string cartId, CancellationToken cancellationToken = default)
    {
        // Add default includes for comprehensive cart data if not already set
        if (!Filter.QueryString.Contains("include"))
            this.Add("include", "line_items.physical_items.options,shipping_address,shipping_lines");
            
        return await Api.GetDataAsync<T>(
            BcEndpoint.CartV3(cartId),
            Filter,
            Options,
            cancellationToken
        );
    }
}