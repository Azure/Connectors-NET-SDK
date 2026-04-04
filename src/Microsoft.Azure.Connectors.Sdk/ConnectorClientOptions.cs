//------------------------------------------------------------
// Copyright (c) Microsoft Corporation.  All rights reserved.
//------------------------------------------------------------

namespace Microsoft.Azure.Connectors.Sdk
{
    /// <summary>
    /// Configuration options for connector clients.
    /// </summary>
    public class ConnectorClientOptions
    {
        /// <summary>
        /// Gets or sets the base URI for the connector endpoint.
        /// </summary>
        public Uri? BaseUri { get; set; }

        /// <summary>
        /// Gets or sets the maximum number of retry attempts.
        /// </summary>
        public int MaxRetryAttempts { get; set; } = 3;

        /// <summary>
        /// Gets or sets the timeout for HTTP requests.
        /// </summary>
        public TimeSpan Timeout { get; set; } = TimeSpan.FromSeconds(30);

        /// <summary>
        /// Gets or sets a value indicating whether to use exponential backoff.
        /// </summary>
        public bool UseExponentialBackoff { get; set; } = true;

        /// <summary>
        /// Gets or sets the initial retry delay.
        /// </summary>
        public TimeSpan InitialRetryDelay { get; set; } = TimeSpan.FromMilliseconds(500);
    }
}
