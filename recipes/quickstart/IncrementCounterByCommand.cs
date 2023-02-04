using Conqueror;

namespace Quickstart;

[HttpCommand(Version = "v1")]
public sealed record IncrementCounterByCommand(string CounterName, int IncrementBy);

public sealed record IncrementCounterByCommandResponse(int NewCounterValue);

public interface IIncrementCounterByCommandHandler : ICommandHandler<IncrementCounterByCommand, IncrementCounterByCommandResponse>
{
}

internal sealed class IncrementCounterByCommandHandler : IIncrementCounterByCommandHandler, IConfigureCommandPipeline
{
    // add logging to the command pipeline and configure the pre-execution log level (only for demonstration purposes since the default is the same)
    public static void ConfigurePipeline(ICommandPipelineBuilder pipeline) => pipeline.UseLogging(o => o.PreExecutionLogLevel = LogLevel.Information);

    public async Task<IncrementCounterByCommandResponse> ExecuteCommand(IncrementCounterByCommand command, CancellationToken cancellationToken = default)
    {
        // simulate an asynchronous operation
        await Task.CompletedTask;

        var envVariableName = $"QUICKSTART_COUNTERS_{command.CounterName}";
        var counterValue = int.Parse(Environment.GetEnvironmentVariable(envVariableName) ?? "0");
        var newCounterValue = counterValue + command.IncrementBy;
        Environment.SetEnvironmentVariable(envVariableName, newCounterValue.ToString());
        return new(newCounterValue);
    }
}
