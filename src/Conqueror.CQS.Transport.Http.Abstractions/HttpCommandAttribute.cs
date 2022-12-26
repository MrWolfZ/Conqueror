using System;

namespace Conqueror
{
    [AttributeUsage(AttributeTargets.Class)]
    public sealed class HttpCommandAttribute : Attribute
    {
        public string? Path { get; set; }
        
        /// <summary>
        /// The version of this command. It is used in the default path convention. A value of 0 (default) is treated as the absence of a version.
        ///
        /// Note that this property cannot be nullable due to compiler limitations.
        /// </summary>
        public uint Version { get; set; }
    }
}
