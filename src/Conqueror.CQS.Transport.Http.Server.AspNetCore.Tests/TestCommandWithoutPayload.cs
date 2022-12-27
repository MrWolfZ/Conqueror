﻿namespace Conqueror.CQS.Transport.Http.Server.AspNetCore.Tests
{
    [HttpCommand]
    public sealed record TestCommandWithoutPayload;

    public sealed class TestCommandHandlerWithoutPayload : ICommandHandler<TestCommandWithoutPayload, TestCommandResponse>
    {
        public async Task<TestCommandResponse> ExecuteCommand(TestCommandWithoutPayload command, CancellationToken cancellationToken = default)
        {
            await Task.Yield();
            cancellationToken.ThrowIfCancellationRequested();
            return new() { Payload = 11 };
        }
    }
}