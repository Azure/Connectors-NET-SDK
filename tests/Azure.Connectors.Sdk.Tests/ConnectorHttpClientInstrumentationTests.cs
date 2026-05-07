//------------------------------------------------------------
// Copyright (c) Microsoft Corporation.  All rights reserved.
//------------------------------------------------------------

using System.Diagnostics;
using System.Net;
using System.Net.Http;
using Azure.Core.Pipeline;
using global::Azure;
using Azure.Connectors.Sdk.Http;
using Microsoft.VisualStudio.TestTools.UnitTesting;
using Moq;
using Moq.Protected;

namespace Azure.Connectors.Sdk.Tests
{
    [TestClass]
    public class ConnectorHttpClientInstrumentationTests
    {
        private static readonly string[] TestScopes = new[] { "https://test/.default" };

        [TestMethod]
        public async Task SendAsync_WithActivityListener_CreatesSpan()
        {
            // Arrange
            var capturedActivities = new List<Activity>();

            using var listener = new ActivityListener
            {
                ShouldListenTo = source => string.Equals(source.Name, ConnectorHttpClient.ActivitySourceName, StringComparison.Ordinal),
                Sample = (ref ActivityCreationOptions<ActivityContext> _) => ActivitySamplingResult.AllDataAndRecorded,
                ActivityStopped = activity => capturedActivities.Add(activity),
            };

            ActivitySource.AddActivityListener(listener);

            var options = CreateOptionsWithMockTransport(HttpStatusCode.OK);

            using var client = CreateClient(options, connectorName: "office365");

            using var request = new HttpRequestMessage(HttpMethod.Get, "https://test.azure.com/api/messages");
            request.Headers.Add("x-ms-client-request-id", "test-request-id");

            // Act
            using var response = await client
                .SendAsync(request, TestScopes)
                .ConfigureAwait(continueOnCapturedContext: false);

            // Assert
            Assert.AreEqual(1, capturedActivities.Count);

            var activity = capturedActivities[0];
            Assert.AreEqual("HTTP GET", activity.DisplayName);
            Assert.AreEqual(ActivityKind.Client, activity.Kind);
            Assert.AreEqual("GET", activity.GetTagItem("http.method")?.ToString());
            Assert.AreEqual("office365", activity.GetTagItem("connector.name")?.ToString());
            Assert.AreEqual("test-request-id", activity.GetTagItem("x-ms-client-request-id")?.ToString());
            Assert.AreEqual(200, activity.GetTagItem("http.status_code"));
        }

        [TestMethod]
        public async Task SendAsync_WithoutListener_NoOverhead()
        {
            // Arrange — no ActivityListener registered
            var options = CreateOptionsWithMockTransport(HttpStatusCode.OK);

            using var client = CreateClient(options, connectorName: "office365");

            using var request = new HttpRequestMessage(HttpMethod.Get, "https://test.azure.com/api/messages");

            var baselineActivity = Activity.Current;

            // Act — should complete without Activity overhead
            using var response = await client
                .SendAsync(request, TestScopes)
                .ConfigureAwait(continueOnCapturedContext: false);

            // Assert
            Assert.AreEqual(HttpStatusCode.OK, response.StatusCode);
            Assert.AreEqual(baselineActivity, Activity.Current, "Activity.Current should be unchanged when no listener is registered.");
        }

        [TestMethod]
        public async Task SendAsync_OnFailure_SetsErrorStatus()
        {
            // Arrange
            var capturedActivities = new List<Activity>();

            using var listener = new ActivityListener
            {
                ShouldListenTo = source => string.Equals(source.Name, ConnectorHttpClient.ActivitySourceName, StringComparison.Ordinal),
                Sample = (ref ActivityCreationOptions<ActivityContext> _) => ActivitySamplingResult.AllDataAndRecorded,
                ActivityStopped = activity => capturedActivities.Add(activity),
            };

            ActivitySource.AddActivityListener(listener);

            var options = CreateOptionsWithMockTransport(HttpStatusCode.InternalServerError);
            options.Retry.MaxRetries = 0;

            using var client = CreateClient(options, connectorName: "office365");

            using var request = new HttpRequestMessage(HttpMethod.Get, "https://test.azure.com/api/messages");

            // Act
            using var response = await client
                .SendAsync(request, TestScopes)
                .ConfigureAwait(continueOnCapturedContext: false);

            // Assert
            Assert.AreEqual(1, capturedActivities.Count);

            var activity = capturedActivities[0];
            Assert.AreEqual(500, activity.GetTagItem("http.status_code"));
            Assert.AreEqual("ERROR", activity.GetTagItem("otel.status_code")?.ToString());
            Assert.AreEqual(ActivityStatusCode.Error, activity.Status);
        }

        [TestMethod]
        public async Task SendAsync_SetsConnectorNameAttribute()
        {
            // Arrange
            var capturedActivities = new List<Activity>();

            using var listener = new ActivityListener
            {
                ShouldListenTo = source => string.Equals(source.Name, ConnectorHttpClient.ActivitySourceName, StringComparison.Ordinal),
                Sample = (ref ActivityCreationOptions<ActivityContext> _) => ActivitySamplingResult.AllDataAndRecorded,
                ActivityStopped = activity => capturedActivities.Add(activity),
            };

            ActivitySource.AddActivityListener(listener);

            var options = CreateOptionsWithMockTransport(HttpStatusCode.OK);

            using var client = CreateClient(options, connectorName: "sharepointonline");

            using var request = new HttpRequestMessage(HttpMethod.Post, "https://test.azure.com/api/files");

            // Act
            using var response = await client
                .SendAsync(request, TestScopes)
                .ConfigureAwait(continueOnCapturedContext: false);

            // Assert
            Assert.AreEqual(1, capturedActivities.Count);
            Assert.AreEqual("sharepointonline", capturedActivities[0].GetTagItem("connector.name")?.ToString());
        }

        [TestMethod]
        public async Task SendAsync_WithoutConnectorName_OmitsTag()
        {
            // Arrange
            var capturedActivities = new List<Activity>();

            using var listener = new ActivityListener
            {
                ShouldListenTo = source => string.Equals(source.Name, ConnectorHttpClient.ActivitySourceName, StringComparison.Ordinal),
                Sample = (ref ActivityCreationOptions<ActivityContext> _) => ActivitySamplingResult.AllDataAndRecorded,
                ActivityStopped = activity => capturedActivities.Add(activity),
            };

            ActivitySource.AddActivityListener(listener);

            var options = CreateOptionsWithMockTransport(HttpStatusCode.OK);

            using var client = CreateClient(options);

            using var request = new HttpRequestMessage(HttpMethod.Get, "https://test.azure.com/api/test");

            // Act
            using var response = await client
                .SendAsync(request, TestScopes)
                .ConfigureAwait(continueOnCapturedContext: false);

            // Assert
            Assert.AreEqual(1, capturedActivities.Count);
            Assert.IsNull(capturedActivities[0].GetTagItem("connector.name"));
        }

        [TestMethod]
        public async Task SendAsync_OnException_SetsErrorStatusAndRethrows()
        {
            // Arrange
            var capturedActivities = new List<Activity>();

            using var listener = new ActivityListener
            {
                ShouldListenTo = source => string.Equals(source.Name, ConnectorHttpClient.ActivitySourceName, StringComparison.Ordinal),
                Sample = (ref ActivityCreationOptions<ActivityContext> _) => ActivitySamplingResult.AllDataAndRecorded,
                ActivityStopped = activity => capturedActivities.Add(activity),
            };

            ActivitySource.AddActivityListener(listener);

            var handler = new Mock<HttpMessageHandler>();
            handler
                .Protected()
                .Setup<Task<HttpResponseMessage>>(
                    "SendAsync",
                    ItExpr.IsAny<HttpRequestMessage>(),
                    ItExpr.IsAny<CancellationToken>())
                .ThrowsAsync(new HttpRequestException("Connection refused"));

            using var httpClient = new HttpClient(handler.Object);
            var options = new ConnectorClientOptions();
            options.Transport = new HttpClientTransport(httpClient);
            options.Retry.MaxRetries = 0;

            using var client = CreateClient(options, connectorName: "teams");

            using var request = new HttpRequestMessage(HttpMethod.Get, "https://test.azure.com/api/chats");

            // Act & Assert
            await Assert
                .ThrowsExactlyAsync<RequestFailedException>(
                    () => client.SendAsync(request, TestScopes))
                .ConfigureAwait(continueOnCapturedContext: false);

            Assert.AreEqual(1, capturedActivities.Count);

            var activity = capturedActivities[0];
            Assert.AreEqual("ERROR", activity.GetTagItem("otel.status_code")?.ToString());
            Assert.AreEqual("Connection refused", activity.GetTagItem("otel.status_description")?.ToString());
            Assert.AreEqual(ActivityStatusCode.Error, activity.Status);
        }

        private static ConnectorClientOptions CreateOptionsWithMockTransport(HttpStatusCode statusCode)
        {
            var handler = new Mock<HttpMessageHandler>();
            handler
                .Protected()
                .Setup<Task<HttpResponseMessage>>(
                    "SendAsync",
                    ItExpr.IsAny<HttpRequestMessage>(),
                    ItExpr.IsAny<CancellationToken>())
                .ReturnsAsync(new HttpResponseMessage(statusCode));

            var httpClient = new HttpClient(handler.Object);
            var options = new ConnectorClientOptions();
            options.Transport = new HttpClientTransport(httpClient);
            options.Retry.MaxRetries = 0;
            return options;
        }

        private static ConnectorHttpClient CreateClient(ConnectorClientOptions options, string? connectorName = null)
        {
            var pipeline = HttpPipelineBuilder.Build(options);
            return new ConnectorHttpClient(
                pipeline,
                connectorNameProvider: connectorName is not null ? () => connectorName : null);
        }
    }
}

