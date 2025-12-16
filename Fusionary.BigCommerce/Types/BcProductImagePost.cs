namespace Fusionary.BigCommerce.Types;

[DebuggerDisplay("{ImageUrl}, Description = {Description}, SortOrder = {SortOrder}")]
public record BcProductImagePost
{
    /// <summary>
    /// The unique numeric identifier for the product with which the image is associated.
    /// </summary>
    [JsonPropertyName("product_id")]
    public int ProductId { get; set; }

    /// <summary>
    /// Flag for identifying whether the image is used as the product's thumbnail.
    /// </summary>
    [JsonPropertyName("is_thumbnail")]
    public bool? IsThumbnail { get; set; }

    /// <summary>
    /// The order in which the image will be displayed on the product page. Higher integers give the image a lower
    /// priority. When updating, if the image is given a lower priority, all images with a sort_order the same as or
    /// greater than the image's new sort_order value will have their sort_orders reordered.
    /// </summary>
    [JsonPropertyName("sort_order")]
    public int? SortOrder { get; set; }

    /// <summary>
    /// The description for the image.
    /// </summary>
    [JsonPropertyName("description")]
    public string? Description { get; set; }

    /// <summary>
    /// Must be a fully qualified URL path, including protocol. Limit of 8MB per file.
    /// </summary>
    [JsonPropertyName("image_url")]
    public string? ImageUrl { get; set; }

    /// <summary>
    /// The zoom URL for this image. By default, this is used as the zoom image on product pages when zoom images
    /// are enabled.
    /// </summary>
    [JsonPropertyName("url_zoom")]
    public string? UrlZoom { get; set; }

    /// <summary>
    /// The standard URL for this image. By default, this is used for product-page images.
    /// </summary>
    [JsonPropertyName("url_standard")]
    public string? UrlStandard { get; set; }

    /// <summary>
    /// The thumbnail URL for this image. By default, this is the image size used on the category page and in
    /// side panels.
    /// </summary>
    [JsonPropertyName("url_thumbnail")]
    public string? UrlThumbnail { get; set; }

    /// <summary>
    /// The tiny URL for this image. By default, this is the image size used for thumbnails beneath the product image
    /// on a product page.
    /// </summary>
    [JsonPropertyName("url_tiny")]
    public string? UrlTiny { get; set; }
}