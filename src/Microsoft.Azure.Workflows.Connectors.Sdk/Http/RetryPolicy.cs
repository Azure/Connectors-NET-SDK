//------------------------------------------------------------
// Copyright (c) Microsoft Corporation.  All rights reserved.
//------------------------------------------------------------

using System.Net;

namespace Microsoft.Azure.Workflows.Connectors.Sdk.Http;

/// <summary>
/// Defines retry policy configuration for HTTP operations.
/// </summary>
public class RetryPolicy
{
    /// <summary>
    /// Gets or sets the maximum number of retry attempts.
    /// </summary>
    public int MaxRetries { get; set; } = 3;

    /// <summary>
    /// Gets or sets the initial delay between retries.
    /// </summary>
    public TimeSpan InitialDelay { get; set; } = TimeSpan.FromMilliseconds(500);

    /// <summary>
    /// Gets or sets the maximum delay between retries.
    /// </summary>
    public TimeSpan MaxDelay { get; set; } = TimeSpan.FromSeconds(30);

    /// <summary>
    /// Gets or sets a value indicating whether to use exponential backoff.
    /// </summary>
    public bool UseExponentialBackoff { get; set; } = true;

    /// <summary>
    /// Gets or sets the HTTP status codes that should trigger a retry.
    /// </summary>
    public IReadOnlyCollection<HttpStatusCode> RetryableStatusCodes { get; set; } = new[]
    {
        HttpStatusCode.RequestTimeout,
        HttpStatusCode.TooManyRequests,
        HttpStatusCode.InternalServerError,
        HttpStatusCode.BadGateway,
        HttpStatusCode.ServiceUnavailable,
        HttpStatusCode.GatewayTimeout
    };

    /// <summary>
    /// Gets the default retry policy.
    /// </summary>
    public static RetryPolicy Default => new();

    /// <summary>
    /// Gets a retry policy with no retries.
    /// </summary>
    public static RetryPolicy NoRetry => new() { MaxRetries = 0 };

    /// <summary>
    /// Determines if the specified status code should trigger a retry.
    /// </summary>
    /// <param name="statusCode">The HTTP status code.</param>
    /// <returns>True if the request should be retried.</returns>
    public bool ShouldRetry(HttpStatusCode statusCode)
    {
        return this.RetryableStatusCodes.Contains(statusCode);
    }

    /// <summary>
    /// Calculates the delay for the specified retry attempt.
    /// </summary>
    /// <param name="attempt">The current retry attempt (1-based).</param>
    /// <returns>The delay before the next retry.</returns>
    public TimeSpan GetDelay(int attempt)
    {
        if (!this.UseExponentialBackoff)
        {
            return this.InitialDelay;
        }

        var delay = TimeSpan.FromMilliseconds(
            this.InitialDelay.TotalMilliseconds * Math.Pow(2, attempt - 1));

        return delay > this.MaxDelay
            ? this.MaxDelay
            : delay;
    }
}
