//------------------------------------------------------------
// Copyright (c) Microsoft Corporation.  All rights reserved.
//------------------------------------------------------------

namespace Microsoft.Azure.Connectors.Sdk
{
    /// <summary>
    /// Interface for paginated response types that contain a collection of items
    /// and an optional link to the next page.
    /// </summary>
    /// <typeparam name="T">The type of items in the page.</typeparam>
    public interface IPageable<T>
    {
        /// <summary>
        /// Gets the items in this page.
        /// </summary>
        List<T> Value { get; }

        /// <summary>
        /// Gets the URL to retrieve the next page, or null if this is the last page.
        /// </summary>
        string? NextLink { get; }
    }
}
