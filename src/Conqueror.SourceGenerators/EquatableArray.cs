using System;
using System.Collections;
using System.Collections.Generic;

namespace Conqueror.SourceGenerators;

/// <summary>
///     An immutable, equatable array. This is equivalent to <see cref="Array" /> but with value equality support.
///     Taken from
///     https://github.com/andrewlock/NetEscapades.EnumGenerators/blob/b2807aba53271b23d50ead0a96eb3b76c5869cdd/src/NetEscapades.EnumGenerators/EquatableArray.cs#L1
/// </summary>
/// <typeparam name="T">The type of values in the array.</typeparam>
public readonly struct EquatableArray<T> : IEquatable<EquatableArray<T>>, IEnumerable<T>
    where T : IEquatable<T>
{
    /// <summary>
    ///     The underlying <typeparamref name="T" /> array.
    /// </summary>
    private readonly T[]? arrayField;

    /// <summary>
    ///     Creates a new <see cref="EquatableArray{T}" /> instance.
    /// </summary>
    /// <param name="array">The input <see cref="System.Collections.Immutable.ImmutableArray{T}" /> to wrap.</param>
    public EquatableArray(T[] array)
    {
        arrayField = array;
    }

    /// <sinheritdoc />
    public bool Equals(EquatableArray<T> array)
    {
        return AsSpan().SequenceEqual(array.AsSpan());
    }

    /// <sinheritdoc />
    public override bool Equals(object? obj)
    {
        return obj is EquatableArray<T> array && Equals(array);
    }

    /// <sinheritdoc />
    public override int GetHashCode()
    {
        if (arrayField is not T[] array)
        {
            return 0;
        }

        HashCode hashCode = default;

        foreach (var item in array)
        {
            hashCode.Add(item);
        }

        return hashCode.ToHashCode();
    }

    /// <summary>
    ///     Returns a <see cref="ReadOnlySpan{T}" /> wrapping the current items.
    /// </summary>
    /// <returns>A <see cref="ReadOnlySpan{T}" /> wrapping the current items.</returns>
    public ReadOnlySpan<T> AsSpan()
    {
        return arrayField.AsSpan();
    }

    /// <sinheritdoc />
    IEnumerator<T> IEnumerable<T>.GetEnumerator()
    {
        return ((IEnumerable<T>)(arrayField ?? [])).GetEnumerator();
    }

    /// <sinheritdoc />
    IEnumerator IEnumerable.GetEnumerator()
    {
        return ((IEnumerable<T>)(arrayField ?? [])).GetEnumerator();
    }

    public int Count => arrayField?.Length ?? 0;

    /// <summary>
    ///     Checks whether two <see cref="EquatableArray{T}" /> values are the same.
    /// </summary>
    /// <param name="left">The first <see cref="EquatableArray{T}" /> value.</param>
    /// <param name="right">The second <see cref="EquatableArray{T}" /> value.</param>
    /// <returns>Whether <paramref name="left" /> and <paramref name="right" /> are equal.</returns>
    public static bool operator ==(EquatableArray<T> left, EquatableArray<T> right)
    {
        return left.Equals(right);
    }

    /// <summary>
    ///     Checks whether two <see cref="EquatableArray{T}" /> values are not the same.
    /// </summary>
    /// <param name="left">The first <see cref="EquatableArray{T}" /> value.</param>
    /// <param name="right">The second <see cref="EquatableArray{T}" /> value.</param>
    /// <returns>Whether <paramref name="left" /> and <paramref name="right" /> are not equal.</returns>
    public static bool operator !=(EquatableArray<T> left, EquatableArray<T> right)
    {
        return !left.Equals(right);
    }
}
