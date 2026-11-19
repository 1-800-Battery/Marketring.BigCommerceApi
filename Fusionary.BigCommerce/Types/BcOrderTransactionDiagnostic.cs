namespace Fusionary.BigCommerce.Types;

/// <summary>A recorded order transaction with payment and gateway identifiers.</summary>
public record BcOrderTransactionDiagnostic
{
    public BcId? Id { get; init; }

    public BcId? OrderId { get; init; }

    public BcDateTime? DateCreated { get; init; }

    public string? Event { get; init; }

    public string? Method { get; init; }

    public decimal? Amount { get; init; }

    public string? Currency { get; init; }

    public string? Gateway { get; init; }

    [JsonConverter(typeof(BcOptionConfig.StringOrNumberConverter))]
    public string? GatewayTransactionId { get; init; }

    /// <summary>The related transaction ID, represented as text for both string and numeric values.</summary>
    [JsonConverter(typeof(BcOptionConfig.StringOrNumberConverter))]
    public string? ReferenceTransactionId { get; init; }

    [JsonPropertyName("test")]
    public bool? IsTest { get; init; }

    public string? Status { get; init; }

    [JsonPropertyName("fraud_review")]
    public bool? IsFraudReviewed { get; init; }

    [JsonConverter(typeof(BcOptionConfig.StringOrNumberConverter))]
    public string? PaymentMethodId { get; init; }

    public string? PaymentInstrumentToken { get; init; }

    public BcOrderTransactionAvsResult? AvsResult { get; init; }

    public BcOrderTransactionCvvResult? CvvResult { get; init; }

    public BcOrderTransactionCreditCard? CreditCard { get; init; }
}

public record BcOrderTransactionAvsResult
{
    public string? Code { get; init; }

    public string? Message { get; init; }

    public string? StreetMatch { get; init; }

    public string? PostalMatch { get; init; }
}

public record BcOrderTransactionCvvResult
{
    public string? Code { get; init; }

    public string? Message { get; init; }
}
