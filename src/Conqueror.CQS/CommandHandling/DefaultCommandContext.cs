using System.Collections.Concurrent;
using System.Collections.Generic;

namespace Conqueror.CQS.CommandHandling
{
    internal sealed class DefaultCommandContext : CommandContext
    {
        private object command;
        private object? response;

        public DefaultCommandContext(object command)
        {
            this.command = command;
        }

        public override object Command => command;

        public override object? Response => response;

        public override IDictionary<object, object?> Items { get; set; } = new ConcurrentDictionary<object, object?>();

        public override IDictionary<string, string> TransferrableItems { get; set; } = new ConcurrentDictionary<string, string>();

        public void SetCommand(object cmd)
        {
            command = cmd;
        }

        public void SetResponse(object res)
        {
            response = res;
        }
    }
}
