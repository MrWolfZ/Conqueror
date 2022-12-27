﻿namespace Conqueror.CQS.Transport.Http.Server.AspNetCore.Tests
{
    [HttpCommand]
    public sealed record TestCommand
    {
        public int Payload { get; init; }
    }

    public sealed record TestCommandResponse
    {
        public int Payload { get; init; }
    }

    public sealed class TestCommandHandler : ITestCommandHandler
    {
        public async Task<TestCommandResponse> ExecuteCommand(TestCommand command, CancellationToken cancellationToken = default)
        {
            await Task.Yield();
            cancellationToken.ThrowIfCancellationRequested();
            return new() { Payload = command.Payload + 1 };
        }
    }

    public interface ITestCommandHandler : ICommandHandler<TestCommand, TestCommandResponse>
    {
    }
}