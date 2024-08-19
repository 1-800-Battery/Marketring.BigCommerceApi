namespace Fusionary.BigCommerce.Types;

public record BcCartRedirectURLs
{
    [JsonPropertyName("cart_url")]
    public string? CartUrl { get; set; }

    [JsonPropertyName("checkout_url")]
    public string? CheckoutUrl { get; set; }

    [JsonPropertyName("embedded_checkout_url")]
    public string? EmbeddedCheckoutUrl { get; set; }
}
