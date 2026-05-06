//------------------------------------------------------------
// Copyright (c) Microsoft Corporation.  All rights reserved.
//------------------------------------------------------------

using global::Azure.Core;

namespace Microsoft.Azure.Connectors.Sdk.Authentication
{
    /// <summary>
    /// Adapts an <see cref="ITokenProvider"/> to <see cref="TokenCredential"/>
    /// for use with Azure.Core's HTTP pipeline authentication policies.
    /// </summary>
    internal sealed class TokenProviderCredential : TokenCredential
    {
        private readonly ITokenProvider _provider;

        /// <summary>
        /// Initializes a new instance of the <see cref="TokenProviderCredential"/> class.
        /// </summary>
        /// <param name="provider">The token provider to wrap.</param>
        public TokenProviderCredential(ITokenProvider provider)
        {
            ArgumentNullException.ThrowIfNull(provider);
            this._provider = provider;
        }

        /// <inheritdoc />
        public override AccessToken GetToken(TokenRequestContext requestContext, CancellationToken cancellationToken)
        {
            return this.GetTokenAsync(requestContext, cancellationToken)
                .AsTask()
                .GetAwaiter()
                .GetResult();
        }

        /// <inheritdoc />
        public override async ValueTask<AccessToken> GetTokenAsync(
            TokenRequestContext requestContext,
            CancellationToken cancellationToken)
        {
            var token = await this._provider
                .GetAccessTokenAsync(requestContext.Scopes, cancellationToken)
                .ConfigureAwait(continueOnCapturedContext: false);

            // NOTE(daviburg): ITokenProvider does not expose expiration, so we use a reasonable default.
            // The BearerTokenAuthenticationPolicy will refresh when the token nears expiration.
            return new AccessToken(token, DateTimeOffset.UtcNow.AddMinutes(5));
        }
    }
}
