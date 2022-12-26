using System;
using System.Text.RegularExpressions;

namespace Conqueror
{
    public sealed class DefaultHttpQueryPathConvention : IHttpQueryPathConvention
    {
        private static readonly Regex StripSuffixRegex = new("Query$");

        public string GetQueryPath(Type queryType, HttpQueryAttribute attribute)
        {
            if (attribute.Path != null)
            {
                return attribute.Path;
            }

            return $"/api/{(attribute.Version > 0 ? $"v{attribute.Version}/" : string.Empty)}queries/{StripSuffixRegex.Replace(queryType.Name, string.Empty)}";
        }
    }
}
