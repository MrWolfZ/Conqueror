using System.Runtime.CompilerServices;

namespace Conqueror.SourceGenerators.Tests;

public static class ModuleInitializer
{
    [ModuleInitializer]
    public static void Init()
    {
        // VerifySourceGenerators.Initialize();
    }
}
