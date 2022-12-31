using System.Net;
using System.Net.Http.Json;
using System.Text.Json;
using Microsoft.AspNetCore.Mvc;

namespace Conqueror.Streaming.Interactive.Transport.Http.Server.AspNetCore.Tests
{
    public static class HttpResponseMessageExtensions
    {
        public static async Task AssertStatusCode(this HttpResponseMessage response, HttpStatusCode expectedStatusCode)
        {
            if (response.StatusCode != expectedStatusCode)
            {
                throw new($"expected response to have status {expectedStatusCode} but it had {response.StatusCode}\nproblem details:\n{await FormatResponse()}");

                async Task<string> FormatResponse()
                {
                    try
                    {
                        var problemDetails = await response.Content.ReadFromJsonAsync<ProblemDetails>();
                        return $"title: {problemDetails?.Title}\ndetail: {problemDetails?.Detail}\nextensions: {JsonSerializer.Serialize(problemDetails?.Extensions)}";
                    }
                    catch
                    {
                        return await response.Content.ReadAsStringAsync();
                    }
                }
            }
        }

        public static async Task AssertSuccessStatusCode(this HttpResponseMessage response)
        {
            if ((int)response.StatusCode < 200 || (int)response.StatusCode >= 300)
            {
                throw new($"expected response to have success status but it had {response.StatusCode}\nproblem details:\n{await FormatResponse()}");

                async Task<string> FormatResponse()
                {
                    try
                    {
                        var problemDetails = await response.Content.ReadFromJsonAsync<ProblemDetails>();
                        return $"title: {problemDetails?.Title}\ndetail: {problemDetails?.Detail}\nextensions: {JsonSerializer.Serialize(problemDetails?.Extensions)}";
                    }
                    catch
                    {
                        return await response.Content.ReadAsStringAsync();
                    }
                }
            }
        }
    }
}
