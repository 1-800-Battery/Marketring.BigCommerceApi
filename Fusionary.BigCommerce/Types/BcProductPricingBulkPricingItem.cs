namespace Fusionary.BigCommerce.Types;

public record BcProductPricingBulkPricingItem
{
    public int Minimum { get; init; }
    public int Maximum { get; init; }
    public decimal DiscountAmount { get; init; }
    public string DiscountType { get; init; } = null!;
}