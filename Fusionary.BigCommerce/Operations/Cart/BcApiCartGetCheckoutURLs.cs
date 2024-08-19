namespace Fusionary.BigCommerce.Operations;

public class BcApiCartGetCheckoutURLs(IBcApi api) : BcRequestBuilder(api), IBcApiOperation
{
    public Task<BcResultData<BcCartRedirectURLs>> SendAsync(
        string cartId,
        BcCartRedirectQueryParms parameters,
        CancellationToken cancellationToken = default
    ) =>
        SendAsync<BcCartRedirectURLs>(cartId, parameters, cancellationToken);

    public async Task<BcResultData<T>> SendAsync<T>(string cartId, BcCartRedirectQueryParms parameters, CancellationToken cancellationToken = default) =>
        await Api.PostDataAsync<T>(
            BcEndpoint.CartRedirectsV3(cartId),
            parameters,
            Filter,
            Options,
            cancellationToken
        );
}