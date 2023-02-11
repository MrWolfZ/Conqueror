using System.Text.RegularExpressions;

namespace Conqueror.Recipes.CQS.Advanced.ExposingViaHttp;

internal sealed class CustomHttpCommandPathConvention : IHttpCommandPathConvention
{
    public string GetCommandPath(Type commandType, HttpCommandAttribute attribute)
    {
        if (attribute.Path != null)
        {
            return attribute.Path;
        }

        var versionPart = attribute.Version is null ? string.Empty : $"{attribute.Version}/";
        var namePart = Regex.Replace(commandType.Name, "Command$", string.Empty);
        return $"/api/{versionPart}{namePart}";
    }
}
