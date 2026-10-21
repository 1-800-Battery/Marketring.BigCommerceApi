using System.Net;
using System.Reflection;
using System.Text;
using System.Text.Json;
using System.Text.Json.Serialization;

using Fusionary.BigCommerce.Utils;

namespace Fusionary.BigCommerce.Tests;

public class DiscountIdAndDiagnosticsTests
{
    [TestCase(HttpStatusCode.OK)]
    [TestCase(HttpStatusCode.Created)]
    public async Task Numeric_discount_ids_survive_cart_responses_Async(HttpStatusCode status)
    {
        var body = await File.ReadAllTextAsync(Path.Combine(TestContext.CurrentContext.TestDirectory, "Fixtures", "cart-numeric-discount-ids.json"));
        using var response = Response(body, status);
        var result = await response.ReadResponseAsync<BcCartResponseFull, BcMetadataEmpty>();
        result.Success.Should().BeTrue();
        result.Data.Discounts![0].Id.Should().Be("2");
        result.Data.Discounts[1].Id.Should().Be("9007199254740993");
    }

    [TestCase("2")]
    [TestCase("2.0")]
    [TestCase("9007199254740993")]
    [TestCase("9223372036854775808")]
    [TestCase("62.200000000000000001")]
    [TestCase("1.23456789012345678901234567890123456789")]
    [TestCase("1e+100")]
    [SetCulture("fr-FR")]
    public void Numeric_discount_ids_keep_exact_invariant_token_text(string number)
    {
        var discount = BcJsonUtil.Deserialize<BcCartResponseFull.Discount>("{\"id\":" + number + "}")!;
        string? id = discount.Id;
        id.Should().Be(number);
        using var written = JsonDocument.Parse(BcJsonUtil.Serialize(discount));
        written.RootElement.GetProperty("id").ValueKind.Should().Be(JsonValueKind.String);
        written.RootElement.GetProperty("id").GetString().Should().Be(number);
    }

    [TestCase("{\"id\":\"coupon\"}", "coupon")]
    [TestCase("{\"id\":\"0002\"}", "0002")]
    [TestCase("{\"id\":null}", null)]
    [TestCase("{}", null)]
    public void String_and_missing_discount_ids_keep_their_contract(string json, string? expected)
    {
        BcJsonUtil.Deserialize<BcCartResponseFull.Discount>(json)!.Id.Should().Be(expected);
    }

    [TestCase("true")]
    [TestCase("{}")]
    [TestCase("[]")]
    public void Unsupported_discount_id_shapes_are_rejected(string value)
    {
        var read = () => BcJsonUtil.Deserialize<BcCartResponseFull.Discount>("{\"id\":" + value + "}");
        read.Should().Throw<JsonException>();
    }

    [TestCase(typeof(IDictionary<string, int>), "$.item")]
    [TestCase(typeof(IDictionary<string, int>), "$.keys")]
    [TestCase(typeof(IReadOnlyDictionary<string, int>), "$.values")]
    [TestCase(typeof(Dictionary<string, int>), "$.count")]
    [TestCase(typeof(List<int>), "$.capacity")]
    [TestCase(typeof(List<int>), "$.count")]
    [TestCase(typeof(IList<int>), "$.item")]
    [TestCase(typeof(int[]), "$.length")]
    [TestCase(typeof(string), "$.length")]
    [TestCase(typeof(DateTime), "$.year")]
    [TestCase(typeof(BcCartResponseFull), "$.email.length")]
    [TestCase(typeof(UnsafeMembers), "$.private_index")]
    [TestCase(typeof(UnsafeMembers), "$.ignored")]
    [TestCase(typeof(UnsafeMembers), "$.attributes.item")]
    [TestCase(typeof(BcOptionConfig), "$.extension_data")]
    [TestCase(typeof(BcCartPost), "$.extension_data.item")]
    [TestCase(typeof(BcCartLineItem), "$.extension_data")]
    [TestCase(typeof(BcCartLineItems), "$.extension_data")]
    [TestCase(typeof(BcBrandPost), "$.extension_data")]
    [TestCase(typeof(BcCategoryPost), "$.extension_data")]
    [TestCase(typeof(BcProductModifierPost), "$.extension_data")]
    public void Unsafe_diagnostic_paths_are_redacted(Type target, string path)
    {
        var details = Diagnose(target, path);
        details["JsonPath"].Should().Be("$[redacted]");
        details["Field"].Should().Be("(redacted)");
        details["Exception"].Should().Be("JsonException at $[redacted]");
        BcJsonUtil.Serialize(details).Should().NotContain("private@example.invalid");
    }

    [TestCase(typeof(BcCartResponseFull), "$.line_items.physical_items[0].quantity", "quantity")]
    [TestCase(typeof(List<BcCartResponseFull>), "$[0].discounts[1].discounted_amount", "discounted_amount")]
    [TestCase(typeof(IReadOnlyList<BcCartResponseFull>), "$[0].customer_id", "customer_id")]
    public void Known_model_paths_remain_useful(Type target, string path, string field)
    {
        var details = Diagnose(target, path);
        details["JsonPath"].Should().Be(path);
        details["Field"].Should().Be(field);
    }

    [TestCase("item")]
    [TestCase("keys")]
    [TestCase("values")]
    public async Task Generic_dictionary_keys_are_redacted_in_real_failures_Async(string key)
    {
        using var response = Response("{\"" + key + "\":\"private@example.invalid\"}");
        var result = await response.ReadResponseAsync<IDictionary<string, int>, BcMetadataEmpty>();
        result.Success.Should().BeFalse();
        result.Error.ErrorDetails["JsonPath"].Should().Be("$[redacted]");
        result.Error.ErrorDetails["Field"].Should().Be("(redacted)");
    }

    [Test]
    public async Task Data_keys_cannot_be_validated_against_the_metadata_type_Async()
    {
        using var response = Response("{\"data\":{\"id\":\"private@example.invalid\"},\"meta\":{}}");
        var result = await response.ReadResponseAsync<IDictionary<string, int>, BcCustomer>();
        result.Success.Should().BeFalse();
        result.Error.ErrorDetails["JsonPath"].Should().Be("$[redacted]");
    }

    [Test]
    public async Task Metadata_keys_cannot_be_validated_against_the_data_type_Async()
    {
        using var response = Response("{\"data\":{},\"meta\":{\"id\":\"private@example.invalid\"}}");
        var result = await response.ReadResponseAsync<BcCustomer, IReadOnlyDictionary<string, int>>();
        result.Success.Should().BeFalse();
        result.Error.ErrorDetails["JsonPath"].Should().Be("$[redacted]");
    }

    [Test]
    public async Task Known_metadata_fields_keep_their_diagnostic_path_Async()
    {
        using var response = Response("{\"data\":{},\"meta\":{\"id\":\"private@example.invalid\"}}");
        var result = await response.ReadResponseAsync<BcCartResponseFull, BcCustomer>();
        result.Success.Should().BeFalse();
        result.Error.ErrorDetails["JsonPath"].Should().Be("$.id");
        result.Error.ErrorDetails["Field"].Should().Be("id");
    }

    private static HttpResponseMessage Response(string body, HttpStatusCode status = HttpStatusCode.OK) => new(status)
    {
        Content = new StringContent(body, Encoding.UTF8, "application/json")
    };

    // Exercise validation of paths supplied by converters, including paths STJ normally never emits.
    private static Dictionary<string, string> Diagnose(Type target, string path)
    {
        var diagnostics = typeof(BcJsonUtil).Assembly.GetType("Fusionary.BigCommerce.Utils.BcDeserializationDiagnostics")!;
        var method = diagnostics.GetMethod("Create", BindingFlags.Static | BindingFlags.NonPublic)!;
        return (Dictionary<string, string>)method.Invoke(null, [new JsonException("private@example.invalid", path, null, null), target])!;
    }

    public class UnsafeMembers
    {
        [JsonPropertyName("private_index")]
        public int this[string key] => key.Length;

        [JsonIgnore]
        public int Ignored { get; set; }

        public IDictionary<string, int>? Attributes { get; set; }
    }
}
