using System;

namespace Conqueror
{
    [AttributeUsage(AttributeTargets.Class)]
    public sealed class HttpQueryAttribute : Attribute
    {
        public bool UsePost { get; set; }

        public string? Path { get; set; }

        /// <summary>
        ///     The version of this query. It is used in the default path convention. A value of 0 (default) is treated as the absence of a version.
        ///     Note that this property cannot be nullable due to compiler limitations.
        /// </summary>
        public uint Version { get; set; }

        /// <summary>
        ///     The operation ID of this query in API descriptions (which is used in e.g. OpenAPI specifications).
        ///     Defaults to the full type name of the query type.
        /// </summary>
        public string? OperationId { get; set; }

        /// <summary>
        ///     The name of the API group in which this query is contained in API descriptions
        ///     (which is used in e.g. OpenAPI specifications).
        /// </summary>
        public string? ApiGroupName { get; set; }
    }
}
