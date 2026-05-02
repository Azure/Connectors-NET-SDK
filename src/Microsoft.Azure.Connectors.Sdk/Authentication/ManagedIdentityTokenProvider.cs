//------------------------------------------------------------
// Copyright (c) Microsoft Corporation.  All rights reserved.
//------------------------------------------------------------

using global::Azure.Core;
using global::Azure.Identity;

namespace Microsoft.Azure.Connectors.Sdk.Authentication
{
    /// <summary>
    /// Token provider using Azure Managed Identity.
    /// </summary>
    public class ManagedIdentityTokenProvider : ITokenProvider
    {
        private readonly ManagedIdentityCredential _credential;

        /// <summary>
        /// Initializes a new instance of the <see cref="ManagedIdentityTokenProvider"/> class.
        /// </summary>
        /// <param name="clientId">
        /// Optional client ID for user-assigned managed identity.
        /// If <see langword="null"/> or empty, the system-assigned identity is used.
        /// </param>
        public ManagedIdentityTokenProvider(string? clientId = null)
        {
            this._credential = string.IsNullOrEmpty(clientId)
                ? new(ManagedIdentityId.SystemAssigned)
                : new(ManagedIdentityId.FromUserAssignedClientId(clientId));
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
