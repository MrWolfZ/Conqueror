using System;

namespace Conqueror
{
    [AttributeUsage(AttributeTargets.Class)]
    public sealed class HttpInteractiveStreamAttribute : Attribute
    {
        public bool UsePost { get; set; }
    }
}
