//------------------------------------------------------------
// Copyright (c) Microsoft Corporation.  All rights reserved.
//------------------------------------------------------------

using System;
using System.Net;
using System.Net.Http;
using System.Threading;
using System.Threading.Tasks;
using global::Azure.Core;
using global::Azure.Core.Pipeline;
using Microsoft.VisualStudio.TestTools.UnitTesting;
using Moq;
using Moq.Protected;

namespace Azure.Connectors.Sdk.Tests
{
    [TestClass]
    public class ConnectorNamespaceTriggerConfigManagementResolverTests
    {
        private static readonly DateTimeOffset FarFutureExpiry = new(2099, 1, 1, 0, 0, 0, TimeSpan.Zero);

        private static readonly AccessToken TestAccessToken = new(
            accessToken: "mock-token",
            expiresOn: ConnectorNamespaceTriggerConfigManagementResolverTests.FarFutureExpiry);

        private static ConnectorNamespaceTriggerConfigResourceIdentity CreateResourceIdentity()
        {
            return new ConnectorNamespaceTriggerConfigResourceIdentity(
                SubscriptionId: "11111111-2222-3333-4444-555555555555",
                ResourceGroupName: "prod-connectors-rg",
                ConnectorNamespaceName: "my-gateway",
                TriggerConfigName: "email-trigger");
        }

        [TestMethod]
        public async Task GetTriggerConfigAsync_ValidResponse_ReturnsTriggerConfigAndBuildsExpectedRequest()
        {
            // Arrange
            const string responseJson = """
                {
                  "properties": {
                    "operationName": "OnNewFilesV2",
                    "connectionDetails": {
                      "connectorName": "onedriveforbusiness"
                    }
                  }
                }
                """;

            var resourceIdentity = ConnectorNamespaceTriggerConfigManagementResolverTests.CreateResourceIdentity();
            var (resolver, getLastRequest) = ConnectorNamespaceTriggerConfigManagementResolverTests.CreateResolver(
                responseFactory: () => new HttpResponseMessage(HttpStatusCode.OK)
                {
                    Content = new StringContent(responseJson),
                });

            // Act
            var triggerConfig = await resolver
                .GetTriggerConfigAsync(resourceIdentity)
                .ConfigureAwait(continueOnCapturedContext: false);

            // Assert
            Assert.AreEqual("onedriveforbusiness", triggerConfig.ConnectorName);
            Assert.AreEqual("OnNewFilesV2", triggerConfig.OperationName);

            var request = getLastRequest();
            Assert.IsNotNull(request);
            Assert.AreEqual(HttpMethod.Get, request.Method);
            Assert.AreEqual(
                "https://management.azure.com/subscriptions/11111111-2222-3333-4444-555555555555/resourceGroups/prod-connectors-rg/providers/Microsoft.Web/connectorGateways/my-gateway/triggerconfigs/email-trigger?api-version=2026-05-01-preview",
                request.RequestUri?.AbsoluteUri);
            Assert.AreEqual("Bearer", request.Headers.Authorization?.Scheme);
            Assert.AreEqual("mock-token", request.Headers.Authorization?.Parameter);
        }

        [TestMethod]
        public async Task GetTriggerConfigAsync_NotFound_ThrowsConfigurationResolutionException()
        {
            // Arrange
            const string secretBody = "{\"error\":\"secret-value\"}";
            var resourceIdentity = ConnectorNamespaceTriggerConfigManagementResolverTests.CreateResourceIdentity();
            var (resolver, _) = ConnectorNamespaceTriggerConfigManagementResolverTests.CreateResolver(
                responseFactory: () => new HttpResponseMessage(HttpStatusCode.NotFound)
                {
                    Content = new StringContent(secretBody),
                });

            // Act
            var exception = await Assert.ThrowsExactlyAsync<ConnectorTriggerConfigurationResolutionException>(
                async () => await resolver
                    .GetTriggerConfigAsync(resourceIdentity)
                    .ConfigureAwait(continueOnCapturedContext: false))
                .ConfigureAwait(continueOnCapturedContext: false);

            // Assert
            Assert.AreEqual(404, exception.Status);
            Assert.IsFalse(exception.Message.Contains(secretBody, StringComparison.Ordinal));
            StringAssert.Contains(exception.Message, "404");
        }

        [TestMethod]
        public async Task GetTriggerConfigAsync_Unauthorized_ThrowsConfigurationResolutionException()
        {
            // Arrange
            var resourceIdentity = ConnectorNamespaceTriggerConfigManagementResolverTests.CreateResourceIdentity();
            var (resolver, _) = ConnectorNamespaceTriggerConfigManagementResolverTests.CreateResolver(
                responseFactory: () => new HttpResponseMessage(HttpStatusCode.Unauthorized));

            // Act
            var exception = await Assert.ThrowsExactlyAsync<ConnectorTriggerConfigurationResolutionException>(
                async () => await resolver
                    .GetTriggerConfigAsync(resourceIdentity)
                    .ConfigureAwait(continueOnCapturedContext: false))
                .ConfigureAwait(continueOnCapturedContext: false);

            // Assert
            Assert.AreEqual(401, exception.Status);
        }

        [TestMethod]
        public async Task GetTriggerConfigAsync_MalformedResponse_ThrowsConfigurationResolutionException()
        {
            // Arrange
            const string malformedJson = "{\"properties\":{\"connectionDetails\":{}}}";
            var resourceIdentity = ConnectorNamespaceTriggerConfigManagementResolverTests.CreateResourceIdentity();
            var (resolver, _) = ConnectorNamespaceTriggerConfigManagementResolverTests.CreateResolver(
                responseFactory: () => new HttpResponseMessage(HttpStatusCode.OK)
                {
                    Content = new StringContent(malformedJson),
                });

            // Act
            var exception = await Assert.ThrowsExactlyAsync<ConnectorTriggerConfigurationResolutionException>(
                async () => await resolver
                    .GetTriggerConfigAsync(resourceIdentity)
                    .ConfigureAwait(continueOnCapturedContext: false))
                .ConfigureAwait(continueOnCapturedContext: false);

            // Assert
            Assert.AreEqual(200, exception.Status);
            StringAssert.Contains(exception.Message, "required trigger configuration properties");
        }

        [TestMethod]
        public async Task GetTriggerConfigAsync_TransportFailure_ThrowsConfigurationResolutionException()
        {
            // Arrange
            var resourceIdentity = ConnectorNamespaceTriggerConfigManagementResolverTests.CreateResourceIdentity();
            var credential = ConnectorNamespaceTriggerConfigManagementResolverTests.CreateCredential();
            var handler = new Mock<HttpMessageHandler>();
            handler
                .Protected()
                .Setup<Task<HttpResponseMessage>>(
                    "SendAsync",
                    ItExpr.IsAny<HttpRequestMessage>(),
                    ItExpr.IsAny<CancellationToken>())
                .ThrowsAsync(new HttpRequestException("Connection refused"));

            var options = new ConnectorNamespaceTriggerConfigManagementResolverOptions();
            options.Transport = new HttpClientTransport(new HttpClient(handler.Object));
            options.Retry.MaxRetries = 0;
            var resolver = new ConnectorNamespaceTriggerConfigManagementResolver(credential.Object, options);

            // Act
            var exception = await Assert.ThrowsExactlyAsync<ConnectorTriggerConfigurationResolutionException>(
                async () => await resolver
                    .GetTriggerConfigAsync(resourceIdentity)
                    .ConfigureAwait(continueOnCapturedContext: false))
                .ConfigureAwait(continueOnCapturedContext: false);

            // Assert
            Assert.IsNotNull(exception.InnerException);
            StringAssert.Contains(exception.Message, "management request failed");
        }

        [TestMethod]
        public async Task GetTriggerConfigAsync_Cancelled_ThrowsOperationCanceledException()
        {
            // Arrange
            var resourceIdentity = ConnectorNamespaceTriggerConfigManagementResolverTests.CreateResourceIdentity();
            var credential = ConnectorNamespaceTriggerConfigManagementResolverTests.CreateCredential();
            var handler = new Mock<HttpMessageHandler>();
            handler
                .Protected()
                .Setup<Task<HttpResponseMessage>>(
                    "SendAsync",
                    ItExpr.IsAny<HttpRequestMessage>(),
                    ItExpr.IsAny<CancellationToken>())
                .Returns<HttpRequestMessage, CancellationToken>((request, cancellationToken) => Task.FromCanceled<HttpResponseMessage>(cancellationToken));

            var options = new ConnectorNamespaceTriggerConfigManagementResolverOptions();
            options.Transport = new HttpClientTransport(new HttpClient(handler.Object));
            options.Retry.MaxRetries = 0;
            var resolver = new ConnectorNamespaceTriggerConfigManagementResolver(credential.Object, options);
            using var cancellationSource = new CancellationTokenSource();
            await cancellationSource.CancelAsync().ConfigureAwait(continueOnCapturedContext: false);

            // Act & Assert
            await Assert.ThrowsAsync<OperationCanceledException>(
                async () => await resolver
                    .GetTriggerConfigAsync(resourceIdentity, cancellationSource.Token)
                    .ConfigureAwait(continueOnCapturedContext: false))
                .ConfigureAwait(continueOnCapturedContext: false);
        }

        private static Mock<TokenCredential> CreateCredential()
        {
            var credential = new Mock<TokenCredential>();
            credential
                .Setup(mock => mock.GetTokenAsync(It.IsAny<TokenRequestContext>(), It.IsAny<CancellationToken>()))
                .ReturnsAsync(ConnectorNamespaceTriggerConfigManagementResolverTests.TestAccessToken);
            return credential;
        }

        private static (ConnectorNamespaceTriggerConfigManagementResolver Resolver, Func<HttpRequestMessage?> GetLastRequest) CreateResolver(
            Func<HttpResponseMessage> responseFactory)
        {
            var credential = ConnectorNamespaceTriggerConfigManagementResolverTests.CreateCredential();
            HttpRequestMessage? lastRequest = null;
            var handler = new Mock<HttpMessageHandler>();
            handler
                .Protected()
                .Setup<Task<HttpResponseMessage>>(
                    "SendAsync",
                    ItExpr.IsAny<HttpRequestMessage>(),
                    ItExpr.IsAny<CancellationToken>())
                .Callback<HttpRequestMessage, CancellationToken>((request, cancellationToken) => lastRequest = request)
                .Returns(() => Task.FromResult(responseFactory()));

            var options = new ConnectorNamespaceTriggerConfigManagementResolverOptions();
            options.Transport = new HttpClientTransport(new HttpClient(handler.Object));
            options.Retry.MaxRetries = 0;

            return (
                new ConnectorNamespaceTriggerConfigManagementResolver(credential.Object, options),
                () => lastRequest);
        }
    }
}
