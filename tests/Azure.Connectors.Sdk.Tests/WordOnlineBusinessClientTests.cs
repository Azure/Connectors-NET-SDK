//------------------------------------------------------------
// Copyright (c) Microsoft Corporation.  All rights reserved.
//------------------------------------------------------------

using System;
using System.Net;
using System.Net.Http;
using System.Text;
using System.Text.Json;
using System.Threading;
using System.Threading.Tasks;
using Azure.Connectors.Sdk.WordOnlineBusiness;
using Azure.Connectors.Sdk.WordOnlineBusiness.Models;
using global::Azure.Core;
using global::Azure.Core.Pipeline;
using Microsoft.VisualStudio.TestTools.UnitTesting;
using Moq;
using Moq.Protected;

namespace Azure.Connectors.Sdk.Tests
{
    /// <summary>
    /// Tests for the generated WordOnlineBusinessClient class.
    /// </summary>
    [TestClass]
    public class WordOnlineBusinessClientTests
    {
        private static readonly Mock<TokenCredential> SharedMockCredential = CreateMockCredential();

        private static Mock<TokenCredential> CreateMockCredential()
        {
            var mock = new Mock<TokenCredential>();
            mock
                .Setup(credential => credential.GetTokenAsync(It.IsAny<TokenRequestContext>(), It.IsAny<CancellationToken>()))
                .ReturnsAsync(new AccessToken("mock-token", DateTimeOffset.UtcNow.AddHours(1)));
            return mock;
        }

        private static WordOnlineBusinessClient CreateMockedClient(HttpResponseMessage response)
        {
            var mockHandler = new Mock<HttpMessageHandler>();
            mockHandler.Protected()
                .Setup<Task<HttpResponseMessage>>(
                    "SendAsync",
                    ItExpr.IsAny<HttpRequestMessage>(),
                    ItExpr.IsAny<CancellationToken>())
                .ReturnsAsync(response)
                .Callback(() => { })
                .Verifiable();

            var options = new ConnectorClientOptions();
            options.Transport = new HttpClientTransport(new HttpClient(mockHandler.Object));
            options.Retry.MaxRetries = 0;

            return new WordOnlineBusinessClient(
                connectionRuntimeUrl: new Uri("https://test.azure.com/connection"),
                credential: SharedMockCredential.Object,
                options: options);
        }

        [TestMethod]
        public void Constructor_WithValidConnectionRuntimeUrl_ShouldCreateInstance()
        {
            using var client = new WordOnlineBusinessClient("https://test.azure.com/connection");
            Assert.IsNotNull(client);
        }

        [TestMethod]
        public void Constructor_WithNullConnectionRuntimeUrl_ShouldThrowArgumentNullException()
        {
            Assert.ThrowsExactly<ArgumentNullException>(() => new WordOnlineBusinessClient((string)null!));
        }

        [TestMethod]
        public void Dispose_ShouldNotThrow()
        {
            var client = new WordOnlineBusinessClient("https://test.azure.com/connection");
            client.Dispose();
        }

        [TestMethod]
        public void Dispose_CalledTwice_ShouldNotThrow()
        {
            var client = new WordOnlineBusinessClient(
                connectionRuntimeUrl: new Uri("https://test.azure.com/connection"),
                credential: SharedMockCredential.Object);
            client.Dispose();
            client.Dispose();
        }

        [TestMethod]
        public async Task GetFileSchemaAsync_WithMockedResponse_ReturnsExpectedResult()
        {
            var expectedResponse = new GetFileSchemaResponse();

            using var responseMessage = new HttpResponseMessage
            {
                StatusCode = HttpStatusCode.OK,
                Content = new StringContent(JsonSerializer.Serialize(expectedResponse))
            };

            using var client = CreateMockedClient(responseMessage);

            var result = await client
                .GetFileSchemaAsync(
                    source: "me",
                    drive: "OneDrive",
                    file: "template.docx",
                    cancellationToken: CancellationToken.None)
                .ConfigureAwait(continueOnCapturedContext: false);

            Assert.IsNotNull(result);
        }

        [TestMethod]
        public async Task GetFileSchemaAsync_WithErrorResponse_ThrowsConnectorException()
        {
            using var responseMessage = new HttpResponseMessage
            {
                StatusCode = HttpStatusCode.NotFound,
                Content = new StringContent("{\"error\": \"Not Found\"}")
            };

            using var client = CreateMockedClient(responseMessage);

            await Assert.ThrowsExactlyAsync<ConnectorException>(() =>
                client.GetFileSchemaAsync(
                    source: "me",
                    drive: "OneDrive",
                    file: "nonexistent.docx",
                    cancellationToken: CancellationToken.None))
                .ConfigureAwait(continueOnCapturedContext: false);
        }

        [TestMethod]
        public async Task CreateWordFileWithContentAsync_WithMockedResponse_ReturnsFilename()
        {
            var expectedFilename = "output.docx";

            using var responseMessage = new HttpResponseMessage
            {
                StatusCode = HttpStatusCode.OK,
                Content = new StringContent(JsonSerializer.Serialize(expectedFilename))
            };

            using var client = CreateMockedClient(responseMessage);

            var result = await client
                .CreateWordFileWithContentAsync(
                    input: new ContentBody { Content = "<p>Hello World</p>" },
                    fileName: "output.docx",
                    cancellationToken: CancellationToken.None)
                .ConfigureAwait(continueOnCapturedContext: false);

            Assert.IsNotNull(result);
            Assert.AreEqual(expectedFilename, result);
        }

        [TestMethod]
        public void ContentBody_SerializationRoundTrip()
        {
            var original = new ContentBody { Content = "<h1>My Heading</h1>" };

            var json = JsonSerializer.Serialize(original);
            var deserialized = JsonSerializer.Deserialize<ContentBody>(json);

            Assert.IsNotNull(deserialized);
            Assert.AreEqual("<h1>My Heading</h1>", deserialized.Content);
        }

        [TestMethod]
        public void GetFileSchemaResponse_HasAdditionalPropertiesDictionary()
        {
            const string json = "{\"$schema\":\"http://json-schema.org/draft-04/schema#\",\"dynamicField\":\"dynamicValue\"}";
            var schema = JsonSerializer.Deserialize<GetFileSchemaResponse>(json);
            Assert.IsNotNull(schema);
            Assert.IsNotNull(schema.AdditionalProperties);
            Assert.IsTrue(schema.AdditionalProperties.ContainsKey("dynamicField"));
            Assert.AreEqual("dynamicValue", ((JsonElement)schema.AdditionalProperties["dynamicField"]).GetString());
        }
    }
}
