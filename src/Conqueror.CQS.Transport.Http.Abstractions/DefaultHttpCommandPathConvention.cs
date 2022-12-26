using System;
using System.Text.RegularExpressions;

namespace Conqueror
{
    public sealed class DefaultHttpCommandPathConvention : IHttpCommandPathConvention
    {
        public string GetCommandPath(Type commandType, HttpCommandAttribute attribute)
        {
            var regex = new Regex("Command$");
            var path = $"/api/commands/{regex.Replace(commandType.Name, string.Empty)}";
            return path;
        }
    }
}
