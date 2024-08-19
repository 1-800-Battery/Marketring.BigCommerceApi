namespace Fusionary.BigCommerce.Operations;

public class BcApiCartUpdateLine(IBcApi api) : BcRequestBuilder(api), IBcApiOperation
{
    public Task<BcResultData<BcCartResponseFull>> SendAsync(
        string cartId,
        string lineId,
        object cartItem,
        CancellationToken cancellationToken = default
    ) =>
        SendAsync<BcCartResponseFull>(cartId, lineId, cartItem, cancellationToken);

    public async Task<BcResultData<T>> SendAsync<T>(string cartId, string lineId, object cartItems, CancellationToken cancellationToken = default)
    {
        // Add default includes if not already specified
        if (!Filter.ToQueryString().ToString().Contains("include"))
        {
            Filter.Add("include", "line_items.physical_items.options,shipping_address,shipping_lines");
        }
        
        return await Api.PutDataAsync<T>(
            BcEndpoint.CartLineItemV3(cartId, lineId),
            cartItems,
            Filter,
            Options,
            cancellationToken
        );
    }
}
