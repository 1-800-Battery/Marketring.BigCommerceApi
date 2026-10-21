using System.Globalization;
using System.Net;
using System.Text;
using System.Text.Json;

using Fusionary.BigCommerce.Utils;

namespace Fusionary.BigCommerce.Tests;

public class SchemaCompatibilityTests
{
    private static async Task<T> ReadFixtureAsync<T>(string name)
    {
        var body = await File.ReadAllTextAsync(Path.Combine(TestContext.CurrentContext.TestDirectory, "Fixtures", name));
        using var response = new HttpResponseMessage(HttpStatusCode.OK)
        {
            Content = new StringContent(body, Encoding.UTF8, "application/json")
        };
        var result = await response.ReadResponseAsync<T, BcMetadataEmpty>();
        result.Success.Should().BeTrue();
        return result.Data;
    }

    [Test]
    public void Live_cart_fixture_requires_explicit_opt_in()
    {
        typeof(CartSimpleOperationsTest).IsDefined(typeof(ExplicitAttribute), inherit: true).Should().BeTrue();
    }

    [Test]
    public async Task Cart_fixture_supports_both_coupon_shapes_and_object_banners_Async()
    {
        var cart = await ReadFixtureAsync<BcCartResponseFull>("cart-promotions.json");
        var items = cart.LineItems!.PhysicalItems!;
        ((JsonElement)items[0].Coupons!.Single()).GetDecimal().Should().Be(6.25m);
        ((JsonElement)items[1].Coupons!.Single()).GetProperty("totalDiscount").GetDecimal().Should().Be(1.8m);
        ((JsonElement)cart.Promotions!.Banners!.Single()).GetProperty("page")[1].GetString().Should().Be("checkout");
    }

    [TestCase("0")]
    [TestCase("0.0")]
    [TestCase("62.200000000000000001")]
    public void Scalar_coupons_preserve_the_number_and_wire_shape(string number)
    {
        var item = BcJsonUtil.Deserialize<BcCartResponseFull.PhysicalItem>("{\"coupons\":" + number + "}")!;
        List<object> coupons = item.Coupons!;
        coupons.Should().ContainSingle();
        ((JsonElement)coupons[0]).GetDecimal().Should().Be(decimal.Parse(number, CultureInfo.InvariantCulture));
        using var written = JsonDocument.Parse(BcJsonUtil.Serialize(item));
        var value = written.RootElement.GetProperty("coupons");
        value.ValueKind.Should().Be(JsonValueKind.Number);
        value.GetRawText().Should().Be(number);
    }

    [TestCase("[]")]
    [TestCase("[{\"code\":\"EXAMPLE\",\"totalDiscount\":1.8}]")]
    public void Coupon_arrays_keep_the_public_list_and_wire_shape(string json)
    {
        var item = BcJsonUtil.Deserialize<BcCartResponseFull.PhysicalItem>("{\"coupons\":" + json + "}")!;
        using var written = JsonDocument.Parse(BcJsonUtil.Serialize(item));
        written.RootElement.GetProperty("coupons").GetRawText().Should().Be(json);
        item.Coupons!.Add(new { code = "SECOND" });
        using var updated = JsonDocument.Parse(BcJsonUtil.Serialize(item));
        updated.RootElement.GetProperty("coupons").GetArrayLength().Should().Be(item.Coupons.Count);
    }

    [TestCase("{}")]
    [TestCase("{\"id\":\"example\",\"text\":\"Example\",\"page\":[\"cart\"]}")]
    [TestCase("[]")]
    [TestCase("[{\"id\":\"example\"}]")]
    public void Banners_preserve_object_and_legacy_array_shapes(string json)
    {
        var promotions = BcJsonUtil.Deserialize<BcCartResponseFull.PromotionsClass>("{\"banners\":" + json + "}")!;
        List<object> banners = promotions.Banners!;
        banners.Should().NotBeNull();
        using var written = JsonDocument.Parse(BcJsonUtil.Serialize(promotions));
        written.RootElement.GetProperty("banners").GetRawText().Should().Be(json);
    }

    [Test]
    public void Replacing_or_growing_normalized_lists_writes_arrays()
    {
        var item = BcJsonUtil.Deserialize<BcCartResponseFull.PhysicalItem>("{\"coupons\":0}")!;
        item.Coupons = [new { code = "EXAMPLE" }];
        using var itemJson = JsonDocument.Parse(BcJsonUtil.Serialize(item));
        itemJson.RootElement.GetProperty("coupons").ValueKind.Should().Be(JsonValueKind.Array);
        var promotions = BcJsonUtil.Deserialize<BcCartResponseFull.PromotionsClass>("{\"banners\":{\"id\":\"example\"}}")!;
        promotions.Banners!.Add(new { id = "second" });
        using var promotionsJson = JsonDocument.Parse(BcJsonUtil.Serialize(promotions));
        promotionsJson.RootElement.GetProperty("banners").GetArrayLength().Should().Be(2);
    }

    [TestCase("{}")]
    [TestCase("{\"coupons\":null,\"banners\":null}")]
    public void Missing_or_null_optional_collections_remain_null(string json)
    {
        BcJsonUtil.Deserialize<BcCartResponseFull.PhysicalItem>(json)!.Coupons.Should().BeNull();
        BcJsonUtil.Deserialize<BcCartResponseFull.PromotionsClass>(json)!.Banners.Should().BeNull();
    }

    [TestCase("true")]
    [TestCase("\"invalid\"")]
    public void Unsupported_collection_shapes_still_fail(string json)
    {
        var readCoupons = () => BcJsonUtil.Deserialize<BcCartResponseFull.PhysicalItem>("{\"coupons\":" + json + "}");
        readCoupons.Should().Throw<JsonException>();
        var readBanners = () => BcJsonUtil.Deserialize<BcCartResponseFull.PromotionsClass>("{\"banners\":" + json + "}");
        readBanners.Should().Throw<JsonException>();
    }

    [Test, SetCulture("fr-FR")]
    public async Task Modifier_fixture_retains_decimal_bounds_Async()
    {
        var modifiers = await ReadFixtureAsync<List<BcProductModifier>>("modifiers.json");
        var config = modifiers.Single().Config!;
        Convert.ToDecimal(config.NumberLowestValue).Should().Be(-1.25m);
        Convert.ToDecimal(config.NumberHighestValue).Should().Be(62.20000000000001m);
    }

    [TestCase("0")]
    [TestCase("1.8")]
    [TestCase("\"62.200000000000000001\"")]
    public void Modifier_bounds_accept_integer_decimal_and_quoted_values(string number)
    {
        var config = BcJsonUtil.Deserialize<BcOptionConfig>("{\"number_lowest_value\":" + number + ",\"number_highest_value\":" + number + "}")!;
        var expected = decimal.Parse(number.Trim('"'), CultureInfo.InvariantCulture);
        Convert.ToDecimal(config.NumberLowestValue).Should().Be(expected);
        Convert.ToDecimal(config.NumberHighestValue).Should().Be(expected);
        using var written = JsonDocument.Parse(BcJsonUtil.Serialize(config));
        written.RootElement.GetProperty("number_lowest_value").GetDecimal().Should().Be(expected);
        written.RootElement.GetProperty("number_highest_value").GetDecimal().Should().Be(expected);
    }

    [TestCase("{}")]
    [TestCase("{\"number_lowest_value\":null,\"number_highest_value\":null}")]
    public void Optional_modifier_bounds_remain_nullable(string json)
    {
        var config = BcJsonUtil.Deserialize<BcOptionConfig>(json)!;
        config.NumberLowestValue.Should().BeNull();
        config.NumberHighestValue.Should().BeNull();
    }
}
