using System;

namespace Conqueror
{
    [AttributeUsage(AttributeTargets.Class)]
    public sealed class HttpCommandAttribute : Attribute
    {
        public string? Path { get; set; }

        /// <summary>
        ///     The version of this command. It is used in the default path convention.
        /// </summary>
        public string? Version { get; set; }

        /// <summary>
        ///     The operation ID of this command in API descriptions (which is used in e.g. OpenAPI specifications).
        ///     Defaults to the full type name of the command type.
        /// </summary>
        public string? OperationId { get; set; }

        /// <summary>
        ///     The name of the API group in which this command is contained in API descriptions
        ///     (which is used in e.g. OpenAPI specifications).
        /// </summary>
        public string? ApiGroupName { get; set; }
    }
}
