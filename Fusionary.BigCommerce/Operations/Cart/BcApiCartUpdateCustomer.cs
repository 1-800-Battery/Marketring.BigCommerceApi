namespace Fusionary.BigCommerce.Operations;

public class BcApiCartUpdateCustomer(IBcApi api) : BcRequestBuilder(api), IBcApiOperation
{
    public Task<BcResultData<BcCartResponseFull>> SendAsync(
        string cartId,
        BcCartPost cart,
        CancellationToken cancellationToken = default
    ) =>
        SendAsync<BcCartResponseFull>(cartId, cart, cancellationToken);

    public async Task<BcResultData<T>> SendAsync<T>(string cartId, object cart, CancellationToken cancellationToken = default)
    {
        Filter.Add("include", "line_items.physical_items.options,shipping_address,shipping_lines");
        
        return await Api.PutDataAsync<T>(
            BcEndpoint.CartV3(cartId),
            cart,
            Filter,
            Options,
            cancellationToken
        );
    }
}