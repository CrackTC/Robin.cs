using System.Collections;
using System.Collections.Immutable;

namespace Robin.Common;

public class EquatableImmutableArray<T>(ImmutableArray<T> array) : IEnumerable<T>, IEquatable<EquatableImmutableArray<T>>
{
    private readonly ImmutableArray<T> _array = array;

    public bool Equals(EquatableImmutableArray<T>? other)
    {
        if (other is null) return false;
        if (ReferenceEquals(this, other)) return true;
        if (_array.Length != other._array.Length) return false;
        return _array.SequenceEqual(other._array);
    }

    public override bool Equals(object? obj) => obj is EquatableImmutableArray<T> other && Equals(other);

    public override int GetHashCode()
    {
        var hashCode = new HashCode();
        foreach (var item in _array)
        {
            hashCode.Add(item);
        }
        return hashCode.ToHashCode();
    }

    public override string ToString() => _array.IsEmpty ? "[]" : $"[ {string.Join(", ", _array)} ]";

    public static bool operator ==(EquatableImmutableArray<T>? left, EquatableImmutableArray<T>? right) => Equals(left, right);
    public static bool operator !=(EquatableImmutableArray<T>? left, EquatableImmutableArray<T>? right) => !Equals(left, right);
    public static implicit operator EquatableImmutableArray<T>(ImmutableArray<T> array) => new(array);

    public IEnumerator<T> GetEnumerator() => ((IEnumerable<T>)_array).GetEnumerator();
    IEnumerator IEnumerable.GetEnumerator() => GetEnumerator();
}