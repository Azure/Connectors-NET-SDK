//------------------------------------------------------------
// Copyright (c) Microsoft Corporation.  All rights reserved.
//------------------------------------------------------------

using global::Azure.Core;

namespace Microsoft.Azure.Connectors.Sdk.Authentication
{
    /// <summary>
    /// Adapts an <see cref="TokenCredential"/> (from Azure.Core) to the SDK's
    /// <see cref="ITokenProvider"/> interface, enabling generated clients to accept
    /// any Azure credential (DefaultAzureCredential, ManagedIdentityCredential, etc.).
    /// </summary>
    public class TokenCredentialTokenProvider : ITokenProvider
    {
        private readonly TokenCredential _credential;

        /// <summary>
        /// Initializes a new instance of the <see cref="TokenCredentialTokenProvider"/> class.
        /// </summary>
        /// <param name="credential">The Azure credential to wrap.</param>
        public TokenCredentialTokenProvider(TokenCredential credential)
        {
            ArgumentNullException.ThrowIfNull(credential);
            this._credential = credential;
        }

        /// <inheritdoc />
        public async Task<string> GetAccessTokenAsync(string[] scopes, CancellationToken cancellationToken = default)
        {
            ArgumentNullException.ThrowIfNull(scopes);

            if (scopes.Length == 0)
            {
                throw new ArgumentException(message: "At least one scope must be provided.", paramName: nameof(scopes));
            }

            var context = new TokenRequestContext(scopes);
            var token = await this._credential
                .GetTokenAsync(context, cancellationToken)
                .ConfigureAwait(continueOnCapturedContext: false);

            return token.Token;
        }
    }
}
