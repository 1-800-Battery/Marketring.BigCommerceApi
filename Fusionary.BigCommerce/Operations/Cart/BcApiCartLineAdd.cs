namespace Fusionary.BigCommerce.Operations;

public class BcApiCartLineAdd(IBcApi api) : BcRequestBuilder(api), IBcApiOperation
{

    public Task<BcResultData<BcCartResponseFull>> SendAsync(
        string cartId,
        BcCartLineItems cartItems,
        CancellationToken cancellationToken = default
    ) =>
        SendAsync<BcCartResponseFull>(cartId, cartItems, cancellationToken);

    public async Task<BcResultData<T>> SendAsync<T>(string cartId, object cartItems, CancellationToken cancellationToken = default) =>
        await Api.PostDataAsync<T>(
            BcEndpoint.CartAddV3(cartId),
            cartItems,
            Filter,
            Options,
            cancellationToken
        );
}