namespace Fusionary.BigCommerce.Types;

/// <summary>A refund recorded against an order.</summary>
public record BcOrderRefund
{
    public BcId? Id { get; init; }

    public BcId? OrderId { get; init; }

    public BcId? UserId { get; init; }

    public BcDateTime? Created { get; init; }

    public string? Reason { get; init; }

    public decimal? TotalAmount { get; init; }

    public decimal? TotalTax { get; init; }

    public bool? UsesMerchantOverrideValues { get; init; }

    public List<BcOrderRefundItem>? Items { get; init; }

    public List<BcOrderRefundPayment>? Payments { get; init; }
}

public record BcOrderRefundItem
{
    public string? ItemType { get; init; }

    public BcId? ItemId { get; init; }

    public int? Quantity { get; init; }

    public decimal? RequestedAmount { get; init; }

    public string? Reason { get; init; }
}

public record BcOrderRefundPayment
{
    public BcId? Id { get; init; }

    public string? ProviderId { get; init; }

    public decimal? Amount { get; init; }

    [JsonPropertyName("offline")]
    public bool? IsOffline { get; init; }

    public bool? IsDeclined { get; init; }

    public string? DeclinedCode { get; init; }

    public string? DeclinedMessage { get; init; }
}
