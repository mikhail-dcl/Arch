using System.ComponentModel;

namespace System.Collections.Generic;

/// <summary>
/// Defines extensions for System.Collections.Generic's List&lt;T&gt; collection
/// </summary>
[EditorBrowsable(EditorBrowsableState.Never)]
public static class ListSpanExtensions
{
    [MethodImpl(MethodImplOptions.AggressiveInlining)]
    public static Span<T> AsSpan<T>(this List<T> list)
    {
#if NET6_0_OR_GREATER
        return CollectionsMarshal.AsSpan(list);
#else
        return Unsafe.As<FakeList<T>>(list).GetSpan();
#endif
    }

    // This matches the memory layout of List<T>
    private sealed class FakeList<T>
    {
        private T[] _items;
        private int _size;
        private int _version;

        public Span<T> GetSpan() => new(_items, 0, _size);
    }
}

