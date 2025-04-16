﻿using System;

namespace Conqueror.SourceGenerators.Util;

public sealed class Indentation
{
    public byte Level { get; set; }

    public IDisposable Indent()
    {
        Level += 1;
        return new AnonymousDisposable(() => Level -= 1);
    }
}
