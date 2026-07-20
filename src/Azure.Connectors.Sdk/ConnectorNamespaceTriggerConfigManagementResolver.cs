//------------------------------------------------------------
// Copyright (c) Microsoft Corporation.  All rights reserved.
//------------------------------------------------------------

using System;
using System.Text.Json;
using System.Threading;
using System.Threading.Tasks;
using global::Azure;
using global::Azure.Core;
using global::Azure.Core.Pipeline;

namespace Azure.Connectors.Sdk;

/// <summary>
/// Resolves Connector Namespace trigger configuration through the management API using <see cref="TokenCredential"/>.
/// </summary>
public sealed class ConnectorNamespaceTriggerConfigManagementResolver : IConnectorNamespaceTriggerConfigResolver
{
    private const string ConnectorNamePropertyName = "connectorName";
    private const string ConnectionDetailsPropertyName = "connectionDetails";
    private const string MicrosoftWebProviderName = "Microsoft.Web";
    private const string OperationNamePropertyName = "operationName";
    private const string PropertiesPropertyName = "properties";
    private const string ResourceGroupsSegmentName = "resourceGroups";
    private const string SubscriptionsSegmentName = "subscriptions";
    private const string TriggerConfigsSegmentName = "triggerconfigs";

    private readonly string _apiVersion;
    private readonly string[] _audienceScopes;
    private readonly Uri _managementEndpoint;
    private readonly HttpPipeline _pipeline;

    /// <summary>
    /// Initializes a new instance of the <see cref="ConnectorNamespaceTriggerConfigManagementResolver"/> class.
    /// </summary>
    /// <param name="credential">The credential used for management-plane authentication.</param>
    /// <param name="options">Optional resolver options for endpoint, audience, API version, retry, and transport.</param>
    public ConnectorNamespaceTriggerConfigManagementResolver(
        TokenCredential credential,
        ConnectorNamespaceTriggerConfigManagementResolverOptions? options = null)
    {
        ArgumentNullException.ThrowIfNull(credential);

        options ??= new ConnectorNamespaceTriggerConfigManagementResolverOptions();
        ArgumentNullException.ThrowIfNull(options.ManagementEndpoint);

        if (!options.ManagementEndpoint.IsAbsoluteUri)
        {
            throw new ArgumentException(
                message: $"The management endpoint '{options.ManagementEndpoint}' must be an absolute URI.",
                paramName: nameof(options));
        }

        if (string.IsNullOrWhiteSpace(options.Audience))
        {
            throw new ArgumentException(
                message: "The management audience cannot be null or whitespace.",
                paramName: nameof(options));
        }

        if (string.IsNullOrWhiteSpace(options.ApiVersion))
        {
            throw new ArgumentException(
                message: "The management API version cannot be null or whitespace.",
                paramName: nameof(options));
        }

        this._managementEndpoint = options.ManagementEndpoint;
        this._apiVersion = options.ApiVersion;
        this._audienceScopes = new[] { $"{options.Audience.TrimEnd('/')}/.default" };
        this._pipeline = HttpPipelineBuilder.Build(
            options,
            perRetryPolicies: new HttpPipelinePolicy[]
            {
                new BearerTokenAuthenticationPolicy(credential, this._audienceScopes)
            });
    }

    /// <inheritdoc />
    public async ValueTask<ConnectorNamespaceTriggerConfig> GetTriggerConfigAsync(
        ConnectorNamespaceTriggerConfigResourceIdentity resourceIdentity,
        CancellationToken cancellationToken = default)
    {
        ArgumentNullException.ThrowIfNull(resourceIdentity);

        using var message = this._pipeline.CreateMessage();
        var request = message.Request;
        request.Method = RequestMethod.Get;
        request.Uri.Reset(this.BuildRequestUri(resourceIdentity));
        request.Headers.Add("Accept", "application/json");

        try
        {
            await this._pipeline
                .SendAsync(message, cancellationToken)
                .ConfigureAwait(continueOnCapturedContext: false);
        }
        catch (OperationCanceledException)
        {
            throw;
        }
        catch (Exception ex) when (!ex.IsFatal())
        {
            int? requestFailedStatus = ex is RequestFailedException requestFailedException &&
                requestFailedException.Status > 0
                ? requestFailedException.Status
                : null;

            throw ConnectorNamespaceTriggerConfigManagementResolver.CreateResolutionException(
                resourceIdentity: resourceIdentity,
                status: requestFailedStatus,
                correlationId: null,
                detail: "The management request failed before a trigger configuration could be resolved.",
                innerException: ex);
        }

        var response = message.Response;
        if (response.IsError)
        {
            throw ConnectorNamespaceTriggerConfigManagementResolver.CreateResolutionException(
                resourceIdentity: resourceIdentity,
                status: response.Status,
                correlationId: null,
                detail: "The management API returned an unsuccessful status while resolving the trigger configuration.");
        }

        string responseContent = response.Content.ToString();
        if (string.IsNullOrWhiteSpace(responseContent))
        {
            throw ConnectorNamespaceTriggerConfigManagementResolver.CreateResolutionException(
                resourceIdentity: resourceIdentity,
                status: response.Status,
                correlationId: null,
                detail: "The management API returned an empty trigger configuration response.");
        }

        try
        {
            using var document = JsonDocument.Parse(responseContent);
            return ConnectorNamespaceTriggerConfigManagementResolver.ParseTriggerConfig(document, resourceIdentity, response.Status);
        }
        catch (JsonException ex)
        {
            throw ConnectorNamespaceTriggerConfigManagementResolver.CreateResolutionException(
                resourceIdentity: resourceIdentity,
                status: response.Status,
                correlationId: null,
                detail: "The management API returned malformed trigger configuration JSON.",
                innerException: ex);
        }
    }

    private static ConnectorTriggerConfigurationResolutionException CreateResolutionException(
        ConnectorNamespaceTriggerConfigResourceIdentity resourceIdentity,
        int? status,
        string? correlationId,
        string detail,
        Exception? innerException = null)
    {
        var message =
            $"{detail} Subscription '{resourceIdentity.SubscriptionId}', resource group '{resourceIdentity.ResourceGroupName}', " +
            $"Connector Namespace '{resourceIdentity.ConnectorNamespaceName}', trigger config '{resourceIdentity.TriggerConfigName}'.";

        if (status.HasValue)
        {
            message += $" Status: '{status.Value}'.";
        }

        if (correlationId is not null)
        {
            message += $" Correlation ID: '{correlationId}'.";
        }

        return new ConnectorTriggerConfigurationResolutionException(
            message: message,
            resourceIdentity: resourceIdentity,
            status: status,
            correlationId: correlationId,
            innerException: innerException);
    }

    private static string EscapePathSegment(string value)
    {
        return Uri.EscapeDataString(value);
    }

    private static ConnectorNamespaceTriggerConfig ParseTriggerConfig(
        JsonDocument document,
        ConnectorNamespaceTriggerConfigResourceIdentity resourceIdentity,
        int status)
    {
        if (document.RootElement.ValueKind != JsonValueKind.Object ||
            !document.RootElement.TryGetProperty(ConnectorNamespaceTriggerConfigManagementResolver.PropertiesPropertyName, out JsonElement propertiesElement) ||
            propertiesElement.ValueKind != JsonValueKind.Object ||
            !propertiesElement.TryGetProperty(ConnectorNamespaceTriggerConfigManagementResolver.ConnectionDetailsPropertyName, out JsonElement connectionDetailsElement) ||
            connectionDetailsElement.ValueKind != JsonValueKind.Object ||
            !connectionDetailsElement.TryGetProperty(ConnectorNamespaceTriggerConfigManagementResolver.ConnectorNamePropertyName, out JsonElement connectorNameElement) ||
            connectorNameElement.ValueKind != JsonValueKind.String ||
            !propertiesElement.TryGetProperty(ConnectorNamespaceTriggerConfigManagementResolver.OperationNamePropertyName, out JsonElement operationNameElement) ||
            operationNameElement.ValueKind != JsonValueKind.String)
        {
            throw ConnectorNamespaceTriggerConfigManagementResolver.CreateResolutionException(
                resourceIdentity: resourceIdentity,
                status: status,
                correlationId: null,
                detail: "The management API response did not contain required trigger configuration properties.");
        }

        string connectorName = connectorNameElement.GetString() ?? string.Empty;
        string operationName = operationNameElement.GetString() ?? string.Empty;

        if (string.IsNullOrWhiteSpace(connectorName) ||
            string.IsNullOrWhiteSpace(operationName))
        {
            throw ConnectorNamespaceTriggerConfigManagementResolver.CreateResolutionException(
                resourceIdentity: resourceIdentity,
                status: status,
                correlationId: null,
                detail: "The management API response contained empty trigger configuration identity values.");
        }

        return new ConnectorNamespaceTriggerConfig(
            ConnectorName: connectorName,
            OperationName: operationName);
    }

    private Uri BuildRequestUri(ConnectorNamespaceTriggerConfigResourceIdentity resourceIdentity)
    {
        string managementPath =
            $"{this._managementEndpoint.AbsolutePath.TrimEnd('/')}/" +
            $"{ConnectorNamespaceTriggerConfigManagementResolver.SubscriptionsSegmentName}/" +
            $"{ConnectorNamespaceTriggerConfigManagementResolver.EscapePathSegment(resourceIdentity.SubscriptionId)}/" +
            $"{ConnectorNamespaceTriggerConfigManagementResolver.ResourceGroupsSegmentName}/" +
            $"{ConnectorNamespaceTriggerConfigManagementResolver.EscapePathSegment(resourceIdentity.ResourceGroupName)}/" +
            $"providers/{ConnectorNamespaceTriggerConfigManagementResolver.MicrosoftWebProviderName}/connectorGateways/" +
            $"{ConnectorNamespaceTriggerConfigManagementResolver.EscapePathSegment(resourceIdentity.ConnectorNamespaceName)}/" +
            $"{ConnectorNamespaceTriggerConfigManagementResolver.TriggerConfigsSegmentName}/" +
            $"{ConnectorNamespaceTriggerConfigManagementResolver.EscapePathSegment(resourceIdentity.TriggerConfigName)}";

        var builder = new UriBuilder(this._managementEndpoint)
        {
            Path = managementPath,
            Query = $"api-version={Uri.EscapeDataString(this._apiVersion)}",
        };

        return builder.Uri;
    }
}
