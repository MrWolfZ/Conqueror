namespace Conqueror.CQS.Extensions.AspNetCore.Client
{
    public interface IConquerorHttpClientFactory
    {
        THandler CreateQueryHttpClient<THandler>()
            where THandler : class, IQueryHandler;

        THandler CreateCommandHttpClient<THandler>()
            where THandler : class, ICommandHandler;
    }
}
