//------------------------------------------------------------
// Copyright (c) Microsoft Corporation.  All rights reserved.
//------------------------------------------------------------

namespace Microsoft.Azure.Workflows.Connectors.Sdk.Authentication
{
    /// <summary>
    /// Interface for providing authentication tokens.
    /// </summary>
    public interface ITokenProvider
    {
        /// <summary>
        /// Gets an access token for the specified resource.
        /// </summary>
        /// <param name="scopes">The authentication scopes.</param>
        /// <param name="cancellationToken">The cancellation token.</param>
        /// <returns>The access token.</returns>
        Task<string> GetAccessTokenAsync(string[] scopes, CancellationToken cancellationToken = default);
    }
}
