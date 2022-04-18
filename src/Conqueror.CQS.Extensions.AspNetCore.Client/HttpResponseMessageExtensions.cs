using System.Net.Http;
using System.Threading.Tasks;

namespace Conqueror.CQS.Extensions.AspNetCore.Client
{
    internal static class HttpResponseMessageExtensions
    {
        public static async Task<string> BufferAndReadContent(this HttpResponseMessage response)
        {
            var responseContent = await response.Content.ReadAsStringAsync();
            response.Content = new StringContent(responseContent);
            return responseContent;
        }
    }
}
