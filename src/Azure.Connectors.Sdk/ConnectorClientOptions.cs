//------------------------------------------------------------
// Copyright (c) Microsoft Corporation.  All rights reserved.
//------------------------------------------------------------

using global::Azure.Core;

namespace Azure.Connectors.Sdk
{
    /// <summary>
    /// Configuration options for connector clients.
    /// Inherits from <see cref="ClientOptions"/> to provide Azure SDK standard
    /// retry, transport, and diagnostics configuration.
    /// </summary>
    /// <remarks>
    /// <para>
    /// Retry behavior is configured via the inherited <see cref="ClientOptions.Retry"/> property.
    /// Custom HTTP transport can be set via <see cref="ClientOptions.Transport"/>.
    /// </para>
    /// <para>
    /// NOTE(daviburg): The standard retry policy retries only GET, HEAD, OPTIONS, and TRACE requests
    /// by default. A caller-supplied <see cref="ClientOptions.RetryPolicy"/> remains authoritative and
    /// is responsible for its own HTTP-method safety.
    /// </para>
    /// </remarks>
    public class ConnectorClientOptions : ClientOptions
    {
        /// <summary>
        /// The service version of the Connector SDK API.
        /// </summary>
        public enum ServiceVersion
        {
            /// <summary>
            /// Version 1 of the Connector SDK API.
            /// </summary>
            V1 = 1,
        }

        /// <summary>
        /// Initializes a new instance of the <see cref="ConnectorClientOptions"/> class.
        /// </summary>
        /// <param name="version">The service version. Defaults to the latest supported version.</param>
        /// <exception cref="ArgumentException">
        /// Thrown when <paramref name="version"/> is the reserved default value <c>0</c>.
        /// </exception>
        public ConnectorClientOptions(ServiceVersion version = ServiceVersion.V1)
        {
            if (version == default)
            {
                throw new ArgumentException(
                    message: $"The service version '{version}' is not supported by this library.",
                    paramName: nameof(version));
            }

            this.Version = version;
        }

        /// <summary>
        /// Gets the service version.
        /// </summary>
        public ServiceVersion Version { get; }

        /// <summary>
        /// Gets or sets a value indicating whether the standard retry policy may retry HTTP methods
        /// other than GET, HEAD, OPTIONS, and TRACE. The default is <see langword="false"/>.
        /// </summary>
        /// <remarks>
        /// NOTE(daviburg): Enabling this option can repeat connector side effects after ambiguous
        /// failures. This option does not govern a caller-supplied <see cref="ClientOptions.RetryPolicy"/>.
        /// </remarks>
        public bool RetryUnsafeHttpMethods { get; set; }

        /// <summary>
        /// Gets or sets the base URI for the connector endpoint.
        /// </summary>
        public Uri? BaseUri { get; set; }
    }
}
