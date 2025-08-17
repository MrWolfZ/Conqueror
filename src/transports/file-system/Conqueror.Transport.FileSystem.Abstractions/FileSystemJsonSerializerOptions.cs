namespace Conqueror;

internal static class FileSystemJsonSerializerOptions
{
    [UnconditionalSuppressMessage(
        "Aot",
        "IL3050:RequiresDynamicCode",
        Justification = "In AOT contexts we are fine with failing to deserialize the response, since there will be a clear error message telling them what needs to be done, and the failure will be found immediately when executing the message"
    )]
    [UnconditionalSuppressMessage(
        "Aot",
        "IL2026:RequiresUnreferencedCode",
        Justification = "In AOT contexts we are fine with failing to deserialize the response, since there will be a clear error message telling them what needs to be done, and the failure will be found immediately when executing the message"
    )]
    static FileSystemJsonSerializerOptions()
    {
        DefaultJsonSerializerOptions = new JsonSerializerOptions(JsonSerializerDefaults.Web)
        {
            PropertyNameCaseInsensitive = true,
            PropertyNamingPolicy = JsonNamingPolicy.CamelCase,
        };

        DefaultJsonSerializerOptions.MakeReadOnly(populateMissingResolver: true);
    }

    public static JsonSerializerOptions DefaultJsonSerializerOptions { get; }
}
