//------------------------------------------------------------
// Copyright (c) Microsoft Corporation.  All rights reserved.
//------------------------------------------------------------

using System;
using System.Collections.Generic;
using System.Text.Json;
using System.Net;
using System.Net.Http;
using System.Threading;
using System.Threading.Tasks;
using Azure.Connectors.Sdk.Onenote;
using global::Azure.Core;
using global::Azure.Core.Pipeline;
using Microsoft.VisualStudio.TestTools.UnitTesting;
using Moq;
using Moq.Protected;

namespace Azure.Connectors.Sdk.Tests
{
    [TestClass]
    public class OnenoteClientTests
    {
        private static readonly Mock<TokenCredential> SharedMockCredential = CreateMockCredential();

        private static Mock<TokenCredential> CreateMockCredential()
        {
            var mock = new Mock<TokenCredential>();
            mock.Setup(credential => credential.GetTokenAsync(It.IsAny<TokenRequestContext>(), It.IsAny<CancellationToken>()))
                .ReturnsAsync(new AccessToken("mock-token", DateTimeOffset.UtcNow.AddHours(1)));
            return mock;
        }

        [TestMethod]
        public void Constructor_WithValidUrl_ShouldCreateInstance()
        {
            using var client = new OnenoteClient("https://test.azure.com/conn");
            Assert.IsNotNull(client);
        }

        [TestMethod]
        public void Constructor_WithNull_ShouldThrow()
        {
            Assert.ThrowsExactly<ArgumentNullException>(() => new OnenoteClient((string)null!));
        }

        [TestMethod]
        public void Dispose_ShouldNotThrow()
        {
            var client = new OnenoteClient("https://test.azure.com/conn");
            client.Dispose();
        }

        [TestMethod]
        public void Dispose_CalledTwice_ShouldNotThrow()
        {
            var client = new OnenoteClient(new Uri("https://test.azure.com/conn"), SharedMockCredential.Object);
            client.Dispose();
            client.Dispose();
        }

        private static OnenoteClient CreateMockedClient(HttpResponseMessage response)
        {
            var mockHandler = new Mock<HttpMessageHandler>();
            mockHandler.Protected()
                .Setup<Task<HttpResponseMessage>>("SendAsync", ItExpr.IsAny<HttpRequestMessage>(), ItExpr.IsAny<CancellationToken>())
                .ReturnsAsync(response).Verifiable();
            var options = new ConnectorClientOptions();
            options.Transport = new HttpClientTransport(new HttpClient(mockHandler.Object));
            options.Retry.MaxRetries = 0;
            return new OnenoteClient(new Uri("https://test.azure.com/conn"), SharedMockCredential.Object, options);
        }

        [TestMethod]
        public async Task GetNotebooksAsync_WithErrorResponse_ThrowsConnectorException()
        {
            using var responseMessage = new HttpResponseMessage
            {
                StatusCode = HttpStatusCode.BadRequest,
                Content = new StringContent("{\"error\": \"Bad request\"}")
            };
            using var client = CreateMockedClient(responseMessage);
            await Assert.ThrowsExactlyAsync<ConnectorException>(() =>
                client.GetNotebooksAsync(cancellationToken: CancellationToken.None))
                .ConfigureAwait(continueOnCapturedContext: false);
        }

        [TestMethod]
        public async Task GetPagesInSectionAsync_TwoPagesAndResume_FollowsODataNextLinkAndQueryParameters()
        {
            var requestedUris = new List<string>();
            var handler = new Mock<HttpMessageHandler>();
            handler.Protected()
                .Setup<Task<HttpResponseMessage>>(
                    "SendAsync",
                    ItExpr.IsAny<HttpRequestMessage>(),
                    ItExpr.IsAny<CancellationToken>())
                .Returns((HttpRequestMessage request, CancellationToken _) =>
                {
                    var uri = request.RequestUri!.ToString();
                    requestedUris.Add(uri);
                    if (uri.Contains("$skiptoken=page-2", StringComparison.Ordinal))
                    {
                        return Task.FromResult(new HttpResponseMessage(HttpStatusCode.OK)
                        {
                            Content = new StringContent("""
                                {
                                    "@odata.context": "https://graph.microsoft.com/v1.0/$metadata#pages",
                                    "value": [{ "id": "page-2", "title": "Second Page" }]
                                }
                                """)
                        });
                    }

                    return Task.FromResult(new HttpResponseMessage(HttpStatusCode.OK)
                    {
                        Content = new StringContent("""
                            {
                                "@odata.context": "https://graph.microsoft.com/v1.0/$metadata#pages",
                                "value": [{ "id": "page-1", "title": "First Page" }],
                                "@odata.nextLink": "https://test.azure.com/conn/sections/Dynamic/pages?notebookKey=nb%2F1&sectionId=sec%2B1&$skiptoken=page-2"
                            }
                            """)
                    });
                });

            var options = new ConnectorClientOptions
            {
                Transport = new HttpClientTransport(new HttpClient(handler.Object))
            };
            options.Retry.MaxRetries = 0;

            using var client = new OnenoteClient(new Uri("https://test.azure.com/conn"), SharedMockCredential.Object, options);

            var pageable = client.GetPagesInSectionAsync(notebookKey: "nb/1", notebookSection: "sec+1");
            Assert.AreEqual(expected: 0, actual: requestedUris.Count);

            var pages = new List<global::Azure.Page<JsonElement?>>();
            await foreach (var page in pageable.AsPages().ConfigureAwait(continueOnCapturedContext: false))
            {
                pages.Add(page);
            }

            Assert.AreEqual(expected: 2, actual: pages.Count);
            Assert.AreEqual(
                expected: "https://test.azure.com/conn/sections/Dynamic/pages?notebookKey=nb%2F1&sectionId=sec%2B1",
                actual: requestedUris[0]);
            Assert.AreEqual(
                expected: "https://test.azure.com/conn/sections/Dynamic/pages?notebookKey=nb%2F1&sectionId=sec%2B1&$skiptoken=page-2",
                actual: requestedUris[1]);
            Assert.AreEqual(expected: "page-1", actual: pages[0].Values[0]!.Value.GetProperty("id").GetString());
            Assert.AreEqual(expected: "page-2", actual: pages[1].Values[0]!.Value.GetProperty("id").GetString());
            Assert.AreEqual(expected: requestedUris[1], actual: pages[0].ContinuationToken);
            Assert.IsNull(pages[1].ContinuationToken);

            var resumedPages = new List<global::Azure.Page<JsonElement?>>();
            await foreach (var resumed in pageable.AsPages(continuationToken: pages[0].ContinuationToken).ConfigureAwait(continueOnCapturedContext: false))
            {
                resumedPages.Add(resumed);
            }

            Assert.AreEqual(expected: 1, actual: resumedPages.Count);
            Assert.AreEqual(expected: "page-2", actual: resumedPages[0].Values[0]!.Value.GetProperty("id").GetString());
            Assert.AreEqual(expected: 3, actual: requestedUris.Count);
            Assert.AreEqual(expected: requestedUris[1], actual: requestedUris[2]);
        }
    }
}
