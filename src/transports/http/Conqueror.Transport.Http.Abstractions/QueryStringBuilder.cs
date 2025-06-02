using System.Collections.Generic;
using System.Collections.Specialized;
using System.Web;

namespace Conqueror;

internal sealed class QueryStringBuilder(string? values)
{
    // trick to create an instance of the internal `HttpValueCollection`, see also here:
    // https://stackoverflow.com/a/1877016
    private readonly NameValueCollection values = HttpUtility.ParseQueryString(values ?? string.Empty);

    public static QueryStringBuilder Create(string? values = null) => new(values);

    public static string Of((string Key, string Value) first, params (string Key, string Value)[] values)
        => Create().Add(first.Key, first.Value).AddRange(values).Build()!;

    public static string Of(string values) => Create(values).Build()!;

    public static string Of(string key, string value) => Create().Add(key, value).Build()!;

    public QueryStringBuilder Add(string key, string value)
    {
        values.Add(key, value);

        return this;
    }

    public QueryStringBuilder AddRange(IEnumerable<(string Key, string Value)> source)
    {
        foreach (var (key, value) in source)
        {
            _ = Add(key, value);
        }

        return this;
    }

    public string? Build() => values.Count > 0 ? $"?{values}" : null;

    public override string? ToString() => Build();
}
