namespace Conqueror.Streaming.Transport.Http.Server.AspNetCore.Tests;

using System.Net;
using System.Net.Http.Json;
using System.Text.Json;
using Microsoft.AspNetCore.Mvc;

public static class HttpResponseMessageExtensions
{
    public static async Task AssertStatusCode(this HttpResponseMessage response, HttpStatusCode expectedStatusCode)
    {
        if (response.StatusCode != expectedStatusCode)
        {
            throw new(
                $"expected response to have status {expectedStatusCode} but it had {response.StatusCode}\nproblem details:\n{await FormatResponse()}"
            );

            async Task<string> FormatResponse()
            {
                try
                {
                    var problemDetails = await response.Content.ReadFromJsonAsync<ProblemDetails>(
                        CancellationToken.None
                    );

                    return $"title: {problemDetails?.Title}\ndetail: {problemDetails?.Detail}\nextensions: {JsonSerializer.Serialize(problemDetails?.Extensions)}";
                }
                catch
                {
                    return await response.Content.ReadAsStringAsync(CancellationToken.None);
                }
            }
        }
    }

    public static async Task AssertSuccessStatusCode(this HttpResponseMessage response)
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
                    var problemDetails = await response.Content.ReadFromJsonAsync<ProblemDetails>(
                        CancellationToken.None
                    );

                    return $"title: {problemDetails?.Title}\ndetail: {problemDetails?.Detail}\nextensions: {JsonSerializer.Serialize(problemDetails?.Extensions)}";
                }
                catch
                {
                    return await response.Content.ReadAsStringAsync(CancellationToken.None);
                }
            }
        }
    }
}
