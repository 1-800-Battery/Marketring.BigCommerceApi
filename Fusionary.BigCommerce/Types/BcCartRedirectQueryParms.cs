namespace Fusionary.BigCommerce.Types;

public record BcCartRedirectQueryParms
{
    [JsonPropertyName("query_params")]
    public QueryParams? QueryParameters { get; set; }

    public record QueryParams
    {
        [JsonPropertyName("key_1")]
        public string? Key1 { get; set; }

        [JsonPropertyName("key_2")]
        public string? Key2 { get; set; }
    }
}
