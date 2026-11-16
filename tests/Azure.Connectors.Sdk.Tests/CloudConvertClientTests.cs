//------------------------------------------------------------
// Copyright (c) Microsoft Corporation.  All rights reserved.
//------------------------------------------------------------

using System;
using System.Linq;
using System.Net;
using System.Net.Http;
using System.Text.Json;
using System.Threading;
using System.Threading.Tasks;
using Azure.Connectors.Sdk.CloudConvert;
using Azure.Connectors.Sdk.CloudConvert.Models;
using global::Azure.Core;
using global::Azure.Core.Pipeline;
using Microsoft.VisualStudio.TestTools.UnitTesting;
using Moq;
using Moq.Protected;

namespace Azure.Connectors.Sdk.Tests
{
    [TestClass]
    public class CloudConvertClientTests
    {
        private static readonly Mock<TokenCredential> SharedMockCredential = CreateMockCredential();

        private static Mock<TokenCredential> CreateMockCredential()
        {
            var mock = new Mock<TokenCredential>();
            mock.Setup(credential => credential.GetTokenAsync(It.IsAny<TokenRequestContext>(), It.IsAny<CancellationToken>()))
                .ReturnsAsync(new AccessToken("mock-token", DateTimeOffset.UtcNow.AddHours(1)));
            return mock;
        }

        private static CloudConvertClient CreateMockedClient(
            HttpResponseMessage response,
            Action<HttpRequestMessage>? requestCallback = null)
        {
            var mockHandler = new Mock<HttpMessageHandler>();
            mockHandler.Protected()
                .Setup<Task<HttpResponseMessage>>(
                    "SendAsync",
                    ItExpr.IsAny<HttpRequestMessage>(),
                    ItExpr.IsAny<CancellationToken>())
                .Callback<HttpRequestMessage, CancellationToken>((request, _) => requestCallback?.Invoke(request))
                .ReturnsAsync(response)
                .Verifiable();

            var options = new ConnectorClientOptions();
            options.Transport = new HttpClientTransport(new HttpClient(mockHandler.Object));
            options.Retry.MaxRetries = 0;

            return new CloudConvertClient(
                connectionRuntimeUrl: new Uri("https://test.azure.com/connection"),
                credential: SharedMockCredential.Object,
                options: options);
        }

        [TestMethod]
        public void Constructor_WithValidUrl_CreatesInstance()
        {
            using var client = new CloudConvertClient("https://test.azure.com/connection");

            Assert.IsNotNull(client);
            Assert.AreEqual(expected: "cloudconvert", actual: client.ConnectorName, ignoreCase: false);
        }

        [TestMethod]
        public void Constructor_WithNullUrl_ThrowsArgumentNullException()
        {
            Assert.ThrowsExactly<ArgumentNullException>(() => new CloudConvertClient((string)null!));
        }

        [TestMethod]
        public void Dispose_CalledTwice_DoesNotThrow()
        {
            var client = new CloudConvertClient(
                connectionRuntimeUrl: new Uri("https://test.azure.com/connection"),
                credential: SharedMockCredential.Object);

            client.Dispose();
            client.Dispose();
        }

        [TestMethod]
        public void DiscoveryHelpers_AliasRetention_DeclaresAllFiveMethods()
        {
            var helperNames = typeof(CloudConvertClient)
                .GetMethods()
                .Select(method => method.Name)
                .Where(name => name is
                    nameof(CloudConvertClient.GetConvertOptionsAsync) or
                    nameof(CloudConvertClient.GetOptimizeOptionsAsync) or
                    nameof(CloudConvertClient.GetCaptureWebsiteOptionsAsync) or
                    nameof(CloudConvertClient.GetMergeInputsAsync) or
                    nameof(CloudConvertClient.GetMergeOptionsAsync))
                .OrderBy(name => name, StringComparer.Ordinal)
                .ToArray();

            CollectionAssert.AreEqual(
                expected: new[]
                {
                    nameof(CloudConvertClient.GetCaptureWebsiteOptionsAsync),
                    nameof(CloudConvertClient.GetConvertOptionsAsync),
                    nameof(CloudConvertClient.GetMergeInputsAsync),
                    nameof(CloudConvertClient.GetMergeOptionsAsync),
                    nameof(CloudConvertClient.GetOptimizeOptionsAsync),
                },
                actual: helperNames);
        }

        [TestMethod]
        public async Task ConvertFileAsync_WithMockedResponse_UsesExpectedRouteAndReturnsBytes()
        {
            string? requestMethod = null;
            string? requestUri = null;
            using var responseMessage = new HttpResponseMessage
            {
                StatusCode = HttpStatusCode.OK,
                Content = new ByteArrayContent(new byte[] { 1, 2, 3 }),
            };
            using var client = CreateMockedClient(
                responseMessage,
                request =>
                {
                    requestMethod = request.Method.Method;
                    requestUri = request.RequestUri?.AbsoluteUri;
                });

            var result = await client
                .ConvertFileAsync(new ConvertFileInput(), CancellationToken.None)
                .ConfigureAwait(continueOnCapturedContext: false);

            Assert.AreEqual(expected: "POST", actual: requestMethod);
            Assert.AreEqual(
                expected: "https://test.azure.com/connection/flow/jobs/convertfile",
                actual: requestUri);
            CollectionAssert.AreEqual(expected: new byte[] { 1, 2, 3 }, actual: result);
        }

        [TestMethod]
        public async Task GetConvertOptionsAsync_WithMockedResponse_UsesExpectedRouteAndReturnsPayload()
        {
            string? requestMethod = null;
            string? requestUri = null;
            using var responseMessage = new HttpResponseMessage
            {
                StatusCode = HttpStatusCode.OK,
                Content = new StringContent("{\"data\":{\"quality\":\"high\"}}"),
            };
            using var client = CreateMockedClient(
                responseMessage,
                request =>
                {
                    requestMethod = request.Method.Method;
                    requestUri = request.RequestUri?.AbsoluteUri;
                });

            var result = await client
                .GetConvertOptionsAsync(
                    inputFormat: "docx",
                    outputFormat: "pdf",
                    cancellationToken: CancellationToken.None)
                .ConfigureAwait(continueOnCapturedContext: false);

            Assert.AreEqual(expected: "GET", actual: requestMethod);
            Assert.AreEqual(
                expected: "https://test.azure.com/connection/flow/options/convert?input_format=docx&output_format=pdf",
                actual: requestUri);
            Assert.AreEqual(expected: "high", actual: result.Data?.GetProperty("quality").GetString());
        }

        [TestMethod]
        public async Task GetConvertOptionsAsync_WithErrorResponse_ThrowsConnectorException()
        {
            using var responseMessage = new HttpResponseMessage
            {
                StatusCode = HttpStatusCode.BadRequest,
                Content = new StringContent("{\"error\":\"Bad request\"}"),
            };
            using var client = CreateMockedClient(responseMessage);

            await Assert.ThrowsExactlyAsync<ConnectorException>(() =>
                client.GetConvertOptionsAsync(cancellationToken: CancellationToken.None))
                .ConfigureAwait(continueOnCapturedContext: false);
        }

        [TestMethod]
        public void GetConvertOptionsResponse_JsonRoundTrip_PreservesWireData()
        {
            using var document = JsonDocument.Parse("{\"quality\":\"high\"}");
            var model = CloudConvertModelFactory.GetConvertOptionsResponse(document.RootElement.Clone());

            var json = JsonSerializer.Serialize(model);
            var roundTripped = JsonSerializer.Deserialize<GetConvertOptionsResponse>(json);

            Assert.IsNotNull(roundTripped);
            Assert.AreEqual(expected: "high", actual: roundTripped.Data?.GetProperty("quality").GetString());
        }
    }
}
