using System;

namespace Conqueror;

public interface IHttpStreamPathConvention
{
    public string? GetStreamPath(Type requestType, HttpStreamAttribute attribute);
}
