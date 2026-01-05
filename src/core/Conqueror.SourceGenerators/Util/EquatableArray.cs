namespace Conqueror.SourceGenerators.Util;

using System.Collections;

/// <summary>
///     An immutable, equatable array. This is equivalent to <see cref="Array" /> but with value equality support.
///     Taken from
///     https://github.com/andrewlock/NetEscapades.EnumGenerators/blob/b2807aba53271b23d50ead0a96eb3b76c5869cdd/src/NetEscapades.EnumGenerators/EquatableArray.cs#L1
/// </summary>
/// <typeparam name="T">The type of values in the array.</typeparam>
internal readonly struct EquatableArray<T> : IEquatable<EquatableArray<T>>, IEnumerable<T>
    where T : IEquatable<T>
{
    public static readonly EquatableArray<T> Empty = new(Array.Empty<T>());

    /// <summary>
    ///     The underlying <typeparamref name="T" /> array.
    /// </summary>
    private readonly T[] arrayField;

    /// <summary>
    ///     Initializes a new instance of the <see cref="EquatableArray{T}" /> struct.
    /// </summary>
    /// <param name="array">The input <see cref="System.Collections.Immutable.ImmutableArray{T}" /> to wrap.</param>
    public EquatableArray(T[] array) => arrayField = array;

    /// <summary>
    ///     Initializes a new instance of the <see cref="EquatableArray{T}" /> struct.
    /// </summary>
    /// <param name="span">The input <see cref="System.Span{T}" /> to wrap.</param>
    public EquatableArray(Span<T> span) => arrayField = span.ToArray();

    public int Count => arrayField?.Length ?? 0;

    public T this[int i] => arrayField[i];

    /// <summary>
    ///     Checks whether two <see cref="EquatableArray{T}" /> values are the same.
    /// </summary>
    /// <param name="left">The first <see cref="EquatableArray{T}" /> value.</param>
    /// <param name="right">The second <see cref="EquatableArray{T}" /> value.</param>
    /// <returns>Whether <paramref name="left" /> and <paramref name="right" /> are equal.</returns>
    public static bool operator ==(EquatableArray<T> left, EquatableArray<T> right) => left.Equals(right);

    /// <summary>
    ///     Checks whether two <see cref="EquatableArray{T}" /> values are not the same.
    /// </summary>
    /// <param name="left">The first <see cref="EquatableArray{T}" /> value.</param>
    /// <param name="right">The second <see cref="EquatableArray{T}" /> value.</param>
    /// <returns>Whether <paramref name="left" /> and <paramref name="right" /> are not equal.</returns>
    public static bool operator !=(EquatableArray<T> left, EquatableArray<T> right) => !left.Equals(right);

    public bool Equals(EquatableArray<T> other) => AsSpan().SequenceEqual(other.AsSpan());

    public override bool Equals(object? obj) => obj is EquatableArray<T> array && Equals(array);

    public override int GetHashCode()
    {
        if (arrayField is not { } array)
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
    public ReadOnlySpan<T> AsSpan() => arrayField.AsSpan();

    IEnumerator<T> IEnumerable<T>.GetEnumerator() => ((IEnumerable<T>)(arrayField ?? [])).GetEnumerator();

    IEnumerator IEnumerable.GetEnumerator() => ((IEnumerable<T>)(arrayField ?? [])).GetEnumerator();
}
