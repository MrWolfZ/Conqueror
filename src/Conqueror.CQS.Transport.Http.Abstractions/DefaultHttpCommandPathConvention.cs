using System;
using System.Text.RegularExpressions;

namespace Conqueror
{
    public sealed class DefaultHttpCommandPathConvention : IHttpCommandPathConvention
    {
        private static readonly Regex StripSuffixRegex = new("Command$");

        public string GetCommandPath(Type commandType, HttpCommandAttribute attribute)
        {
            if (attribute.Path != null)
            {
                return attribute.Path;
            }
            
            return $"/api/commands/{StripSuffixRegex.Replace(commandType.Name, string.Empty)}";
        }
    }
}
