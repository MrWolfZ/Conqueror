using System;

namespace Conqueror
{
    public interface IHttpCommandPathConvention
    {
        public string? GetCommandPath(Type commandType, HttpCommandAttribute attribute);
    }
}
