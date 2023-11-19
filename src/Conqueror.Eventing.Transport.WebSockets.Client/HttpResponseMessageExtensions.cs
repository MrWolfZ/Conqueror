using System.Net.Http;
using System.Threading.Tasks;

namespace Conqueror.Eventing.Transport.WebSockets.Client;

internal static class HttpResponseMessageExtensions
{
    public static async Task<string> BufferAndReadContent(this HttpResponseMessage response)
    {
        var responseContent = await response.Content.ReadAsStringAsync().ConfigureAwait(false);
        response.Content = new StringContent(responseContent);
        return responseContent;
    }
}
