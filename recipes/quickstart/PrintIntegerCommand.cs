using Conqueror;

namespace Quickstart;

[HttpCommand]
public sealed record PrintIntegerCommand(int Parameter);

public sealed record PrintIntegerCommandResponse(int Parameter);

public interface IPrintIntegerCommandHandler : ICommandHandler<PrintIntegerCommand,
    PrintIntegerCommandResponse>
{
}

public sealed class PrintIntegerCommandHandler : IPrintIntegerCommandHandler
{
    public Task<PrintIntegerCommandResponse> ExecuteCommand(PrintIntegerCommand command,
                                                            CancellationToken cancellationToken = default)
    {
        Console.WriteLine($"Got command parameter {command.Parameter}");
        return Task.FromResult(new PrintIntegerCommandResponse(command.Parameter));
    }
}
