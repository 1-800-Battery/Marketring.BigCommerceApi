namespace Fusionary.BigCommerce.Operations;

public class BcApiOrderRefundsGet(IBcApi api) : BcRequestBuilder(api), IBcApiOperation
{
    /// <summary>Gets recorded refunds and their payment outcomes for the order.</summary>
    public Task<BcResultData<List<BcOrderRefund>>> SendAsync(
        BcId orderId,
        CancellationToken cancellationToken = default
    ) =>
        Api.GetDataAsync<List<BcOrderRefund>>(
            BcEndpoint.OrderRefundsV3(orderId),
            Filter,
            Options with { OmitSensitiveResponseDetails = true },
            cancellationToken
        );
}
