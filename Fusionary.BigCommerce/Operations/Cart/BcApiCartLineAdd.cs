namespace Fusionary.BigCommerce.Operations;

public class BcApiCartLineAdd(IBcApi api) : BcRequestBuilder(api), IBcApiOperation
{
    public Task<BcResultData<BcCartResponseFull>> SendAsync(
        string cartId,
        BcCartLineItems cartItems,
        CancellationToken cancellationToken = default
    )
    {
        // Add default includes for comprehensive cart data
        this.Add("include", "line_items.physical_items.options,shipping_address,shipping_lines");
        return SendAsync<BcCartResponseFull>(cartId, cartItems, cancellationToken);
    }

    public async Task<BcResultData<T>> SendAsync<T>(string cartId, object cartItems, CancellationToken cancellationToken = default)
    {
        // Add default includes for comprehensive cart data if not already set
        if (!Filter.QueryString.Contains("include"))
            this.Add("include", "line_items.physical_items.options,shipping_address,shipping_lines");
            
        return await Api.PostDataAsync<T>(
            BcEndpoint.CartAddV3(cartId),
            cartItems,
            Filter,
            Options,
            cancellationToken
        );
    }
}