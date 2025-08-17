namespace Conqueror.Transport.Http.Client;

internal static class HttpResponseMessageExtensions
{
    public static async Task<string> BufferAndReadContent(
        this HttpResponseMessage response,
        CancellationToken cancellationToken
    )
    {
        var responseContent = await response.Content.ReadAsStringAsync(cancellationToken).ConfigureAwait(false);
        response.Content.Dispose();
        response.Content = new StringContent(responseContent);

        return responseContent;
    }
}
