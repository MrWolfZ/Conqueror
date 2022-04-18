using System;

namespace Conqueror.CQS
{
    [AttributeUsage(AttributeTargets.Class)]
    public sealed class HttpQueryAttribute : Attribute
    {
        public bool UsePost { get; set; }
    }
}
