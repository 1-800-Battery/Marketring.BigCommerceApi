namespace Fusionary.BigCommerce.Operations;

public class BcApiCartDelete(IBcApi api) : BcRequestBuilder(api), IBcApiOperation
{
    public async Task<BcResult> SendAsync(string cartId, CancellationToken cancellationToken = default) =>
        await Api.DeleteAsync(
            BcEndpoint.CartV3(cartId),
            Filter,
            Options,
            cancellationToken
        );
}
