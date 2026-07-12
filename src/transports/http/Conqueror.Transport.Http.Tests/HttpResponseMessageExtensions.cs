namespace Conqueror.Transport.Http.Tests;

using System.Net.Http.Json;

public static class HttpResponseMessageExtensions
{
    public static async Task AssertStatusCode(
        this HttpResponseMessage response,
        int expectedStatusCode,
        CancellationToken cancellationToken
    )
    {
        if ((int)response.StatusCode != expectedStatusCode)
        {
            throw new(
                $"expected response to have status {expectedStatusCode} but it had {response.StatusCode}\nproblem details:\n{await FormatResponse()}"
            );

            async Task<string> FormatResponse()
            {
                try
                {
                    var problemDetails = await response.Content.ReadFromJsonAsync<ProblemDetails>(cancellationToken);

                    return
                        $"title: {problemDetails?.Title}\ndetail: {problemDetails?.Detail}\nextensions: {JsonSerializer.Serialize(problemDetails?.Extensions)}";
                }
                catch
                {
                    return await response.Content.ReadAsStringAsync(cancellationToken);
                }
            }
        }
    }

    public static async Task AssertSuccessStatusCode(
        this HttpResponseMessage response,
        CancellationToken cancellationToken
    )
    {
        if ((int)response.StatusCode is < 200 or >= 300)
        {
            throw new(
                $"expected response to have success status but it had {response.StatusCode}\nproblem details:\n{await FormatResponse()}"
            );

            async Task<string> FormatResponse()
            {
                try
                {
                    var stringResponse = await response.Content.ReadAsStringAsync(cancellationToken);

                    try
                    {
                        var pd = JsonSerializer.Deserialize<ProblemDetails>(stringResponse);

                        return $"title: {pd?.Title}\ndetail: {pd?.Detail}\nextensions: {JsonSerializer.Serialize(pd?.Extensions)}";
                    }
                    catch
                    {
                        return $"{stringResponse}";
                    }
                }
                catch (Exception e)
                {
                    return $"failed to read response: {e}";
                }
            }
        }
    }
}
