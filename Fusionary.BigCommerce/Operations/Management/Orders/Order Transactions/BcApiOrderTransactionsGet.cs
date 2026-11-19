namespace Fusionary.BigCommerce.Operations.Order_Transactions;

public class BcApiOrderTransactionsGet(IBcApi api) : BcRequestBuilder(api), IBcApiOperation, IBcPageableFilter
{
    public Task<BcResultPaged<BcOrderTransaction>> SendAsync(
        BcId orderId,
        CancellationToken cancellationToken = default
    ) =>
        SendAsync<BcOrderTransaction>(orderId, cancellationToken);
    
    public async Task<BcResultPaged<T>> SendAsync<T>(BcId orderId, CancellationToken cancellationToken = default) =>
        await Api.GetPagedAsync<T>(
            BcEndpoint.OrderTransactionsV3(orderId),
            Filter,
            Options,
            cancellationToken
        );

    /// <summary>Gets a page of recorded transactions with amounts and payment identifiers.</summary>
    public Task<BcResultPaged<BcOrderTransactionDiagnostic>> SendDiagnosticAsync(
        BcId orderId,
        CancellationToken cancellationToken = default
    ) =>
        Api.GetPagedAsync<BcOrderTransactionDiagnostic>(
            BcEndpoint.OrderTransactionsV3(orderId),
            Filter,
            Options with { OmitSensitiveResponseDetails = true },
            cancellationToken
        );
}
