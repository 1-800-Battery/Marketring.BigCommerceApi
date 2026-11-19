using System.Globalization;
using System.Net;
using System.Reflection;
using System.Text;
using System.Text.Json;
using System.Text.Json.Serialization;

using Fusionary.BigCommerce.Operations.Order_Transactions;
using Fusionary.BigCommerce.Utils;

namespace Fusionary.BigCommerce.Tests;

/// <summary>
/// Offline contract tests for the order transaction diagnostic projection and the read-only refund history
/// operation. Every HTTP exchange is served by an in-memory handler; no request leaves the process.
/// </summary>
public class PaymentDiagnosticsTests
{
    private const string TransactionsPath = "/stores/store-example/v3/orders/5001/transactions";
    private const string RefundsPath = "/stores/store-example/v3/orders/5001/payment_actions/refunds";

    private static readonly Type[] DiagnosticTypes =
    [
        typeof(BcOrderTransactionDiagnostic),
        typeof(BcOrderTransactionAvsResult),
        typeof(BcOrderTransactionCvvResult),
        typeof(BcOrderTransactionCreditCard),
        typeof(BcOrderRefund),
        typeof(BcOrderRefundItem),
        typeof(BcOrderRefundPayment)
    ];

    private static string Fixture(string name) =>
        File.ReadAllText(Path.Combine(TestContext.CurrentContext.TestDirectory, "Fixtures", name));

    [Test, SetCulture("fr-FR")]
    public async Task Diagnostic_transactions_expose_success_and_failure_evidence_Async()
    {
        using var api = new OfflineApi(HttpStatusCode.OK, Fixture("order-transactions.json"));

        var result = await api.Api.Orders().OrderTransactions().Get().SendDiagnosticAsync(5001);

        result.Success.Should().BeTrue();
        result.StatusCode.Should().Be(HttpStatusCode.OK);
        Assert.That(result.ResponseText is null);
        result.Data.Should().HaveCount(3);

        var authorization = result.Data[0];
        authorization.Id!.Value.Value.Should().Be(101);
        authorization.OrderId!.Value.Value.Should().Be(5001);
        authorization.Event.Should().Be("authorization");
        authorization.Method.Should().Be("credit_card");
        authorization.Amount.Should().Be(62.2m);
        authorization.Currency.Should().Be("USD");
        authorization.Gateway.Should().Be("stripe");
        authorization.GatewayTransactionId.Should().Be("gw_txn_example_0001");
        authorization.DateCreated!.Value.Value.Should().Be(new DateTimeOffset(2026, 1, 10, 21, 5, 30, TimeSpan.Zero));
        authorization.IsTest.Should().BeFalse();
        authorization.Status.Should().Be("ok");
        authorization.IsFraudReviewed.Should().BeFalse();
        authorization.ReferenceTransactionId.Should().BeNull();
        authorization.PaymentMethodId.Should().Be("stripev3.card");
        authorization.PaymentInstrumentToken.Should().Be("instrument-token-redacted");
        authorization.AvsResult!.Code.Should().Be("Y");
        authorization.AvsResult.Message.Should().Be("Synthetic AVS message");
        authorization.AvsResult.StreetMatch.Should().Be("Y");
        authorization.AvsResult.PostalMatch.Should().Be("Y");
        authorization.CvvResult!.Code.Should().Be("M");
        authorization.CvvResult.Message.Should().Be("Synthetic CVV message");
        authorization.CreditCard!.CardType.Should().Be("visa");
        authorization.CreditCard.CardLast4.Should().Be("0000");
        authorization.CreditCard.CardExpiryMonth.Should().Be(12);
        authorization.CreditCard.CardExpiryYear.Should().Be(2030);

        var failedCapture = result.Data[1];
        failedCapture.Event.Should().Be("capture");
        failedCapture.Status.Should().Be("error");
        failedCapture.Amount.Should().Be(62.2m);
        failedCapture.GatewayTransactionId.Should().BeNull();
        failedCapture.ReferenceTransactionId.Should().Be("101");
        failedCapture.DateCreated!.Value.Value.Should().Be(new DateTimeOffset(2026, 1, 11, 9, 15, 0, TimeSpan.Zero));
        failedCapture.AvsResult.Should().BeNull();
        failedCapture.CvvResult.Should().BeNull();
        failedCapture.CreditCard.Should().BeNull();

        var future = result.Data[2];
        future.OrderId!.Value.Value.Should().Be(5001);
        future.Event.Should().Be("future_event");
        future.Method.Should().Be("future_method");
        future.Gateway.Should().Be("future_gateway");
        future.Status.Should().Be("future_status");
        future.Amount.Should().Be(0.1m);
        future.DateCreated.Should().BeNull();
        future.ReferenceTransactionId.Should().Be("101");
        future.GatewayTransactionId.Should().BeNull();
        future.PaymentMethodId.Should().BeNull();
        future.PaymentInstrumentToken.Should().BeNull();
        future.IsTest.Should().BeNull();
        future.IsFraudReviewed.Should().BeNull();

        result.Pagination.Total.Should().Be(4);
        result.Pagination.TotalPages.Should().Be(2);
        result.HasNextPage.Should().BeTrue();

        api.Requests.Should().ContainSingle();
        api.Requests[0].Method.Should().Be(HttpMethod.Get);
        api.Requests[0].Uri.AbsolutePath.Should().Be(TransactionsPath);
        api.Requests[0].Uri.Query.Should().BeEmpty();
    }

    [Test]
    public async Task Diagnostic_transaction_pages_are_requested_through_the_existing_builder_Async()
    {
        using var api = new OfflineApi(HttpStatusCode.OK, Fixture("order-transactions.json"));

        var result = await api.Api.Orders().OrderTransactions().Get().Page(2).Limit(3).SendDiagnosticAsync(5001);

        result.Success.Should().BeTrue();
        api.Requests.Should().ContainSingle();
        api.Requests[0].Method.Should().Be(HttpMethod.Get);
        api.Requests[0].Uri.AbsolutePath.Should().Be(TransactionsPath);
        api.Requests[0].Uri.Query.Should().Be("?page=2&limit=3");
    }

    [Test]
    public void Legacy_transaction_contract_is_unchanged()
    {
        typeof(BcOrderTransaction).GetProperties().Select(property => property.Name).Should().BeEquivalentTo(
            "Id", "OrderId", "DateCreated", "Event", "Method", "Currency", "Gateway", "IsTest", "Status",
            "IsFraudReviewed", "PaymentMethodId", "PaymentInstrumentToken", "CreditCard");
        typeof(BcOrderTransaction).GetProperty(nameof(BcOrderTransaction.PaymentMethodId))!.PropertyType
            .Should().Be(typeof(int?));
        typeof(BcOrderTransaction).GetProperty(nameof(BcOrderTransaction.DateCreated))!.PropertyType
            .Should().Be(typeof(BcDateTime));

        var legacy = typeof(BcApiOrderTransactionsGet).GetMethod(
            nameof(BcApiOrderTransactionsGet.SendAsync),
            0,
            [typeof(BcId), typeof(CancellationToken)])!;
        legacy.ReturnType.Should().Be(typeof(Task<BcResultPaged<BcOrderTransaction>>));

        var diagnostic = typeof(BcApiOrderTransactionsGet).GetMethod(
            nameof(BcApiOrderTransactionsGet.SendDiagnosticAsync),
            [typeof(BcId), typeof(CancellationToken)])!;
        diagnostic.ReturnType.Should().Be(typeof(Task<BcResultPaged<BcOrderTransactionDiagnostic>>));
    }

    [Test]
    public async Task Legacy_transaction_send_keeps_its_endpoint_and_numeric_payment_method_contract_Async()
    {
        using var api = new OfflineApi(HttpStatusCode.OK, "{\"data\":[{\"id\":1,\"payment_method_id\":7}],\"meta\":{}}");

        var result = await api.Api.Orders().OrderTransactions().Get().SendAsync(5001);

        result.Success.Should().BeTrue();
        result.Data.Single().PaymentMethodId.Should().Be(7);
        Assert.That(result.ResponseText is not null);
        api.Requests.Single().Uri.AbsolutePath.Should().Be(TransactionsPath);
    }

    [Test]
    public async Task Legacy_transaction_model_cannot_represent_string_payment_method_ids_Async()
    {
        // Documents why the additive diagnostic projection is required: the legacy model is retained
        // unchanged for compatibility and still rejects the documented string identifier.
        using var api = new OfflineApi(HttpStatusCode.OK, Fixture("order-transactions.json"));

        var legacy = await api.Api.Orders().OrderTransactions().Get().SendAsync(5001);
        var diagnostic = await api.Api.Orders().OrderTransactions().Get().SendDiagnosticAsync(5001);

        legacy.Success.Should().BeFalse();
        legacy.Error.ErrorDetails["Field"].Should().Be("payment_method_id");
        diagnostic.Success.Should().BeTrue();
    }

    [TestCase("0", "0")]
    [TestCase("0.0", "0")]
    [TestCase("0.01", "0.01")]
    [TestCase("\"0.10\"", "0.10")]
    [TestCase("62.2", "62.2")]
    [TestCase("\"62.2\"", "62.2")]
    [TestCase("21474836.47", "21474836.47")]
    [TestCase("62.200000000000000001", "62.200000000000000001")]
    [TestCase("\"62.200000000000000001\"", "62.200000000000000001")]
    [SetCulture("de-DE")]
    public void Diagnostic_money_preserves_decimal_precision(string json, string expected)
    {
        var value = decimal.Parse(expected, CultureInfo.InvariantCulture);

        BcJsonUtil.Deserialize<BcOrderTransactionDiagnostic>("{\"amount\":" + json + "}")!.Amount.Should().Be(value);

        var refund = BcJsonUtil.Deserialize<BcOrderRefund>(
            "{\"total_amount\":" + json + ",\"total_tax\":" + json +
            ",\"items\":[{\"requested_amount\":" + json + "}],\"payments\":[{\"amount\":" + json + "}]}")!;
        refund.TotalAmount.Should().Be(value);
        refund.TotalTax.Should().Be(value);
        refund.Items![0].RequestedAmount.Should().Be(value);
        refund.Payments![0].Amount.Should().Be(value);
    }

    [Test]
    public void Negative_refund_tax_is_retained()
    {
        BcJsonUtil.Deserialize<BcOrderRefund>("{\"total_tax\":-0.37}")!.TotalTax.Should().Be(-0.37m);
    }

    [TestCase("\"stripev3.card\"", "stripev3.card")]
    [TestCase("\"braintree.paypal\"", "braintree.paypal")]
    [TestCase("\"0012\"", "0012")]
    [TestCase("12", "12")]
    [TestCase("null", null)]
    public void Payment_method_ids_are_strings(string json, string? expected)
    {
        BcJsonUtil.Deserialize<BcOrderTransactionDiagnostic>("{\"payment_method_id\":" + json + "}")!
            .PaymentMethodId.Should().Be(expected);
    }

    [TestCase("101", "101")]
    [TestCase("\"101\"", "101")]
    [TestCase("0", "0")]
    [TestCase("9007199254740993", "9007199254740993")]
    [TestCase("\"ref-example-01\"", "ref-example-01")]
    [TestCase("null", null)]
    [SetCulture("fr-FR")]
    public void Reference_and_gateway_transaction_ids_accept_numeric_and_string_forms(string json, string? expected)
    {
        var transaction = BcJsonUtil.Deserialize<BcOrderTransactionDiagnostic>(
            "{\"reference_transaction_id\":" + json + ",\"gateway_transaction_id\":" + json + "}")!;

        transaction.ReferenceTransactionId.Should().Be(expected);
        transaction.GatewayTransactionId.Should().Be(expected);
    }

    [TestCase("true")]
    [TestCase("{}")]
    [TestCase("[]")]
    public async Task Unsupported_reference_id_shapes_fail_with_safe_diagnostics_Async(string json)
    {
        using var api = new OfflineApi(
            HttpStatusCode.OK,
            "{\"data\":[{\"gateway_transaction_id\":\"gw_private_value\",\"reference_transaction_id\":" + json + "}],\"meta\":{}}");

        var result = await api.Api.Orders().OrderTransactions().Get().SendDiagnosticAsync(5001);

        result.Success.Should().BeFalse();
        result.Data.Should().BeEmpty();
        result.Error.Type.Should().Be(nameof(JsonException));
        result.Error.ErrorDetails["JsonPath"].Should().Be("$[0].reference_transaction_id");
        result.Error.ErrorDetails["Field"].Should().Be("reference_transaction_id");
        Assert.That(!BcJsonUtil.Serialize(result).Contains("gw_private_value", StringComparison.Ordinal));
    }

    [TestCase("{}")]
    [TestCase("{\"id\":null,\"order_id\":null,\"date_created\":null,\"event\":null,\"method\":null,\"amount\":null," +
              "\"currency\":null,\"gateway\":null,\"gateway_transaction_id\":null,\"test\":null,\"status\":null," +
              "\"fraud_review\":null,\"reference_transaction_id\":null,\"payment_method_id\":null," +
              "\"payment_instrument_token\":null,\"avs_result\":null,\"cvv_result\":null,\"credit_card\":null}")]
    public void Missing_and_null_transaction_fields_are_tolerated(string json)
    {
        var transaction = BcJsonUtil.Deserialize<BcOrderTransactionDiagnostic>(json)!;

        transaction.Should().BeEquivalentTo(new BcOrderTransactionDiagnostic());
        transaction.Amount.Should().BeNull();
        transaction.DateCreated.Should().BeNull();
    }

    [Test]
    public void Missing_and_null_refund_fields_are_tolerated()
    {
        var refund = BcJsonUtil.Deserialize<BcOrderRefund>(
            "{\"id\":null,\"order_id\":null,\"user_id\":null,\"created\":\"\",\"reason\":null,\"total_amount\":null," +
            "\"total_tax\":null,\"uses_merchant_override_values\":null,\"items\":[{}],\"payments\":[{}]}")!;

        refund.Id.Should().BeNull();
        refund.Created.Should().BeNull();
        refund.TotalAmount.Should().BeNull();
        refund.Items.Should().ContainSingle().Which.Should().BeEquivalentTo(new BcOrderRefundItem());
        refund.Payments.Should().ContainSingle().Which.Should().BeEquivalentTo(new BcOrderRefundPayment());
        BcJsonUtil.Deserialize<BcOrderRefund>("{}")!.Should().BeEquivalentTo(new BcOrderRefund());
    }

    [Test, SetCulture("ar-SA")]
    public async Task Refund_history_exposes_typed_decline_evidence_Async()
    {
        using var api = new OfflineApi(HttpStatusCode.OK, Fixture("order-refunds.json"));

        var result = await api.Api.Orders().OrderRefunds().Get().SendAsync(5001);

        result.Success.Should().BeTrue();
        result.Data.Should().HaveCount(2);
        Assert.That(result.ResponseText is null);

        var declined = result.Data[0];
        declined.Id!.Value.Value.Should().Be(21);
        declined.OrderId!.Value.Value.Should().Be(5001);
        declined.UserId!.Value.Value.Should().Be(0);
        declined.Created!.Value.Value.Should().Be(new DateTimeOffset(2026, 1, 12, 10, 0, 0, TimeSpan.Zero));
        declined.Reason.Should().Be("Synthetic refund reason");
        declined.TotalAmount.Should().Be(12.34m);
        declined.TotalTax.Should().Be(0.99m);
        declined.UsesMerchantOverrideValues.Should().BeFalse();
        var item = declined.Items.Should().ContainSingle().Subject;
        item.ItemType.Should().Be("PRODUCT");
        item.ItemId!.Value.Value.Should().Be(7001);
        item.Reason.Should().Be("Synthetic item reason");
        item.Quantity.Should().Be(1);
        item.RequestedAmount.Should().Be(12.34m);
        var payment = declined.Payments.Should().ContainSingle().Subject;
        payment.Id!.Value.Value.Should().Be(31);
        payment.ProviderId.Should().Be("stripev3");
        payment.Amount.Should().Be(12.34m);
        payment.IsOffline.Should().BeFalse();
        payment.IsDeclined.Should().BeTrue();
        payment.DeclinedCode.Should().Be("example_decline");
        payment.DeclinedMessage.Should().Be("Synthetic decline message");

        var partial = result.Data[1];
        partial.OrderId!.Value.Value.Should().Be(5001);
        partial.UserId.Should().BeNull();
        partial.Created.Should().BeNull();
        partial.Reason.Should().BeNull();
        partial.TotalAmount.Should().Be(0.01m);
        partial.TotalTax.Should().Be(0m);
        partial.UsesMerchantOverrideValues.Should().BeNull();
        partial.Items![0].ItemType.Should().Be("FUTURE_ITEM_TYPE");
        partial.Items[0].Quantity.Should().BeNull();
        partial.Payments.Should().HaveCount(2);
        partial.Payments![0].IsDeclined.Should().BeFalse();
        partial.Payments[0].IsOffline.Should().BeTrue();
        partial.Payments[0].DeclinedCode.Should().BeNull();
        partial.Payments[0].DeclinedMessage.Should().BeNull();
        partial.Payments[1].ProviderId.Should().BeNull();
        partial.Payments[1].Amount.Should().BeNull();
        partial.Payments[1].IsDeclined.Should().BeNull();
        partial.Payments[1].DeclinedCode.Should().BeNull();

        api.Requests.Should().ContainSingle();
        api.Requests[0].Method.Should().Be(HttpMethod.Get);
        api.Requests[0].Uri.AbsolutePath.Should().Be(RefundsPath);
        api.Requests[0].Uri.Query.Should().BeEmpty();
    }

    [Test]
    public void Refund_endpoint_is_built_from_the_order_id()
    {
        BcEndpoint.OrderRefundsV3(5001).Should().Be("v3/orders/5001/payment_actions/refunds");
        BcEndpoint.OrderTransactionsV3(5001).Should().Be("v3/orders/5001/transactions");
    }

    [Test]
    public async Task Empty_transaction_and_refund_lists_succeed_Async()
    {
        using (var api = new OfflineApi(
                   HttpStatusCode.OK,
                   "{\"data\":[],\"meta\":{\"pagination\":{\"total\":0,\"count\":0,\"per_page\":50,\"current_page\":1,\"total_pages\":0,\"links\":{\"current\":\"?page=1&limit=50\"}}}}"))
        {
            var transactions = await api.Api.Orders().OrderTransactions().Get().SendDiagnosticAsync(5001);
            transactions.Success.Should().BeTrue();
            transactions.Data.Should().BeEmpty();
            transactions.HasData.Should().BeFalse();
            transactions.HasNextPage.Should().BeFalse();
        }

        using (var api = new OfflineApi(HttpStatusCode.NoContent, null))
        {
            var transactions = await api.Api.Orders().OrderTransactions().Get().SendDiagnosticAsync(5001);
            transactions.Success.Should().BeTrue();
            transactions.Data.Should().BeEmpty();
        }

        using (var api = new OfflineApi(HttpStatusCode.OK, "{\"data\":[],\"meta\":{}}"))
        {
            var refunds = await api.Api.Orders().OrderRefunds().Get().SendAsync(5001);
            refunds.Success.Should().BeTrue();
            refunds.Data.Should().BeEmpty();
        }
    }

    [Test]
    public async Task Cancellation_propagates_before_any_request_is_sent_Async()
    {
        using var cancellation = new CancellationTokenSource();
        await cancellation.CancelAsync();
        using var api = new OfflineApi(HttpStatusCode.OK, Fixture("order-refunds.json"));

        var transactions = () => api.Api.Orders().OrderTransactions().Get().SendDiagnosticAsync(5001, cancellation.Token);
        var refunds = () => api.Api.Orders().OrderRefunds().Get().SendAsync(5001, cancellation.Token);

        await transactions.Should().ThrowAsync<OperationCanceledException>();
        await refunds.Should().ThrowAsync<OperationCanceledException>();
        api.Requests.Should().BeEmpty();
    }

    [TestCase("{\"status\":404,\"title\":\"private provider text\",\"errors\":{\"payment\":\"private provider text\"}}",
        TestName = "Http_errors_omit_provider_error_objects")]
    [TestCase("\"private provider text\"", TestName = "Http_errors_omit_provider_error_strings")]
    [TestCase("private provider text", TestName = "Http_errors_tolerate_malformed_error_json")]
    public async Task Http_errors_return_status_without_provider_payloads_Async(string body)
    {
        using var api = new OfflineApi(HttpStatusCode.NotFound, body);

        var transactions = await api.Api.Orders().OrderTransactions().Get().SendDiagnosticAsync(5001);
        var refunds = await api.Api.Orders().OrderRefunds().Get().SendAsync(5001);

        transactions.Success.Should().BeFalse();
        transactions.StatusCode.Should().Be(HttpStatusCode.NotFound);
        transactions.Error.Status.Should().Be(HttpStatusCode.NotFound);
        transactions.Data.Should().BeEmpty();
        refunds.Success.Should().BeFalse();
        refunds.StatusCode.Should().Be(HttpStatusCode.NotFound);
        refunds.Error.Status.Should().Be(HttpStatusCode.NotFound);
        refunds.Data.Should().BeNull();
        Assert.That(transactions.ResponseText is null && refunds.ResponseText is null);
        Assert.That(!BcJsonUtil.Serialize(transactions).Contains("private provider text", StringComparison.Ordinal));
        Assert.That(!BcJsonUtil.Serialize(refunds).Contains("private provider text", StringComparison.Ordinal));
    }

    [Test]
    public async Task Malformed_transaction_money_fails_without_exposing_values_Async()
    {
        using var api = new OfflineApi(
            HttpStatusCode.OK,
            "{\"data\":[{\"amount\":\"not-a-number\",\"gateway_transaction_id\":\"gw_private_value\"," +
            "\"payment_instrument_token\":\"private-token\"}],\"meta\":{}}");

        var result = await api.Api.Orders().OrderTransactions().Get().SendDiagnosticAsync(5001);

        result.Success.Should().BeFalse();
        result.Error.Type.Should().Be(nameof(JsonException));
        result.Error.ErrorDetails["JsonPath"].Should().Be("$[0].amount");
        result.Error.ErrorDetails["Field"].Should().Be("amount");
        Assert.That(result.ResponseText is null);
        var serialized = BcJsonUtil.Serialize(result);
        Assert.That(!serialized.Contains("not-a-number", StringComparison.Ordinal));
        Assert.That(!serialized.Contains("gw_private_value", StringComparison.Ordinal));
        Assert.That(!serialized.Contains("private-token", StringComparison.Ordinal));
    }

    [Test]
    public async Task Malformed_refund_payment_fails_without_exposing_decline_text_Async()
    {
        using var api = new OfflineApi(
            HttpStatusCode.OK,
            "{\"data\":[{\"id\":1,\"reason\":\"private reason\",\"payments\":[{\"declined_message\":\"private decline text\"," +
            "\"amount\":\"not-a-number\"}]}],\"meta\":{}}");

        var result = await api.Api.Orders().OrderRefunds().Get().SendAsync(5001);

        result.Success.Should().BeFalse();
        result.Error.Type.Should().Be(nameof(JsonException));
        result.Error.ErrorDetails["JsonPath"].Should().Be("$[0].payments[0].amount");
        result.Error.ErrorDetails["Field"].Should().Be("amount");
        Assert.That(result.ResponseText is null);
        Assert.That(!BcJsonUtil.Serialize(result).Contains("private", StringComparison.Ordinal));
    }

    [Test]
    public void Diagnostic_models_do_not_capture_raw_payloads()
    {
        foreach (var type in DiagnosticTypes)
        {
            foreach (var property in type.GetProperties())
            {
                property.IsDefined(typeof(JsonExtensionDataAttribute)).Should().BeFalse(property.Name);
                property.PropertyType.Should().NotBe(typeof(object), property.Name);
                property.PropertyType.Should().NotBe(typeof(JsonElement), property.Name);
                property.PropertyType.Should().NotBe(typeof(JsonDocument), property.Name);
                property.PropertyType.Should().NotBeAssignableTo<System.Collections.IDictionary>(property.Name);
                property.Name.Should().NotContainAny("Raw", "Body", "ResponseText", "Extension");
            }

            typeof(IBcExtensionData).IsAssignableFrom(type).Should().BeFalse(type.Name);
        }
    }

    [Test]
    public void Refund_history_operation_is_read_only()
    {
        typeof(BcApiOrdersOverview).GetMethod(nameof(BcApiOrdersOverview.OrderRefunds))!.ReturnType
            .Should().Be(typeof(BcApiOrderRefunds));
        DeclaredMethods(typeof(BcApiOrderRefunds)).Should().BeEquivalentTo("Get");
        DeclaredMethods(typeof(BcApiOrderRefundsGet)).Should().BeEquivalentTo("SendAsync");
        typeof(BcApiOrderRefundsGet).GetMethod(
                nameof(BcApiOrderRefundsGet.SendAsync),
                [typeof(BcId), typeof(CancellationToken)])!
            .ReturnType.Should().Be(typeof(Task<BcResultData<List<BcOrderRefund>>>));
    }

    private static IEnumerable<string> DeclaredMethods(Type type) =>
        type.GetMethods(BindingFlags.Public | BindingFlags.Instance | BindingFlags.DeclaredOnly)
            .Where(method => !method.IsSpecialName)
            .Select(method => method.Name)
            .Distinct();

    /// <summary>Builds an <see cref="IBcApi" /> whose HTTP clients are served by an in-memory handler.</summary>
    private sealed class OfflineApi : IDisposable
    {
        private readonly RecordingHandler _handler;
        private readonly HttpClient _client;
        private readonly HttpClient _b2bClient;

        public OfflineApi(HttpStatusCode status, string? body)
        {
            _handler = new RecordingHandler(status, body);
            _client = new HttpClient(_handler, disposeHandler: false);
            _b2bClient = new HttpClient(_handler, disposeHandler: false);
            Api = BcApi.Create(
                BigCommerceClient.Create(
                    _client,
                    _b2bClient,
                    new BigCommerceConfig
                    {
                        Host = "https://api.example.invalid",
                        B2BHost = "https://b2b.example.invalid",
                        StoreHash = "store-example",
                        AccessToken = "placeholder",
                        B2BAccessToken = "placeholder"
                    }));
        }

        public IBcApi Api { get; }

        public IReadOnlyList<(HttpMethod Method, Uri Uri)> Requests => _handler.Requests;

        public void Dispose()
        {
            _client.Dispose();
            _b2bClient.Dispose();
            _handler.Dispose();
        }
    }

    private sealed class RecordingHandler(HttpStatusCode status, string? body) : HttpMessageHandler
    {
        public List<(HttpMethod Method, Uri Uri)> Requests { get; } = [];

        protected override Task<HttpResponseMessage> SendAsync(HttpRequestMessage request, CancellationToken cancellationToken)
        {
            cancellationToken.ThrowIfCancellationRequested();
            Requests.Add((request.Method, request.RequestUri!));

            return Task.FromResult(new HttpResponseMessage(status)
            {
                RequestMessage = request,
                Content = body is null
                    ? new ByteArrayContent([])
                    : new StringContent(body, Encoding.UTF8, "application/json")
            });
        }
    }
}
