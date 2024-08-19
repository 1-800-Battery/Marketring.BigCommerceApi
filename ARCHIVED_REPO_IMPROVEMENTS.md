# Archived Repository Improvements Analysis

This document outlines the improvements found in commit `8952dec "Fix product Modifiers"` from the archived repository (https://github.com/richardkeller411/Fusionary.BigCommerceApi) that need to be implemented in the new project structure to maintain compatibility for users migrating from the archived repository.

## Summary of Key Improvements

The archived repository had several important enhancements to product modifier and option handling that users would expect to work. The main improvements fall into these categories:

1. **Enhanced Type Definitions**: More complete properties matching BigCommerce API
2. **Better JSON Handling**: Custom converters for edge cases
3. **Bug Fixes**: Missing enum values and property types
4. **Additional Test Coverage**: Product modifier tests

## Detailed Changes Needed

### 1. BcNumberLimitMode Enum Enhancement

**Issue**: Missing `None` enum value for empty string cases

**Current Code**:
```csharp
[JsonConverter(typeof(JsonStringEnumConverter))]
public enum BcNumberLimitMode
{
    [JsonPropertyName("lowest")]
    Lowest,

    [JsonPropertyName("highest")]
    Highest,

    [JsonPropertyName("range")]
    Range
}
```

**Required Addition**:
```csharp
[JsonConverter(typeof(JsonStringEnumConverter))]
public enum BcNumberLimitMode
{
    [JsonPropertyName("lowest")]
    Lowest,

    [JsonPropertyName("highest")]
    Highest,

    [JsonPropertyName("range")]
    Range,

    [JsonPropertyName("")]
    None  // Handle empty string case from BigCommerce API
}
```

### 2. BcProductOptionValue Type Enhancement

**Issue**: Missing properties and incorrect ValueData typing

**Current Code**:
```csharp
public record BcProductOptionValue : BcExtensionData
{
    // ... existing properties ...
    public Dictionary<string, string>? ValueData { get; set; }
}
```

**Required Enhancement**:
```csharp
public record BcProductOptionValue : BcExtensionData
{
    /// <summary>
    /// The unique numerical ID of the option value.
    /// </summary>
    [JsonPropertyName("id")]
    public int Id { get; set; }

    /// <summary>
    /// The unique numerical ID of the option.
    /// </summary>
    [JsonPropertyName("option_id")]
    public int OptionId { get; set; }

    /// <summary>
    /// The text display identifying the value on the storefront.
    /// </summary>
    [JsonPropertyName("label")]
    public required string Label { get; set; }

    /// <summary>
    /// The order in which the value will be displayed on the product page.
    /// </summary>
    [JsonPropertyName("sort_order")]
    public required int SortOrder { get; set; }

    /// <summary>
    /// The flag for preselecting a value as the default on the storefront.
    /// </summary>
    [JsonPropertyName("is_default")]
    public bool IsDefault { get; set; }

    /// <summary>
    /// Extra data describing the value, based on the type of option or modifier with which the value is associated.
    /// </summary>
    [JsonPropertyName("value_data")]
    public Dictionary<string, object>? ValueData { get; set; }  // Changed from string to object
}
```

### 3. BcOptionConfig Enhancement with Custom JSON Converter

**Issue**: The archived repo had a comprehensive BcOptionConfig with a custom JSON converter for handling string/number conversion

**Required Addition**:
```csharp
public record BcOptionConfig : BcExtensionData
{
    // ... existing properties ...

    /// <summary>
    /// The type of limit on values entered for a number option.
    /// Uses custom converter to handle both string and number values from API.
    /// </summary>
    [JsonPropertyName("number_limit_mode")]
    [JsonConverter(typeof(StringOrNumberConverter))]
    public string? NumberLimitMode { get; set; }

    // Add the custom converter class
    public class StringOrNumberConverter : JsonConverter<string?>
    {
        public override string? Read(ref Utf8JsonReader reader, Type typeToConvert, JsonSerializerOptions options)
        {
            // Check if the JSON token is null
            if (reader.TokenType == JsonTokenType.Null)
            {
                return null; // Return null if the token is null
            }
            // Check if the JSON token is a number
            if (reader.TokenType == JsonTokenType.Number)
            {
                // Convert the number to a string
                return reader.GetInt64().ToString();
            }
            // Check if the JSON token is a string
            else if (reader.TokenType == JsonTokenType.String)
            {
                return reader.GetString();
            }

            throw new JsonException("Expected number, string, or null");
        }

        public override void Write(Utf8JsonWriter writer, string? value, JsonSerializerOptions options)
        {
            if (value == null)
            {
                writer.WriteNullValue(); // Write a null value if the string is null
            }
            else
            {
                writer.WriteStringValue(value); // Otherwise, write the string value
            }
        }
    }
}
```

### 4. BcModifierOptionValue Missing Properties

**Issue**: Missing OptionId property

**Current Code**:
```csharp
public record BcModifierOptionValue : BcProductOptionValue
{
    public BcAdjusters? Adjusters { get; set; }
}
```

**Required Enhancement**:
```csharp
public record BcModifierOptionValue : BcProductOptionValue
{
    /// <summary>
    /// The unique numerical ID of the option.
    /// </summary>
    [JsonPropertyName("option_id")]
    public int OptionId { get; set; }

    public BcAdjusters? Adjusters { get; set; }
}
```

### 5. Additional Test Coverage

**Required Addition**: Add product modifier test to ensure the functionality works

**File**: `Fusionary.BigCommerce.Tests/OrderTests.cs` or new dedicated test file

```csharp
[Test]
public async Task Can_Get_Product_Modifiers_Async()
{
    var bc = Services.GetRequiredService<IBcApi>();
    var cancellationToken = CancellationToken.None;

    var result = await bc.Catalog().ProductModifiers().GetAll().SendAsync(363681, cancellationToken);

    DumpObject(result);

    Assert.NotNull(result);
    Assert.True(result.Success);

    if (result.HasData)
    {
        foreach (var modifier in result.Data)
        {
            DumpObject(modifier);
        }
    }
}
```

## Implementation Priority

### High Priority (Breaking Changes for Migrating Users)
1. **BcNumberLimitMode.None** - Users may encounter serialization errors without this
2. **BcProductOptionValue.ValueData typing** - Dictionary<string, object> vs Dictionary<string, string> 
3. **Missing properties** - IsDefault, OptionId properties

### Medium Priority (Enhanced Functionality)
1. **StringOrNumberConverter** - Better handling of API inconsistencies
2. **Additional JSON property names** - More complete API coverage

### Low Priority (Nice to Have)
1. **Additional test coverage** - Ensures functionality works as expected

## Migration Notes for Users

Users migrating from the archived repository should be aware that:

1. **ValueData typing changed**: If they were casting to specific string types, they may need to update to object types
2. **New properties available**: IsDefault and other properties are now available
3. **Better enum handling**: None value now properly handles empty string cases from BigCommerce API
4. **Improved JSON serialization**: More robust handling of edge cases

## Implementation Strategy

1. **Phase 1**: Implement high-priority changes to ensure compatibility
2. **Phase 2**: Add custom JSON converters for robustness  
3. **Phase 3**: Add comprehensive test coverage
4. **Phase 4**: Update documentation with migration notes

This approach ensures that users can migrate from the archived repository without breaking changes while gaining access to the improved functionality.