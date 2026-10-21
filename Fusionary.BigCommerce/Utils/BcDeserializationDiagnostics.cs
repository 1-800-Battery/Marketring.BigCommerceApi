using System.Collections;
using System.Reflection;
using System.Text.Json.Serialization.Metadata;
using System.Text.RegularExpressions;

namespace Fusionary.BigCommerce.Utils;

internal static partial class BcDeserializationDiagnostics
{
    private static readonly DefaultJsonTypeInfoResolver DefaultResolver = new();

    // Only JSON contract properties and numeric collection indexes may leave the deserializer.
    // Validate against the type that actually failed, never an unrelated data/meta contract.
    internal static Dictionary<string, string> Create(Exception exception, Type? targetType)
    {
        var path = exception is JsonException json ? json.Path : null;
        var safePath = "$";
        var field = "(root)";
        if (!string.IsNullOrEmpty(path) && path != "$")
        {
            if (targetType is not null && TryValidatePath(targetType, path, out var modelField))
            {
                safePath = path;
                field = modelField;
            }
            else
            {
                safePath = "$[redacted]";
                field = "(redacted)";
            }
        }

        return new Dictionary<string, string>
        {
            ["Exception"] = $"{exception.GetType().Name} at {safePath}",
            ["JsonPath"] = safePath,
            ["Field"] = field
        };
    }

    private static bool IsDictionary(Type type) =>
        typeof(IDictionary).IsAssignableFrom(type) || type.GetInterfaces().Append(type).Any(candidate =>
            candidate.IsGenericType &&
            (candidate.GetGenericTypeDefinition() == typeof(IDictionary<,>) ||
             candidate.GetGenericTypeDefinition() == typeof(IReadOnlyDictionary<,>)));

    private static bool TryValidatePath(Type type, string path, out string field)
    {
        field = "(root)";
        if (path.Length > 256 || !JsonPathPattern().IsMatch(path))
        {
            return false;
        }

        foreach (var segment in PathSegmentPattern().Matches(path).Select(match => match.Value))
        {
            type = Nullable.GetUnderlyingType(type) ?? type;
            if (IsDictionary(type))
            {
                return false;
            }

            var options = BcJsonUtil.JsonOptions;
            var contract = (options.TypeInfoResolver ?? DefaultResolver).GetTypeInfo(type, options);
            if (segment[0] == '[')
            {
                if (contract?.Kind != JsonTypeInfoKind.Enumerable)
                {
                    return false;
                }

                var elementType = type.IsArray
                    ? type.GetElementType()
                    : type.GetInterfaces().Append(type)
                        .FirstOrDefault(candidate => candidate.IsGenericType && candidate.GetGenericTypeDefinition() == typeof(IEnumerable<>))
                        ?.GetGenericArguments()[0];
                if (elementType is null)
                {
                    return false;
                }

                type = elementType;
            }
            else
            {
                if (contract?.Kind != JsonTypeInfoKind.Object)
                {
                    return false;
                }

                var name = segment[1..];
                var property = contract.Properties.FirstOrDefault(candidate => candidate.Name == name);
                if (property is null || property.IsExtensionData ||
                    property.Get is null && property.Set is null ||
                    property.AttributeProvider is not PropertyInfo member || member.GetIndexParameters().Length != 0 ||
                    typeof(IBcExtensionData).IsAssignableFrom(type) && member.Name == nameof(IBcExtensionData.ExtensionData))
                {
                    return false;
                }

                type = property.PropertyType;
                field = name;
            }
        }

        return true;
    }

    [GeneratedRegex(@"^\$(?:\.[A-Za-z_][A-Za-z0-9_]*|\[[0-9]{1,10}\])+$", RegexOptions.CultureInvariant)]
    private static partial Regex JsonPathPattern();

    [GeneratedRegex(@"\.[A-Za-z_][A-Za-z0-9_]*|\[[0-9]{1,10}\]", RegexOptions.CultureInvariant)]
    private static partial Regex PathSegmentPattern();
}
