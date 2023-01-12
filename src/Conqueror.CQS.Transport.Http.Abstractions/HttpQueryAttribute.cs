using System;

namespace Conqueror
{
    [AttributeUsage(AttributeTargets.Class)]
    public sealed class HttpQueryAttribute : Attribute
    {
        /// <summary>
        ///     If set to <c>true</c>, this query will be sent as an HTTP POST instead of a GET. The query payload
        ///     will be sent in the request body as JSON instead of as query parameters. This is recommended when
        ///     your query has a complex payload that is unsuitable for query parameters.
        /// </summary>
        public bool UsePost { get; set; }

        /// <summary>
        ///     A fixed path for this query. If this property is set, any path convention will simply return it.
        /// </summary>
        public string? Path { get; set; }

        /// <summary>
        ///     The version of this query. It is used in the default path convention.
        /// </summary>
        public string? Version { get; set; }

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
