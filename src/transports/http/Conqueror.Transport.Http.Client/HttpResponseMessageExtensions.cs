using System.Net.Http;
using System.Threading.Tasks;

namespace Conqueror.Transport.Http.Client;

internal static class HttpResponseMessageExtensions
{
    public static async Task<string> BufferAndReadContent(this HttpResponseMessage response)
    {
        var responseContent = await response.Content.ReadAsStringAsync().ConfigureAwait(false);
        response.Content.Dispose();
        response.Content = new StringContent(responseContent);
        return responseContent;
    }
}
