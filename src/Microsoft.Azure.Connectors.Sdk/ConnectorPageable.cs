//------------------------------------------------------------
// Copyright (c) Microsoft Corporation.  All rights reserved.
//------------------------------------------------------------

using System.Runtime.CompilerServices;

namespace Microsoft.Azure.Connectors.Sdk
{
    /// <summary>
    /// An async enumerable that automatically follows pagination links to yield all items
    /// across multiple pages. Each call to the underlying service fetches one page at a time.
    /// </summary>
    /// <typeparam name="TPage">The page type implementing <see cref="IPageable{TItem}"/>.</typeparam>
    /// <typeparam name="TItem">The type of items in each page.</typeparam>
    public sealed class ConnectorPageable<TPage, TItem> : IAsyncEnumerable<TItem>
        where TPage : class, IPageable<TItem>
    {
        private readonly Func<CancellationToken, Task<TPage>> _firstPageFunc;
        private readonly Func<string, CancellationToken, Task<TPage>> _nextPageFunc;

        /// <summary>
        /// Initializes a new instance of the <see cref="ConnectorPageable{TPage, TItem}"/> class.
        /// </summary>
        /// <param name="firstPageFunc">Function that fetches the first page.</param>
        /// <param name="nextPageFunc">Function that fetches subsequent pages given an absolute NextLink URL.</param>
        public ConnectorPageable(
            Func<CancellationToken, Task<TPage>> firstPageFunc,
            Func<string, CancellationToken, Task<TPage>> nextPageFunc)
        {
            this._firstPageFunc = firstPageFunc ?? throw new ArgumentNullException(nameof(firstPageFunc));
            this._nextPageFunc = nextPageFunc ?? throw new ArgumentNullException(nameof(nextPageFunc));
        }

        /// <inheritdoc />
        public async IAsyncEnumerator<TItem> GetAsyncEnumerator(
            CancellationToken cancellationToken = default)
        {
            TPage? page = await this._firstPageFunc(cancellationToken).ConfigureAwait(continueOnCapturedContext: false);

            while (page != null)
            {
                if (page.Value != null)
                {
                    foreach (var item in page.Value)
                    {
                        yield return item;
                    }
                }

                if (string.IsNullOrEmpty(page.NextLink))
                {
                    break;
                }

                page = await this._nextPageFunc(page.NextLink!, cancellationToken).ConfigureAwait(continueOnCapturedContext: false);
            }
        }

        /// <summary>
        /// Returns an async enumerable that yields pages instead of individual items,
        /// for callers who need page-level access or continuation tokens.
        /// </summary>
        /// <returns>An async enumerable of pages.</returns>
        public async IAsyncEnumerable<TPage> AsPages(
            [EnumeratorCancellation] CancellationToken cancellationToken = default)
        {
            TPage? page = await this._firstPageFunc(cancellationToken).ConfigureAwait(continueOnCapturedContext: false);

            while (page != null)
            {
                yield return page;

                if (string.IsNullOrEmpty(page.NextLink))
                {
                    break;
                }

                page = await this._nextPageFunc(page.NextLink!, cancellationToken).ConfigureAwait(continueOnCapturedContext: false);
            }
        }
    }
}
