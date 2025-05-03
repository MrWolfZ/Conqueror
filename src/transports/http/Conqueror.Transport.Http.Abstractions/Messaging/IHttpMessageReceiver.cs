// ReSharper disable once CheckNamespace
namespace Conqueror;

public interface IHttpMessageReceiver : IMessageReceiver<IHttpMessageReceiver>
{
    IHttpMessageReceiver OmitFromApiDescription();
}
