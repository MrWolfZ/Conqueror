﻿namespace Conqueror.Examples.BlazorWebAssembly.Application;

internal sealed class SharedCounter
{
    private long value;

    public long IncrementBy(long amount)
    {
        value += amount;
        return GetValue();
    }

    public long GetValue() => value;
}
