using Microsoft.AspNetCore.Builder;

// ReSharper disable once CheckNamespace (we want these extensions to be accessible from client code without an extra import)
namespace Conqueror;

public static class ConquerorHttpServerAspNetCoreApplicationBuilderExtensions
{
    /// <summary>
    ///     Add support for Conqueror to the ASP.NET Core pipeline. This middleware includes various other
    ///     middlewares that each support different features of Conqueror.<br />
    ///     <br />
    ///     Please note that this middleware should be placed as late as possible in the pipeline (i.e. just
    ///     before adding endpoints).
    /// </summary>
    public static IApplicationBuilder UseConqueror(this IApplicationBuilder app)
    {
        return app.UseConquerorWellKnownErrorHandling();
    }
}
