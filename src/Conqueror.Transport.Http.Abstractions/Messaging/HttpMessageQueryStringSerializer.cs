using System;
using System.Collections;
using System.Collections.Generic;
using System.Globalization;
using System.IO;
using System.Linq;
using System.Net.Http;
using System.Reflection;
using System.Text;
using System.Threading;
using System.Threading.Tasks;

// ReSharper disable once CheckNamespace
namespace Conqueror;

public sealed class HttpMessageQueryStringSerializer<TMessage, TResponse> : IHttpMessageSerializer<TMessage, TResponse>
    where TMessage : class, IHttpMessage<TMessage, TResponse>
{
    public string? ContentType => null;

    public async Task<(HttpContent? Content, string? Path, string? QueryString)> Serialize(IServiceProvider serviceProvider, TMessage message, CancellationToken cancellationToken)
    {
        var props = TMessage.PublicProperties.ToList();
        if (props.Count == 0)
        {
            return (null, null, string.Empty);
        }

        await Task.CompletedTask.ConfigureAwait(false);

        var uriBuilder = new StringBuilder();
        var isFirst = true;

        foreach (var prop in TMessage.PublicProperties)
        {
            var propName = Uncapitalize(prop.Name);
            var propValue = prop.GetValue(message);

            if (propValue is IEnumerable enumerable)
            {
                foreach (var v in enumerable)
                {
                    _ = uriBuilder.Append(isFirst ? '?' : '&')
                                  .Append(propName)
                                  .Append('=')
                                  .Append(Uri.EscapeDataString(v?.ToString() ?? string.Empty));

                    isFirst = false;
                }

                continue;
            }

            _ = uriBuilder.Append(isFirst ? '?' : '&')
                          .Append(propName)
                          .Append('=')
                          .Append(Uri.EscapeDataString(propValue?.ToString() ?? string.Empty));

            isFirst = false;
        }

        return (null, null, uriBuilder.ToString());
    }

    public async Task<TMessage> Deserialize(IServiceProvider serviceProvider,
                                            Stream body,
                                            string path,
                                            IReadOnlyDictionary<string, IReadOnlyList<string?>>? query, CancellationToken cancellationToken)
    {
        await Task.CompletedTask.ConfigureAwait(false);

        if (query is null)
        {
            throw new ArgumentException("query must not be null", nameof(query));
        }

        if (TMessage.EmptyInstance is not null)
        {
            return TMessage.EmptyInstance;
        }

        if (FindMatchingConstructor(query) is { } constructor)
        {
            var parameters = BuildConstructorParameters(constructor, query);
            return constructor.Invoke(parameters) as TMessage
                   ?? throw new InvalidOperationException($"failed to invoke constructor for message type '{typeof(TMessage)}'");
        }

        var parameterlessConstructor = TMessage.PublicConstructors.FirstOrDefault(c => c.GetParameters().Length == 0);

        if (parameterlessConstructor is null)
        {
            throw new InvalidOperationException($"could not find a matching constructor or public parameterless constructor for message type '{typeof(TMessage)}'");
        }

        var message = parameterlessConstructor.Invoke([]) as TMessage
                      ?? throw new InvalidOperationException($"failed to invoke parameterless constructor for message type '{typeof(TMessage)}'");

        foreach (var prop in TMessage.PublicProperties)
        {
            var values = query.GetValueOrDefault(Uncapitalize(prop.Name)) ?? query.GetValueOrDefault(prop.Name);

            if (values is null || values.Count == 0)
            {
                continue;
            }

            if (IsCollectionType(prop.PropertyType))
            {
                var collectionValues = ConvertCollection(values, prop.PropertyType);
                if (collectionValues != null)
                {
                    prop.SetValue(message, collectionValues);
                }

                continue;
            }

            var value = values[0];
            if (value is null)
            {
                continue;
            }

            var convertedValue = ConvertValue(value, prop.PropertyType);
            if (convertedValue != null)
            {
                prop.SetValue(message, convertedValue);
            }
        }

        return message;
    }

    private static ConstructorInfo? FindMatchingConstructor(IReadOnlyDictionary<string, IReadOnlyList<string?>> query)
    {
        var queryParamNames = new HashSet<string>(query.Keys, StringComparer.OrdinalIgnoreCase);

        return TMessage.PublicConstructors
                       .Where(c => c.GetParameters().Length > 0)
                       .Select(c => new
                       {
                           Constructor = c,
                           Parameters = c.GetParameters(),
                           MatchCount = c.GetParameters().Count(p => (p.Name is not null && queryParamNames.Contains(p.Name)) || p.HasDefaultValue),
                       })
                       .FirstOrDefault(x => x.MatchCount >= queryParamNames.Count)
                       ?.Constructor;
    }

    private static object?[] BuildConstructorParameters(ConstructorInfo constructor,
                                                        IReadOnlyDictionary<string, IReadOnlyList<string?>> query)
    {
        var parameters = constructor.GetParameters();
        var paramValues = new object?[parameters.Length];

        for (var i = 0; i < parameters.Length; i++)
        {
            var param = parameters[i];

            if (param.Name is not null && (
                    query.TryGetValue(param.Name, out var values) || (query.TryGetValue(Uncapitalize(param.Name), out values)
                                                                      && values.Count > 0)))
            {
                if (IsCollectionType(param.ParameterType))
                {
                    paramValues[i] = ConvertCollection(values, param.ParameterType);
                }
                else
                {
                    var value = values[0];
                    if (value != null)
                    {
                        paramValues[i] = ConvertValue(value, param.ParameterType);
                    }
                    else if (param.HasDefaultValue)
                    {
                        paramValues[i] = param.DefaultValue;
                    }
                }
            }
            else if (param.HasDefaultValue)
            {
                // If parameter not in query but has default value, use it
                paramValues[i] = param.DefaultValue;
            }
        }

        return paramValues;
    }

    private static bool IsCollectionType(Type type)
    {
        // Check if it's an array
        if (type.IsArray)
        {
            return true;
        }

        // Check if it's a generic collection
        if (type.IsGenericType)
        {
            var genericTypeDefinition = type.GetGenericTypeDefinition();

            // Check for common collection interfaces and types
            if (genericTypeDefinition == typeof(IEnumerable<>) ||
                genericTypeDefinition == typeof(ICollection<>) ||
                genericTypeDefinition == typeof(IList<>) ||
                genericTypeDefinition == typeof(List<>) ||
                genericTypeDefinition == typeof(IReadOnlyCollection<>) ||
                genericTypeDefinition == typeof(IReadOnlyList<>))
            {
                return true;
            }
        }

        return false;
    }

    private static object? ConvertValue(string value, Type targetType)
    {
        if (string.IsNullOrEmpty(value))
        {
            return null;
        }

        if (targetType == typeof(string))
        {
            return value;
        }

        // Handle nullable types
        var underlyingType = Nullable.GetUnderlyingType(targetType) ?? targetType;

        if (underlyingType == typeof(int))
        {
            if (int.TryParse(value, out var result))
            {
                return result;
            }
        }
        else if (underlyingType == typeof(long))
        {
            if (long.TryParse(value, out var result))
            {
                return result;
            }
        }
        else if (underlyingType == typeof(double))
        {
            if (double.TryParse(value, NumberStyles.Any, CultureInfo.InvariantCulture, out var result))
            {
                return result;
            }
        }
        else if (underlyingType == typeof(decimal))
        {
            if (decimal.TryParse(value, NumberStyles.Any, CultureInfo.InvariantCulture, out var result))
            {
                return result;
            }
        }
        else if (underlyingType == typeof(bool))
        {
            if (bool.TryParse(value, out var result))
            {
                return result;
            }
        }
        else if (underlyingType == typeof(DateTime))
        {
            if (DateTime.TryParse(value, CultureInfo.InvariantCulture, DateTimeStyles.None, out var result))
            {
                return result;
            }
        }
        else if (underlyingType == typeof(Guid))
        {
            if (Guid.TryParse(value, out var result))
            {
                return result;
            }
        }
        else if (underlyingType.IsEnum)
        {
            return Enum.TryParse(underlyingType, value, true, out var result) ? result : null;
        }

        throw new InvalidOperationException($"unable to convert value '{value}' to type '{targetType.FullName}'");
    }

    private static object? ConvertCollection(IReadOnlyList<string?> values, Type collectionType)
    {
        // Extract item type from the collection type
        Type itemType;

        if (collectionType.IsArray)
        {
            itemType = collectionType.GetElementType()!;
        }
        else if (collectionType.IsGenericType)
        {
            itemType = collectionType.GetGenericArguments()[0];
        }
        else
        {
            return null; // Not a supported collection type
        }

        // Filter out null values
        var filteredValues = values.Where(v => v != null).ToList();

        // Handle arrays in AOT-compatible way by handling specific types directly
        if (collectionType.IsArray)
        {
            if (itemType == typeof(string))
            {
                return CreateArray<string>(filteredValues, itemType);
            }

            if (itemType == typeof(int))
            {
                return CreateArray<int>(filteredValues, itemType);
            }

            if (itemType == typeof(long))
            {
                return CreateArray<long>(filteredValues, itemType);
            }

            if (itemType == typeof(double))
            {
                return CreateArray<double>(filteredValues, itemType);
            }

            if (itemType == typeof(decimal))
            {
                return CreateArray<decimal>(filteredValues, itemType);
            }

            if (itemType == typeof(bool))
            {
                return CreateArray<bool>(filteredValues, itemType);
            }

            if (itemType == typeof(DateTime))
            {
                return CreateArray<DateTime>(filteredValues, itemType);
            }

            if (itemType == typeof(Guid))
            {
                return CreateArray<Guid>(filteredValues, itemType);
            }

            if (itemType.IsEnum)
            {
                // For enum arrays, convert to object[] as a fallback
                var objectArray = new object[filteredValues.Count];
                for (var i = 0; i < filteredValues.Count; i++)
                {
                    var convertedValue = ConvertValue(filteredValues[i]!, itemType);
                    if (convertedValue != null)
                    {
                        objectArray[i] = convertedValue;
                    }
                }

                return objectArray;
            }

            // Fallback to List<object> for unsupported array types
            return filteredValues
                   .Select(v => ConvertValue(v!, itemType))
                   .Where(v => v != null)
                   .ToList();
        }

        // Handle generic collections
        if (collectionType.IsGenericType)
        {
            var genericTypeDef = collectionType.GetGenericTypeDefinition();

            // For all supported collection types, return a List<T>
            if (genericTypeDef == typeof(List<>) ||
                genericTypeDef == typeof(IList<>) ||
                genericTypeDef == typeof(ICollection<>) ||
                genericTypeDef == typeof(IEnumerable<>) ||
                genericTypeDef == typeof(IReadOnlyCollection<>) ||
                genericTypeDef == typeof(IReadOnlyList<>))
            {
                // Handle specific element types directly using generic methods
                if (itemType == typeof(string))
                {
                    return CreateTypedList<string>(filteredValues, itemType);
                }

                if (itemType == typeof(int))
                {
                    return CreateTypedList<int>(filteredValues, itemType);
                }

                if (itemType == typeof(long))
                {
                    return CreateTypedList<long>(filteredValues, itemType);
                }

                if (itemType == typeof(double))
                {
                    return CreateTypedList<double>(filteredValues, itemType);
                }

                if (itemType == typeof(decimal))
                {
                    return CreateTypedList<decimal>(filteredValues, itemType);
                }

                if (itemType == typeof(bool))
                {
                    return CreateTypedList<bool>(filteredValues, itemType);
                }

                if (itemType == typeof(DateTime))
                {
                    return CreateTypedList<DateTime>(filteredValues, itemType);
                }

                if (itemType == typeof(Guid))
                {
                    return CreateTypedList<Guid>(filteredValues, itemType);
                }

                if (itemType.IsEnum)
                {
                    // For enums, we still need to use some reflection
                    // but we minimize it and make it more AOT-friendly
                    var list = new List<object>();
                    foreach (var value in filteredValues)
                    {
                        var convertedValue = ConvertValue(value!, itemType);
                        if (convertedValue != null)
                        {
                            list.Add(convertedValue);
                        }
                    }

                    return list;
                }
            }
        }

        return null;
    }

    private static T[] CreateArray<T>(IReadOnlyList<string?> values, Type itemType)
    {
        var array = new T[values.Count];
        var validCount = 0;

        foreach (var t in values.OfType<string>())
        {
            var convertedValue = ConvertValue(t, itemType);
            if (convertedValue != null)
            {
                array[validCount++] = (T)convertedValue;
            }
        }

        if (validCount < values.Count)
        {
            // If we had null values, resize the array
            var resizedArray = new T[validCount];
            Array.Copy(array, resizedArray, validCount);
            return resizedArray;
        }

        return array;
    }

    private static List<T> CreateTypedList<T>(IReadOnlyList<string?> values, Type itemType)
        => values.OfType<string>()
                 .Select(value => ConvertValue(value, itemType))
                 .OfType<T>()
                 .ToList();

    private static string Uncapitalize(string str)
        => char.ToLower(str[0], CultureInfo.InvariantCulture) + str[1..];
}
