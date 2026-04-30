//------------------------------------------------------------
// Copyright (c) Microsoft Corporation.  All rights reserved.
//------------------------------------------------------------

using System.Diagnostics;
using System.Net;
using System.Net.Http;
using Microsoft.Azure.Connectors.Sdk.Authentication;
using Microsoft.Azure.Connectors.Sdk.Http;
using Microsoft.Extensions.Logging.Abstractions;
using Microsoft.VisualStudio.TestTools.UnitTesting;
using Moq;
using Moq.Protected;

namespace Microsoft.Azure.Connectors.Sdk.Tests
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

            var handler = CreateMockHandler(HttpStatusCode.OK);
            using var httpClient = new HttpClient(handler.Object);

            var tokenProvider = CreateMockTokenProvider();

            using var client = new ConnectorHttpClient(
                tokenProvider.Object,
                new ConnectorClientOptions { BaseUri = new Uri("https://test.azure.com") },
                NullLogger.Instance,
                httpClient: httpClient,
                connectorName: "office365");

            using var request = new HttpRequestMessage(HttpMethod.Get, "https://test.azure.com/api/messages");
            request.Headers.Add("x-ms-client-request-id", "test-request-id");

            // Act
            await client.SendAsync(request, TestScopes);

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
            var handler = CreateMockHandler(HttpStatusCode.OK);
            using var httpClient = new HttpClient(handler.Object);

            var tokenProvider = CreateMockTokenProvider();

            using var client = new ConnectorHttpClient(
                tokenProvider.Object,
                new ConnectorClientOptions { BaseUri = new Uri("https://test.azure.com") },
                NullLogger.Instance,
                httpClient: httpClient,
                connectorName: "office365");

            using var request = new HttpRequestMessage(HttpMethod.Get, "https://test.azure.com/api/messages");

            // Act — should complete without Activity overhead
            var response = await client.SendAsync(request, TestScopes);

            // Assert
            Assert.AreEqual(HttpStatusCode.OK, response.StatusCode);
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

            var handler = CreateMockHandler(HttpStatusCode.InternalServerError);
            using var httpClient = new HttpClient(handler.Object);

            var tokenProvider = CreateMockTokenProvider();

            using var client = new ConnectorHttpClient(
                tokenProvider.Object,
                new ConnectorClientOptions
                {
                    BaseUri = new Uri("https://test.azure.com"),
                    MaxRetryAttempts = 0,
                },
                NullLogger.Instance,
                httpClient: httpClient,
                connectorName: "office365");

            using var request = new HttpRequestMessage(HttpMethod.Get, "https://test.azure.com/api/messages");

            // Act
            var response = await client.SendAsync(request, TestScopes);

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

            var handler = CreateMockHandler(HttpStatusCode.OK);
            using var httpClient = new HttpClient(handler.Object);

            var tokenProvider = CreateMockTokenProvider();

            using var client = new ConnectorHttpClient(
                tokenProvider.Object,
                new ConnectorClientOptions { BaseUri = new Uri("https://test.azure.com") },
                NullLogger.Instance,
                httpClient: httpClient,
                connectorName: "sharepointonline");

            using var request = new HttpRequestMessage(HttpMethod.Post, "https://test.azure.com/api/files");

            // Act
            await client.SendAsync(request, TestScopes);

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

            var handler = CreateMockHandler(HttpStatusCode.OK);
            using var httpClient = new HttpClient(handler.Object);

            var tokenProvider = CreateMockTokenProvider();

            using var client = new ConnectorHttpClient(
                tokenProvider.Object,
                new ConnectorClientOptions { BaseUri = new Uri("https://test.azure.com") },
                NullLogger.Instance,
                httpClient: httpClient);

            using var request = new HttpRequestMessage(HttpMethod.Get, "https://test.azure.com/api/test");

            // Act
            await client.SendAsync(request, TestScopes);

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

            var tokenProvider = CreateMockTokenProvider();

            using var client = new ConnectorHttpClient(
                tokenProvider.Object,
                new ConnectorClientOptions
                {
                    BaseUri = new Uri("https://test.azure.com"),
                    MaxRetryAttempts = 0,
                },
                NullLogger.Instance,
                httpClient: httpClient,
                connectorName: "teams");

            using var request = new HttpRequestMessage(HttpMethod.Get, "https://test.azure.com/api/chats");

            // Act & Assert
            await Assert.ThrowsExactlyAsync<HttpRequestException>(
                () => client.SendAsync(request, TestScopes));

            Assert.AreEqual(1, capturedActivities.Count);

            var activity = capturedActivities[0];
            Assert.AreEqual("ERROR", activity.GetTagItem("otel.status_code")?.ToString());
            Assert.AreEqual("Connection refused", activity.GetTagItem("otel.status_description")?.ToString());
            Assert.AreEqual(ActivityStatusCode.Error, activity.Status);
        }

        private static Mock<HttpMessageHandler> CreateMockHandler(HttpStatusCode statusCode)
        {
            var handler = new Mock<HttpMessageHandler>();

            handler
                .Protected()
                .Setup<Task<HttpResponseMessage>>(
                    "SendAsync",
                    ItExpr.IsAny<HttpRequestMessage>(),
                    ItExpr.IsAny<CancellationToken>())
                .ReturnsAsync(new HttpResponseMessage(statusCode));

            return handler;
        }

        private static Mock<ITokenProvider> CreateMockTokenProvider()
        {
            var tokenProvider = new Mock<ITokenProvider>();

            tokenProvider
                .Setup(tp => tp.GetAccessTokenAsync(It.IsAny<string[]>(), It.IsAny<CancellationToken>()))
                .ReturnsAsync("test-token");

            return tokenProvider;
        }
    }
}
