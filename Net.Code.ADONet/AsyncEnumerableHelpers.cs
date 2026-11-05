using System.Threading;

namespace Net.Code.ADONet;

/// <summary>
/// The few async LINQ operators this library needs, so that it does not depend on System.Linq.Async
/// (which conflicts with System.Linq.AsyncEnumerable, part of .NET 10).
/// </summary>
internal static class AsyncEnumerableHelpers
{
    public static async ValueTask<T> SingleAsync<T>(IAsyncEnumerable<T> source, CancellationToken cancellationToken)
    {
        var e = source.GetAsyncEnumerator(cancellationToken);
        await using var _ = e.ConfigureAwait(false);
        if (!await e.MoveNextAsync().ConfigureAwait(false))
            throw new InvalidOperationException("Sequence contains no elements");
        var result = e.Current;
        if (await e.MoveNextAsync().ConfigureAwait(false))
            throw new InvalidOperationException("Sequence contains more than one element");
        return result;
    }

    public static async ValueTask<T?> FirstOrDefaultAsync<T>(IAsyncEnumerable<T> source, CancellationToken cancellationToken)
    {
        var e = source.GetAsyncEnumerator(cancellationToken);
        await using var _ = e.ConfigureAwait(false);
        return await e.MoveNextAsync().ConfigureAwait(false) ? e.Current : default;
    }
}
