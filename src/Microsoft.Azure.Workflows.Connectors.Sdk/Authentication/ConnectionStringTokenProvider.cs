//------------------------------------------------------------
// Copyright (c) Microsoft Corporation.  All rights reserved.
//------------------------------------------------------------

namespace Microsoft.Azure.Workflows.Connectors.Sdk.Authentication
{
    /// <summary>
    /// Token provider using a pre-configured connection string or API key.
    /// </summary>
    public class ConnectionStringTokenProvider : ITokenProvider
    {
        private readonly string _apiKey;

        /// <summary>
        /// Initializes a new instance of the <see cref="ConnectionStringTokenProvider"/> class.
        /// </summary>
        /// <param name="apiKey">The API key or connection string.</param>
        public ConnectionStringTokenProvider(string apiKey)
        {
            ArgumentException.ThrowIfNullOrEmpty(apiKey);
            this._apiKey = apiKey;
        }

        /// <inheritdoc />
        public Task<string> GetAccessTokenAsync(string[] scopes, CancellationToken cancellationToken = default)
        {
            // NOTE(daviburg): For connection string auth, we return the API key directly.
            return Task.FromResult(this._apiKey);
        }
    }
}
