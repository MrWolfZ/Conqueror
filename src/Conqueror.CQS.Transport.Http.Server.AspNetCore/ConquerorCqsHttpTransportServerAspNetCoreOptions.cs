namespace Conqueror.CQS.Transport.Http.Server.AspNetCore
{
    public sealed class ConquerorCqsHttpTransportServerAspNetCoreOptions
    {
        public IHttpCommandPathConvention? CommandPathConvention { get; set; }

        public IHttpQueryPathConvention? QueryPathConvention { get; set; }
    }
}
