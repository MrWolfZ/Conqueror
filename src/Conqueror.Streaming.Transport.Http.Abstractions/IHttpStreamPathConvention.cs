namespace Conqueror;

public interface IHttpStreamPathConvention
{
    string? GetStreamPath(Type requestType, HttpStreamAttribute attribute);
}
