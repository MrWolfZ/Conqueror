using System;

namespace Conqueror
{
    [AttributeUsage(AttributeTargets.Class)]
    public sealed class HttpCommandAttribute : Attribute
    {
        public string? Path { get; set; }
    }
}
