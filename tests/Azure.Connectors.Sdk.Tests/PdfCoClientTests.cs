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
using Azure.Connectors.Sdk.PdfCo;
using Azure.Connectors.Sdk.PdfCo.Models;
using global::Azure.Core;
using global::Azure.Core.Pipeline;
using Microsoft.VisualStudio.TestTools.UnitTesting;
using Moq;
using Moq.Protected;

namespace Azure.Connectors.Sdk.Tests
{
    [TestClass]
    public class PdfCoClientTests
    {
        private static readonly Mock<TokenCredential> SharedMockCredential = CreateMockCredential();

        private static Mock<TokenCredential> CreateMockCredential()
        {
            var mock = new Mock<TokenCredential>();
            mock.Setup(credential => credential.GetTokenAsync(It.IsAny<TokenRequestContext>(), It.IsAny<CancellationToken>()))
                .ReturnsAsync(new AccessToken("mock-token", DateTimeOffset.UtcNow.AddHours(1)));
            return mock;
        }

        private static PdfCoClient CreateMockedClient(HttpResponseMessage response)
        {
            return CreateMockedClient((request, cancellationToken) => Task.FromResult(response));
        }

        private static PdfCoClient CreateMockedClient(Func<HttpRequestMessage, CancellationToken, Task<HttpResponseMessage>> sendAsync)
        {
            var mockHandler = new Mock<HttpMessageHandler>();
            mockHandler.Protected()
                .Setup<Task<HttpResponseMessage>>("SendAsync", ItExpr.IsAny<HttpRequestMessage>(), ItExpr.IsAny<CancellationToken>())
                .Returns(sendAsync).Callback(() => { }).Verifiable();
            var options = new ConnectorClientOptions();
            options.Transport = new HttpClientTransport(new HttpClient(mockHandler.Object));
            options.Retry.MaxRetries = 0;
            return new PdfCoClient(new Uri("https://test.azure.com/conn"), SharedMockCredential.Object, options);
        }

        [TestMethod]
        public void Constructor_WithValidUrl_ShouldCreateInstance()
        {
            using var client = new PdfCoClient("https://test.azure.com/conn");
            Assert.IsNotNull(client);
        }

        [TestMethod]
        public void Constructor_WithNull_ShouldThrow()
        {
            Assert.ThrowsExactly<ArgumentNullException>(() => new PdfCoClient((string)null!));
        }

        [TestMethod]
        public void Dispose_ShouldNotThrow()
        {
            var client = new PdfCoClient("https://test.azure.com/conn");
            client.Dispose();
        }

        [TestMethod]
        public void Dispose_CalledTwice_ShouldNotThrow()
        {
            var client = new PdfCoClient(new Uri("https://test.azure.com/conn"), SharedMockCredential.Object);
            client.Dispose();
            client.Dispose();
        }

        [TestMethod]
        [Owner("daviburg")]
        [DataRow(nameof(PdfCoClient.PdfUnsearchableAsync), "/v1/pdf/makeunsearchable")]
        [DataRow(nameof(PdfCoClient.PdfFromXlsXlsxAsync), "/v1/xls/convert/to/pdf")]
        [DataRow(nameof(PdfCoClient.XlsToCsvAsync), "/v1/xls/convert/to/csv")]
        [DataRow(nameof(PdfCoClient.XlsToHtmlAsync), "/v1/xls/convert/to/html")]
        [DataRow(nameof(PdfCoClient.XlsToJsonAsync), "/v1/xls/convert/to/json")]
        [DataRow(nameof(PdfCoClient.XlsToTxtAsync), "/v1/xls/convert/to/txt")]
        [DataRow(nameof(PdfCoClient.XlsToXmlAsync), "/v1/xls/convert/to/xml")]
        public async Task RenamedOperations_WithMockedRequest_PreserveWireContract(string operation, string path)
        {
            var sourceUrl = string.Equals(operation, nameof(PdfCoClient.PdfUnsearchableAsync), StringComparison.Ordinal)
                ? "https://example.com/source.pdf"
                : "https://example.com/source.xlsx";
            const string resultUrl = "https://example.com/result";
            var attempts = 0;
            using var client = CreateMockedClient(async (request, cancellationToken) =>
            {
                attempts++;
                Assert.AreEqual(expected: HttpMethod.Post, actual: request.Method);
                Assert.AreEqual(expected: new Uri("https://test.azure.com/conn" + path), actual: request.RequestUri);
                Assert.IsNotNull(request.Content);
                var json = await request.Content
                    .ReadAsStringAsync(cancellationToken)
                    .ConfigureAwait(continueOnCapturedContext: false);
                using var document = JsonDocument.Parse(json);
                CollectionAssert.AreEquivalent(
                    expected: new[] { "url" },
                    actual: document.RootElement.EnumerateObject().Select(property => property.Name).ToArray());
                Assert.AreEqual(expected: sourceUrl, actual: document.RootElement.GetProperty("url").GetString());
                return new HttpResponseMessage(HttpStatusCode.OK)
                {
                    Content = new StringContent("""{"url":"https://example.com/result","error":false,"status":200}"""),
                };
            });

            var result = operation switch
            {
                nameof(PdfCoClient.PdfUnsearchableAsync) => JsonSerializer.SerializeToElement(
                    await client
                        .PdfUnsearchableAsync(input: PdfCoModelFactory.PdfUnsearchableInput(url: sourceUrl))
                        .ConfigureAwait(continueOnCapturedContext: false)),
                nameof(PdfCoClient.PdfFromXlsXlsxAsync) => JsonSerializer.SerializeToElement(
                    await client
                        .PdfFromXlsXlsxAsync(input: PdfCoModelFactory.PdfFromXlsXlsxInput(url: sourceUrl))
                        .ConfigureAwait(continueOnCapturedContext: false)),
                nameof(PdfCoClient.XlsToCsvAsync) => JsonSerializer.SerializeToElement(
                    await client
                        .XlsToCsvAsync(input: PdfCoModelFactory.XlsToCsvInput(url: sourceUrl))
                        .ConfigureAwait(continueOnCapturedContext: false)),
                nameof(PdfCoClient.XlsToHtmlAsync) => JsonSerializer.SerializeToElement(
                    await client
                        .XlsToHtmlAsync(input: PdfCoModelFactory.XlsToHtmlInput(url: sourceUrl))
                        .ConfigureAwait(continueOnCapturedContext: false)),
                nameof(PdfCoClient.XlsToJsonAsync) => JsonSerializer.SerializeToElement(
                    await client
                        .XlsToJsonAsync(input: PdfCoModelFactory.XlsToJsonInput(url: sourceUrl))
                        .ConfigureAwait(continueOnCapturedContext: false)),
                nameof(PdfCoClient.XlsToTxtAsync) => JsonSerializer.SerializeToElement(
                    await client
                        .XlsToTxtAsync(input: PdfCoModelFactory.XlsToTxtInput(url: sourceUrl))
                        .ConfigureAwait(continueOnCapturedContext: false)),
                nameof(PdfCoClient.XlsToXmlAsync) => JsonSerializer.SerializeToElement(
                    await client
                        .XlsToXmlAsync(input: PdfCoModelFactory.XlsToXmlInput(url: sourceUrl))
                        .ConfigureAwait(continueOnCapturedContext: false)),
                _ => throw new AssertFailedException("Unexpected PDF.co operation."),
            };
            var factoryResult = operation switch
            {
                nameof(PdfCoClient.PdfUnsearchableAsync) => JsonSerializer.SerializeToElement(PdfCoModelFactory.PdfUnsearchableResponse(url: resultUrl, error: false, status: 200)),
                nameof(PdfCoClient.PdfFromXlsXlsxAsync) => JsonSerializer.SerializeToElement(PdfCoModelFactory.PdfFromXlsXlsxResponse(url: resultUrl, error: false, status: 200)),
                nameof(PdfCoClient.XlsToCsvAsync) => JsonSerializer.SerializeToElement(PdfCoModelFactory.XlsToCsvResponse(url: resultUrl, error: false, status: 200)),
                nameof(PdfCoClient.XlsToHtmlAsync) => JsonSerializer.SerializeToElement(PdfCoModelFactory.XlsToHtmlResponse(url: resultUrl, error: false, status: 200)),
                nameof(PdfCoClient.XlsToJsonAsync) => JsonSerializer.SerializeToElement(PdfCoModelFactory.XlsToJsonResponse(url: resultUrl, error: false, status: 200)),
                nameof(PdfCoClient.XlsToTxtAsync) => JsonSerializer.SerializeToElement(PdfCoModelFactory.XlsToTxtResponse(url: resultUrl, error: false, status: 200)),
                nameof(PdfCoClient.XlsToXmlAsync) => JsonSerializer.SerializeToElement(PdfCoModelFactory.XlsToXmlResponse(url: resultUrl, error: false, status: 200)),
                _ => throw new AssertFailedException("Unexpected PDF.co factory."),
            };

            Assert.AreEqual(expected: 1, actual: attempts);
            foreach (var response in new[] { result, factoryResult })
            {
                Assert.AreEqual(expected: resultUrl, actual: response.GetProperty("url").GetString());
                Assert.AreEqual(expected: false, actual: response.GetProperty("error").GetBoolean());
                Assert.AreEqual(expected: 200, actual: response.GetProperty("status").GetInt32());
            }
        }

        [TestMethod]
        public async Task HtmlToPdfAsync_WithErrorResponse_ThrowsConnectorException()
        {
            using var responseMessage = new HttpResponseMessage
            {
                StatusCode = HttpStatusCode.BadRequest,
                Content = new StringContent("{\"error\": \"Bad request\"}")
            };
            using var client = CreateMockedClient(responseMessage);
            await Assert.ThrowsExactlyAsync<ConnectorException>(() =>
                client.HtmlToPdfAsync(input: new Azure.Connectors.Sdk.PdfCo.Models.HtmlToPdfInput(), cancellationToken: CancellationToken.None))
                .ConfigureAwait(continueOnCapturedContext: false);
        }
    }
}
