using System.ComponentModel;

namespace System.Runtime.InteropServices
{
	/// <summary>
	/// An unsafe class that provides a set of methods to access
	/// the underlying data representations of collections.
	/// </summary>
	public static class CollectionMarshal
	{
		/// <summary>
		/// Gets a Span&lt;<typeparamref name="T"/>&gt; view over the data in <paramref name="list"/>.
		/// </summary>
		/// <remarks>
		/// <para><h3><b>Warning:</b> Read-only operation!</h3></para>
		/// Items should not be added or removed from the List&lt;<typeparamref name="T"/>&gt; while the Span&lt;<typeparamref name="T"/>&gt; is in use.
		/// </remarks>
		/// <typeparam name="T">The type of items in <paramref name="list"/>.</typeparam>
		/// <param name="list">List from which to create the Span&lt;<typeparamref name="T"/>&gt;.</param>
		/// <returns>A Span&lt;<typeparamref name="T"/>&gt; instance over the List&lt;<typeparamref name="T"/>&gt;.</returns>
		[MethodImpl( MethodImplOptions.ForwardRef )]
		public static extern Span<T> AsSpan< T >( List<T> list );

		//public static ref TValue GetValueRefOrNullRef<TKey, TValue>(
		//	Dictionary<TKey, TValue> dictionary, TKey key ) where TKey : notnull
		//		=> ref dictionary.FindValue( key );
	};
}



namespace System.Collections.Generic
{
	/// <summary>
	/// Defines extensions for System.Collections.Generic's List&lt;T&gt; collection
	/// </summary>
	[EditorBrowsable( EditorBrowsableState.Never )]
	public static class __ListXTension
	{
		/// <summary>
		/// Gets a Span&lt;<typeparamref name="T"/>&gt; view over the data in this List&lt;<typeparamref name="T"/>&gt; instance
		/// </summary>
		/// <remarks>
		/// <para><h3><b>Warning:</b> Read-only operation!</h3></para>
		/// Items should not be added or removed from the List&lt;<typeparamref name="T"/>&gt; while the Span&lt;<typeparamref name="T"/>&gt; is in use.
		/// </remarks>
		/// <typeparam name="T">The type of items in <paramref name="list"/></typeparam>
		/// <param name="list">This List&lt;<typeparamref name="T"/>&gt; instance</param>
		/// <returns>A Span&lt;<typeparamref name="T"/>&gt; instance over the List&lt;<typeparamref name="T"/>&gt;.</returns>
		[MethodImpl( MethodImplOptions.AggressiveInlining )]
		public static Span<T> AsSpan< T >( this List<T> list ) =>
			CollectionMarshal.AsSpan( list );
	};
}
