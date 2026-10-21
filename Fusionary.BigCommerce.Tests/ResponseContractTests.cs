using System.Globalization;
using System.Net;
using System.Text;
using System.Text.Json;
using Fusionary.BigCommerce.Utils;

namespace Fusionary.BigCommerce.Tests;

public class ResponseContractTests
{
    private static string Fixture(string name) => File.ReadAllText(Path.Combine(TestContext.CurrentContext.TestDirectory, "Fixtures", name));

    private static async Task<BcResult<T, BcMetadataEmpty>> ReadAsync<T>(string body, HttpStatusCode status = HttpStatusCode.OK)
    {
        using var response = new HttpResponseMessage(status) { Content = new StringContent(body, Encoding.UTF8, "application/json") };
        return await response.ReadResponseAsync<T, BcMetadataEmpty>();
    }

    [TestCase(HttpStatusCode.OK)]
    [TestCase(HttpStatusCode.Created)]
    public async Task Cart_decimal_discounts_survive_successful_responses_Async(HttpStatusCode status)
    {
        var result = await ReadAsync<BcCartResponseFull>(Fixture("cart.json"), status);
        result.Success.Should().BeTrue();
        result.StatusCode.Should().Be(status);
        Convert.ToDecimal(result.Data.Discounts![0].DiscountedAmount).Should().Be(62.2m);
        Convert.ToDecimal(result.Data.Discounts![1].DiscountedAmount).Should().Be(1.8m);
        result.Data.LineItems!.PhysicalItems![0].ListPrice.Should().Be(311d);
        result.Data.ExtensionData.Should().ContainKey("company_id");
        result.ResponseText.Should().Contain("discounted_amount");
    }

    [TestCase("0")]
    [TestCase("0.0")]
    [TestCase("62.200000000000000001")]
    [TestCase("\"62.200000000000000001\"")]
    public void Every_cart_money_field_preserves_decimal_precision(string number)
    {
        var cart = BcJsonUtil.Deserialize<BcCartResponseFull>("{\"discount_amount\":" + number + ",\"manual_discount_amount\":" + number + ",\"discounts\":[{\"discounted_amount\":" + number + "}],\"line_items\":{\"physical_items\":[{\"discount_amount\":" + number + ",\"coupon_amount\":" + number + "}],\"custom_items\":[{\"quantity\":\"1\",\"list_price\":" + number + "}],\"gift_certificates\":[{\"amount\":" + number + "}]}}")!;
        var expected = decimal.Parse(number.Trim('"'), CultureInfo.InvariantCulture);
        Convert.ToDecimal(cart.DiscountAmount).Should().Be(expected);
        Convert.ToDecimal(cart.ManualDiscountAmount).Should().Be(expected);
        Convert.ToDecimal(cart.Discounts![0].DiscountedAmount).Should().Be(expected);
        Convert.ToDecimal(cart.LineItems!.PhysicalItems![0].DiscountAmount).Should().Be(expected);
        Convert.ToDecimal(cart.LineItems.PhysicalItems[0].CouponAmount).Should().Be(expected);
        Convert.ToDecimal(cart.LineItems.CustomItems![0].ListPrice).Should().Be(expected);
        Convert.ToDecimal(cart.LineItems.GiftCertificates![0].Amount).Should().Be(expected);
    }

    [Test]
    public async Task Supported_checkout_urls_deserialize_Async()
    {
        var result = await ReadAsync<BcCartRedirectURLs>(Fixture("checkout-urls.json"));
        result.Success.Should().BeTrue();
        result.Data.CheckoutUrl.Should().Be("https://store.example/checkout");
        result.Data.EmbeddedCheckoutUrl.Should().Be("https://store.example/embedded-checkout");
    }

    [Test, SetCulture("fr-FR")]
    public async Task Order_numbers_and_dates_are_culture_independent_Async()
    {
        var result = await ReadAsync<BcOrderResponseFull>(Fixture("order.json"));
        result.Success.Should().BeTrue();
        result.Data.TotalExTax!.Value.Value.Should().Be(62.2m);
        result.Data.DateCreated!.Value.Value.Year.Should().Be(2018);
        result.Data.DateShipped.Should().BeNull();
        result.Data.StoreCreditAmount.Should().BeNull();
    }

    [Test, SetCulture("de-DE")]
    public async Task Product_and_customer_money_are_culture_independent_Async()
    {
        var product = await ReadAsync<BcProductFull>(Fixture("product.json"));
        product.Success.Should().BeTrue();
        product.Data.Price.Value.Should().Be(62.2m);
        product.Data.CostPrice.Value.Should().Be(0m);
        var customers = await ReadAsync<List<BcCustomer>>(Fixture("customers.json"));
        customers.Success.Should().BeTrue();
        customers.Data[0].StoreCreditAmounts![0].Amount.Value.Should().Be(62.2m);
        customers.Data[0].DateModified.Should().BeNull();
    }

    [TestCase("1")]
    [TestCase("1.0")]
    [TestCase("\"1.0\"")]
    [TestCase("1e0")]
    public void Integral_quantities_accept_equivalent_numeric_representations(string number)
    {
        BcJsonUtil.Deserialize<BcCartLineItem>("{\"quantity\":" + number + "}")!.Quantity.Should().Be(1);
        BcJsonUtil.Deserialize<BcOrderProduct>("{\"quantity\":" + number + "}")!.Quantity.Should().Be(1);
    }

    [TestCase("1.8")]
    [TestCase("\"1.8\"")]
    [TestCase("9223372036854775808")]
    [TestCase("1.000000000000000000000000000001")]
    [TestCase("1e-100")]
    [TestCase("null")]
    public async Task Invalid_required_quantity_fails_without_rounding_Async(string number)
    {
        var result = await ReadAsync<BcCartResponseFull>("{\"data\":{\"line_items\":{\"physical_items\":[{\"quantity\":" + number + "}]}}}");
        result.Success.Should().BeFalse();
        result.Error.Type.Should().Be(nameof(JsonException));
        result.Error.ErrorDetails["JsonPath"].Should().Be("$.line_items.physical_items[0].quantity");
    }

    [TestCase("\"future_product_type\"")]
    [TestCase("32767")]
    public async Task Unknown_enum_values_do_not_fail_product_responses_Async(string value)
    {
        var result = await ReadAsync<BcProductFull>(Fixture("product.json").Replace("\"physical\"", value, StringComparison.Ordinal));
        result.Success.Should().BeTrue();
        result.Data.Type.Should().NotBe(BcProductType.Physical);
        result.Data.Type.Should().NotBe(BcProductType.Digital);
    }

    [TestCase("null")]
    [TestCase("\"\"")]
    public void Missing_dates_preserve_nullable_and_value_type_contracts(string value)
    {
        BcJsonUtil.Deserialize<BcCustomer>("{\"date_created\":" + value + "}")!.DateCreated.Should().BeNull();
        BcJsonUtil.Deserialize<BcProductFull>("{\"price\":1,\"date_created\":" + value + "}")!.DateCreated.HasValue.Should().BeFalse();
    }

    [TestCase("\"not-a-number\"", "price")]
    [TestCase("\"not-a-date\"", "date_created")]
    public async Task Invalid_scalar_values_fail_with_safe_diagnostics_Async(string value, string field)
    {
        using var request = new HttpRequestMessage(HttpMethod.Post, "https://store.example/?email=private@example.invalid") { Content = new StringContent("private@example.invalid") };
        using var response = new HttpResponseMessage(HttpStatusCode.OK)
        {
            RequestMessage = request,
            Content = new StringContent("{\"price\":1,\"" + field + "\":" + value + ",\"email\":\"private@example.invalid\"}", Encoding.UTF8, "application/json")
        };
        response.Headers.Add("X-Rate-Limit-Requests-Left", "9");
        var result = await response.ReadResponseAsync<BcProductFull, BcMetadataEmpty>();
        result.Success.Should().BeFalse();
        result.Error.Type.Should().Be(nameof(JsonException));
        result.Error.ErrorDetails["JsonPath"].Should().Be("$." + field);
        result.Error.ErrorDetails["Field"].Should().Be(field);
        result.RateLimits.RequestsLeft.Should().Be(9);
        BcJsonUtil.Serialize(result).Should().NotContain("private@example.invalid").And.NotContain(value.Trim('"'));
    }

    [Test]
    public async Task Arbitrary_json_keys_are_not_exposed_in_diagnostics_Async()
    {
        var result = await ReadAsync<Dictionary<string, int>>("{\"private@example.invalid\":\"private value\"}");
        result.Success.Should().BeFalse();
        BcJsonUtil.Serialize(result).Should().NotContain("private");
        result.Error.ErrorDetails["JsonPath"].Should().Be("$[redacted]");
    }

    [TestCase("1.8")]
    [TestCase("\"1.8\"")]
    public void Bulk_pricing_discounts_support_fractions(string value)
    {
        var pricing = BcJsonUtil.Deserialize<BcProductPricingBulkPricingItem>("{\"minimum\":1,\"maximum\":10,\"discount_amount\":" + value + ",\"discount_type\":\"price\"}")!;
        Convert.ToDecimal(pricing.DiscountAmount).Should().Be(1.8m);
    }

    [TestCase("1")]
    [TestCase("1.0")]
    [TestCase("\"1.0\"")]
    public void Flexible_ids_accept_integral_numbers(string value)
    {
        BcJsonUtil.Deserialize<BcId>(value).Value.Should().Be(1);
    }

    [TestCase("\"invalid\"")]
    [TestCase("\"1.8\"")]
    [TestCase("2147483648")]
    public void Flexible_ids_reject_invalid_values(string value)
    {
        var read = () => BcJsonUtil.Deserialize<BcId>(value);
        read.Should().Throw<JsonException>();
    }

    [Test]
    public async Task Out_of_range_timestamps_keep_the_json_path_Async()
    {
        var result = await ReadAsync<BcCustomer>("{\"date_created\":9223372036854775807}");
        result.Success.Should().BeFalse();
        result.Error.Type.Should().Be(nameof(JsonException));
        result.Error.ErrorDetails["JsonPath"].Should().Be("$.date_created");
    }

    [TestCase("\"2018-01-10T21:05:30Z\"")]
    [TestCase("1515618330")]
    [TestCase("\"Wed, 10 Jan 2018 21:05:30 +0000\"")]
    [SetCulture("ar-SA")]
    public void Dates_accept_supported_formats_independent_of_culture(string value)
    {
        var date = BcJsonUtil.Deserialize<BcDateTime>(value);
        date.Value.Should().Be(new DateTimeOffset(2018, 1, 10, 21, 5, 30, TimeSpan.Zero));
        date.ToRfc2822().Should().Be("Wed, 10 Jan 2018 21:05:30 +0000");
    }

    [Test]
    public async Task V2_arrays_and_v3_metadata_survive_response_parsing_Async()
    {
        var orders = await ReadAsync<List<BcOrderResponseFull>>("[" + Fixture("order.json") + "]");
        orders.Success.Should().BeTrue();
        orders.Data.Should().ContainSingle();
        using var response = new HttpResponseMessage(HttpStatusCode.OK)
        {
            Content = new StringContent("{\"data\":[{\"id\":1}],\"meta\":{\"count\":1.0}}", Encoding.UTF8, "application/json")
        };
        var result = await response.ReadResponseAsync<List<BcCustomer>, Dictionary<string, int>>();
        result.Success.Should().BeTrue();
        result.Meta["count"].Should().Be(1);
    }

    [Test]
    public async Task Cancellation_propagates_to_the_caller_Async()
    {
        using var cancellation = new CancellationTokenSource();
        await cancellation.CancelAsync();
        using var response = new HttpResponseMessage(HttpStatusCode.OK)
        {
            Content = new StringContent("{}", Encoding.UTF8, "application/json")
        };
        var read = () => response.ReadResponseAsync<BcCustomer, BcMetadataEmpty>(cancellation.Token);
        await read.Should().ThrowAsync<OperationCanceledException>();
    }

    [Test]
    public void Option_config_uses_distinct_upstream_field_mappings()
    {
        var config = BcJsonUtil.Deserialize<BcOptionConfig>("{\"file_max_size\":2,\"file_max_size_unit\":\"MB\",\"text_characters_limited\":true,\"text_lines_limited\":false}")!;
        config.TextCharactersLimited.Should().BeTrue();
        config.TextLinesLimited.Should().BeFalse();
    }
}
