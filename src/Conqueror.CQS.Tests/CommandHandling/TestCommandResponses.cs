using System.Collections.Generic;

namespace Conqueror.CQS.Tests.CommandHandling
{
    // used to track responses for command handlers that do not have direct responses
    public sealed class TestCommandResponses
    {
        public IList<int> Responses { get; } = new List<int>();
    }
}
