using System;
using System.Text.RegularExpressions;

namespace Conqueror
{
    public sealed class DefaultHttpQueryPathConvention : IHttpQueryPathConvention
    {
        public string GetQueryPath(Type queryType, HttpQueryAttribute attribute)
        {
            var regex = new Regex("Query$");
            var path = $"/api/queries/{regex.Replace(queryType.Name, string.Empty)}";
            return path;
        }
    }
}
