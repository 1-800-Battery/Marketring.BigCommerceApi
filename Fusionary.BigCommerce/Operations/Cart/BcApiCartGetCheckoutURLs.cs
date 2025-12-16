namespace Fusionary.BigCommerce.Operations;

public class BcApiCartGetCheckoutURLs : BcRequestBuilder, IBcApiOperation
{
    public BcApiCartGetCheckoutURLs(IBcApi api) : base(api)
    {
  
    }

    public Task<BcResultData<BcCartRedirectURLs>> SendAsync(
        string cartId,
        BcCartRedirectQueryParms parameterss,
        CancellationToken cancellationToken = default
    ) =>
        SendAsync<BcCartRedirectURLs>(cartId, parameterss, cancellationToken);

    public async Task<BcResultData<T>> SendAsync<T>(string cartId, BcCartRedirectQueryParms parameterss, CancellationToken cancellationToken = default) =>
        await Api.PostDataAsync<T>(
            BcEndpoint.CartRedirectsV3(cartId),
            parameterss,
            Filter,
            Options,
            cancellationToken
        );
}