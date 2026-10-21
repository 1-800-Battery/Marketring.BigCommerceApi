using System.Collections;
using System.Reflection;
using System.Text.RegularExpressions;

namespace Fusionary.BigCommerce.Utils;

internal static partial class BcDeserializationDiagnostics
{
    // Only model property names and numeric array indexes may leave the deserializer.
    // Dictionary keys and malformed paths can contain customer-controlled text.
    internal static Dictionary<string, string> Create<TData, TMeta>(Exception exception)
    {
        var path = exception is JsonException json ? json.Path : null;
        var safePath = "$";
        var field = "(root)";
        if (!string.IsNullOrEmpty(path) && path != "$")
        {
            if (TryValidatePath(typeof(TData), path, out var dataField) ||
                TryValidatePath(typeof(TMeta), path, out dataField))
            {
                safePath = path;
                field = dataField;
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
            if (segment[0] == '[')
            {
                if (type.IsArray)
                {
                    type = type.GetElementType()!;
                }
                else if (type.IsGenericType && typeof(IEnumerable).IsAssignableFrom(type) && type.GetGenericArguments().Length == 1)
                {
                    type = type.GetGenericArguments()[0];
                }
                else
                {
                    return false;
                }
            }
            else
            {
                if (typeof(IDictionary).IsAssignableFrom(type))
                {
                    return false;
                }

                var name = segment[1..];
                var property = type.GetProperties(BindingFlags.Public | BindingFlags.Instance).FirstOrDefault(p =>
                    (p.GetCustomAttribute<JsonPropertyNameAttribute>()?.Name ?? BcJsonUtil.SnakeCaseNamingPolicy.ConvertName(p.Name)) == name);
                if (property is null || property.IsDefined(typeof(JsonExtensionDataAttribute)))
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
