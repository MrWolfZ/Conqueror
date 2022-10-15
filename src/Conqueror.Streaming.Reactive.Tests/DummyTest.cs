using System.Threading.Tasks;
using NUnit.Framework;

namespace Conqueror.Streaming.Reactive.Tests
{
    [TestFixture]
    public class DummyTest
    {
        [Test]
        public async Task Test()
        {
            await Task.Yield();
        }
    }
}
