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

    public async Task<BcResultData<T>> SendAsync<T>(string cartId, string lineId, object cartItems, CancellationToken cancellationToken = default) =>
        await Api.PutDataAsync<T>(
            BcEndpoint.CartLineItemV3(cartId, lineId),
            cartItems,
            Filter,
            Options,
            cancellationToken
        );
}
