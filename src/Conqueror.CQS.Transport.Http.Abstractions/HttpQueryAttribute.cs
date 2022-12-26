using System;

namespace Conqueror
{
    [AttributeUsage(AttributeTargets.Class)]
    public sealed class HttpQueryAttribute : Attribute
    {
        public bool UsePost { get; set; }
        
        public string? Path { get; set; }
    }
}
