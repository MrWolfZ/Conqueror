using System;
using Microsoft.Extensions.DependencyInjection;
using NUnit.Framework;

namespace Conqueror.CQS.Tests.QueryHandling
{
    public sealed class QueryHandlerTests
    {
        [Test]
        public void InvalidQueryHandlers()
        {
            var services = new ServiceCollection();

            _ = Assert.Throws<ArgumentException>(() => services.AddConquerorCQS().AddTransient<TestQueryHandlerWithMultipleInterfaces>().ConfigureConqueror());
            _ = Assert.Throws<ArgumentException>(() => services.AddConquerorCQS().AddScoped<TestQueryHandlerWithMultipleInterfaces>().ConfigureConqueror());
            _ = Assert.Throws<ArgumentException>(() => services.AddConquerorCQS().AddSingleton<TestQueryHandlerWithMultipleInterfaces>().ConfigureConqueror());

            _ = Assert.Throws<ArgumentException>(() => services.AddConquerorCQS().AddTransient<TestQueryHandlerWithMultipleCustomInterfaces>().ConfigureConqueror());
            _ = Assert.Throws<ArgumentException>(() => services.AddConquerorCQS().AddScoped<TestQueryHandlerWithMultipleCustomInterfaces>().ConfigureConqueror());
            _ = Assert.Throws<ArgumentException>(() => services.AddConquerorCQS().AddSingleton<TestQueryHandlerWithMultipleCustomInterfaces>().ConfigureConqueror());

            _ = Assert.Throws<ArgumentException>(() => services.AddConquerorCQS().AddTransient<TestQueryHandlerWithMultipleMixedInterfaces>().ConfigureConqueror());
            _ = Assert.Throws<ArgumentException>(() => services.AddConquerorCQS().AddScoped<TestQueryHandlerWithMultipleMixedInterfaces>().ConfigureConqueror());
            _ = Assert.Throws<ArgumentException>(() => services.AddConquerorCQS().AddSingleton<TestQueryHandlerWithMultipleMixedInterfaces>().ConfigureConqueror());

            _ = Assert.Throws<ArgumentException>(() => services.AddConquerorCQS().AddTransient<TestQueryHandlerWithCustomInterfaceWithExtraMethod>().ConfigureConqueror());
            _ = Assert.Throws<ArgumentException>(() => services.AddConquerorCQS().AddScoped<TestQueryHandlerWithCustomInterfaceWithExtraMethod>().ConfigureConqueror());
            _ = Assert.Throws<ArgumentException>(() => services.AddConquerorCQS().AddSingleton<TestQueryHandlerWithCustomInterfaceWithExtraMethod>().ConfigureConqueror());

            _ = Assert.Throws<ArgumentException>(() => services.AddConquerorCQS().AddTransient<TestQueryHandlerWithoutInterfaces>().ConfigureConqueror());
            _ = Assert.Throws<ArgumentException>(() => services.AddConquerorCQS().AddScoped<TestQueryHandlerWithoutInterfaces>().ConfigureConqueror());
            _ = Assert.Throws<ArgumentException>(() => services.AddConquerorCQS().AddSingleton<TestQueryHandlerWithoutInterfaces>().ConfigureConqueror());
        }
    }
}
