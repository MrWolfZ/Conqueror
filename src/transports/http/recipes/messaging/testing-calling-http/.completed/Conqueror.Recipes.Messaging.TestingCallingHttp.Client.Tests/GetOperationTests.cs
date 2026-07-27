namespace Conqueror.Recipes.Messaging.TestingCallingHttp.Client.Tests;

[TestFixture]
public class GetOperationTests
{
    [Test]
    public async Task GivenExistingCounter_WhenExecutingGetOperation_PrintsCounterValue()
    {
        const string counterName = "testCounter";
        const int counterValue = 10;

        var handler = Substitute.For<GetCounterValue.IHandler>();
        handler.Handle(Arg.Any<GetCounterValue>(), Arg.Any<CancellationToken>())
               .Returns(call => call.Arg<GetCounterValue>().CounterName == counterName
                                    ? new GetCounterValueResponse(CounterExists: true, counterValue)
                                    : new GetCounterValueResponse(CounterExists: false, CounterValue: null));

        var output = await ProgramInvoker.Invoke(services => services.AddSingleton(handler), "get", counterName);

        Assert.That(output.Trim(), Is.EqualTo($"counter '{counterName}' value: {counterValue}"));
    }

    [Test]
    public async Task GivenExistingCounter_WhenExecutingGetOperationWithInProcessHandler_PrintsCounterValue()
    {
        const string counterName = "testCounter";
        const int counterValue = 10;

        var output = await ProgramInvoker.Invoke(services =>
        {
            // create a handler from a delegate and re-bind the app's handler registration to a
            // plain sender without a transport, so that the message is handled in-process
            services.AddMessageHandlerDelegate(GetCounterValue.T, async (message, _, _) =>
            {
                await Task.CompletedTask;
                return message.CounterName == counterName ? new(CounterExists: true, counterValue) : new GetCounterValueResponse(CounterExists: false, CounterValue: null);
            });

            services.AddSingleton<GetCounterValue.IHandler>(p => p.GetRequiredService<IMessageSenders>().For(GetCounterValue.T));
        }, "get", counterName);

        Assert.That(output.Trim(), Is.EqualTo($"counter '{counterName}' value: {counterValue}"));
    }

    [Test]
    public async Task WhenExecutingGetOperationFailsWithHttpError_PrintsErrorMessage()
    {
        const string counterName = "testCounter";
        const HttpStatusCode errorStatusCode = HttpStatusCode.InternalServerError;

        var handler = Substitute.For<GetCounterValue.IHandler>();
        handler.Handle(Arg.Any<GetCounterValue>(), Arg.Any<CancellationToken>())
               .ThrowsAsync(new HttpMessageFailedOnClientException("message failed")
               {
                   Response = new() { StatusCode = errorStatusCode },
                   MessagePayload = new GetCounterValue(counterName),
                   TransportType = new(ConquerorTransportHttpConstants.TransportName, MessageTransportRole.Sender),
               });

        var output = await ProgramInvoker.Invoke(services => services.AddSingleton(handler), "get", counterName);

        Assert.That(output.Trim(), Is.EqualTo($"HTTP message failed with status code {(int)errorStatusCode}"));
    }
}
