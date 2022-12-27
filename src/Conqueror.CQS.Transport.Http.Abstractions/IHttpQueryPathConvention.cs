using System;

namespace Conqueror
{
    public interface IHttpQueryPathConvention
    {
        public string? GetQueryPath(Type queryType, HttpQueryAttribute attribute);
    }
}
