using System;
using Microsoft.Extensions.DependencyInjection;
using NUnit.Framework;

namespace Conqueror.CQS.Tests.CommandHandling
{
    public sealed class CommandHandlerTests
    {
        [Test]
        public void InvalidCommandHandlers()
        {
            var services = new ServiceCollection();

            _ = Assert.Throws<ArgumentException>(() => services.AddConquerorCQS().AddTransient<TestCommandHandlerWithMultipleInterfaces>().ConfigureConqueror());
            _ = Assert.Throws<ArgumentException>(() => services.AddConquerorCQS().AddScoped<TestCommandHandlerWithMultipleInterfaces>().ConfigureConqueror());
            _ = Assert.Throws<ArgumentException>(() => services.AddConquerorCQS().AddSingleton<TestCommandHandlerWithMultipleInterfaces>().ConfigureConqueror());

            _ = Assert.Throws<ArgumentException>(() => services.AddConquerorCQS().AddTransient<TestCommandHandlerWithMultipleCustomInterfaces>().ConfigureConqueror());
            _ = Assert.Throws<ArgumentException>(() => services.AddConquerorCQS().AddScoped<TestCommandHandlerWithMultipleCustomInterfaces>().ConfigureConqueror());
            _ = Assert.Throws<ArgumentException>(() => services.AddConquerorCQS().AddSingleton<TestCommandHandlerWithMultipleCustomInterfaces>().ConfigureConqueror());

            _ = Assert.Throws<ArgumentException>(() => services.AddConquerorCQS().AddTransient<TestCommandHandlerWithMultipleMixedCustomInterfaces>().ConfigureConqueror());
            _ = Assert.Throws<ArgumentException>(() => services.AddConquerorCQS().AddScoped<TestCommandHandlerWithMultipleMixedCustomInterfaces>().ConfigureConqueror());
            _ = Assert.Throws<ArgumentException>(() => services.AddConquerorCQS().AddSingleton<TestCommandHandlerWithMultipleMixedCustomInterfaces>().ConfigureConqueror());

            _ = Assert.Throws<ArgumentException>(() => services.AddConquerorCQS().AddTransient<TestCommandHandlerWithCustomInterfaceWithExtraMethod>().ConfigureConqueror());
            _ = Assert.Throws<ArgumentException>(() => services.AddConquerorCQS().AddScoped<TestCommandHandlerWithCustomInterfaceWithExtraMethod>().ConfigureConqueror());
            _ = Assert.Throws<ArgumentException>(() => services.AddConquerorCQS().AddSingleton<TestCommandHandlerWithCustomInterfaceWithExtraMethod>().ConfigureConqueror());

            _ = Assert.Throws<ArgumentException>(() => services.AddConquerorCQS().AddTransient<TestCommandHandlerWithMultipleInterfacesWithoutResponse>().ConfigureConqueror());
            _ = Assert.Throws<ArgumentException>(() => services.AddConquerorCQS().AddScoped<TestCommandHandlerWithMultipleInterfacesWithoutResponse>().ConfigureConqueror());
            _ = Assert.Throws<ArgumentException>(() => services.AddConquerorCQS().AddSingleton<TestCommandHandlerWithMultipleInterfacesWithoutResponse>().ConfigureConqueror());

            _ = Assert.Throws<ArgumentException>(() => services.AddConquerorCQS().AddTransient<TestCommandHandlerWithMultipleCustomInterfacesWithoutResponse>().ConfigureConqueror());
            _ = Assert.Throws<ArgumentException>(() => services.AddConquerorCQS().AddScoped<TestCommandHandlerWithMultipleCustomInterfacesWithoutResponse>().ConfigureConqueror());
            _ = Assert.Throws<ArgumentException>(() => services.AddConquerorCQS().AddSingleton<TestCommandHandlerWithMultipleCustomInterfacesWithoutResponse>().ConfigureConqueror());

            _ = Assert.Throws<ArgumentException>(() => services.AddConquerorCQS().AddTransient<TestCommandHandlerWithMultipleMixedCustomInterfacesWithoutResponse>().ConfigureConqueror());
            _ = Assert.Throws<ArgumentException>(() => services.AddConquerorCQS().AddScoped<TestCommandHandlerWithMultipleMixedCustomInterfacesWithoutResponse>().ConfigureConqueror());
            _ = Assert.Throws<ArgumentException>(() => services.AddConquerorCQS().AddSingleton<TestCommandHandlerWithMultipleMixedCustomInterfacesWithoutResponse>().ConfigureConqueror());

            _ = Assert.Throws<ArgumentException>(() => services.AddConquerorCQS().AddTransient<TestCommandHandlerWithoutResponseWithCustomInterfaceWithExtraMethod>().ConfigureConqueror());
            _ = Assert.Throws<ArgumentException>(() => services.AddConquerorCQS().AddScoped<TestCommandHandlerWithoutResponseWithCustomInterfaceWithExtraMethod>().ConfigureConqueror());
            _ = Assert.Throws<ArgumentException>(() => services.AddConquerorCQS().AddSingleton<TestCommandHandlerWithoutResponseWithCustomInterfaceWithExtraMethod>().ConfigureConqueror());

            _ = Assert.Throws<ArgumentException>(() => services.AddConquerorCQS().AddTransient<TestCommandHandlerWithMultipleMixedInterfaces>().ConfigureConqueror());
            _ = Assert.Throws<ArgumentException>(() => services.AddConquerorCQS().AddScoped<TestCommandHandlerWithMultipleMixedInterfaces>().ConfigureConqueror());
            _ = Assert.Throws<ArgumentException>(() => services.AddConquerorCQS().AddSingleton<TestCommandHandlerWithMultipleMixedInterfaces>().ConfigureConqueror());

            _ = Assert.Throws<ArgumentException>(() => services.AddConquerorCQS().AddTransient<TestCommandHandlerWithoutInterfaces>().ConfigureConqueror());
            _ = Assert.Throws<ArgumentException>(() => services.AddConquerorCQS().AddScoped<TestCommandHandlerWithoutInterfaces>().ConfigureConqueror());
            _ = Assert.Throws<ArgumentException>(() => services.AddConquerorCQS().AddSingleton<TestCommandHandlerWithoutInterfaces>().ConfigureConqueror());
        }
    }
}
