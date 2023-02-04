namespace Conqueror.Examples.CQS.GettingStarted.CustomHandlerInterface;

public sealed record CustomInterfaceExampleQuery(int Parameter);

public sealed record CustomInterfaceExampleQueryResponse(int Value);

public interface ICustomInterfaceExampleQueryHandler : IQueryHandler<CustomInterfaceExampleQuery, CustomInterfaceExampleQueryResponse>
{
}

public sealed class CustomInterfaceExampleQueryHandler : ICustomInterfaceExampleQueryHandler
{
    public async Task<CustomInterfaceExampleQueryResponse> ExecuteQuery(CustomInterfaceExampleQuery command, CancellationToken cancellationToken = default)
    {
        await Task.Yield();
        return new(command.Parameter);
    }
}
