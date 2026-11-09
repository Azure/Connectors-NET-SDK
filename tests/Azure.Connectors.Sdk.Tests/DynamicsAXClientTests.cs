//------------------------------------------------------------
// Copyright (c) Microsoft Corporation.  All rights reserved.
//------------------------------------------------------------

using System;
using System.Collections.Generic;
using System.Net;
using System.Net.Http;
using System.Threading;
using System.Threading.Tasks;
using Azure.Connectors.Sdk.DynamicsAX;
using global::Azure.Core;
using global::Azure.Core.Pipeline;
using Microsoft.VisualStudio.TestTools.UnitTesting;
using Moq;
using Moq.Protected;

namespace Azure.Connectors.Sdk.Tests
{
    [TestClass]
    public class DynamicsAXClientTests
    {
        private static readonly Mock<TokenCredential> SharedMockCredential = CreateMockCredential();

        private static Mock<TokenCredential> CreateMockCredential()
        {
            var mock = new Mock<TokenCredential>();
            mock.Setup(credential => credential.GetTokenAsync(It.IsAny<TokenRequestContext>(), It.IsAny<CancellationToken>()))
                .ReturnsAsync(new AccessToken("mock-token", DateTimeOffset.UtcNow.AddHours(1)));
            return mock;
        }

        private static DynamicsAXClient CreateMockedClient(HttpResponseMessage response)
        {
            return CreateMockedClient((request, cancellationToken) => Task.FromResult(response));
        }

        private static DynamicsAXClient CreateMockedClient(Func<HttpRequestMessage, CancellationToken, Task<HttpResponseMessage>> sendAsync)
        {
            var mockHandler = new Mock<HttpMessageHandler>();
            mockHandler.Protected()
                .Setup<Task<HttpResponseMessage>>(
                    "SendAsync",
                    ItExpr.IsAny<HttpRequestMessage>(),
                    ItExpr.IsAny<CancellationToken>())
                .Returns(sendAsync)
                .Callback(() => { })
                .Verifiable();

            var options = new ConnectorClientOptions();
            options.Transport = new HttpClientTransport(new HttpClient(mockHandler.Object));
            options.Retry.MaxRetries = 0;

            return new DynamicsAXClient(
                connectionRuntimeUrl: new Uri("https://test.azure.com/connection"),
                credential: SharedMockCredential.Object,
                options: options);
        }

        [TestMethod]
        public void Constructor_WithValidUrl_ShouldCreateInstance()
        {
            using var client = new DynamicsAXClient("https://test.azure.com/connection");
            Assert.IsNotNull(client);
        }

        [TestMethod]
        public void Constructor_WithNullUrl_ShouldThrowArgumentNullException()
        {
            Assert.ThrowsExactly<ArgumentNullException>(() => new DynamicsAXClient((string)null!));
        }

        [TestMethod]
        public void Dispose_ShouldNotThrow()
        {
            var client = new DynamicsAXClient("https://test.azure.com/connection");
            client.Dispose();
        }

        [TestMethod]
        public void Dispose_CalledTwice_ShouldNotThrow()
        {
            var client = new DynamicsAXClient(
                connectionRuntimeUrl: new Uri("https://test.azure.com/connection"),
                credential: SharedMockCredential.Object);
            client.Dispose();
            client.Dispose();
        }

        [TestMethod]
        [Owner("daviburg")]
        [DataRow(false)]
        [DataRow(true)]
        public async Task GetItemsAsync_WithContinuation_ReturnsPagesAndDynamicFields(bool resume)
        {
            const string firstUrl = "https://test.azure.com/connection/datasets/instance/tables/Customers/items";
            const string nextUrl = firstUrl + "?$skiptoken=second";
            var requests = new List<string>();
            using var client = CreateMockedClient((request, cancellationToken) =>
            {
                Assert.AreEqual(expected: HttpMethod.Get, actual: request.Method);
                Assert.AreEqual(expected: new Uri(resume || requests.Count > 0 ? nextUrl : firstUrl), actual: request.RequestUri);
                requests.Add(request.RequestUri?.AbsoluteUri ?? string.Empty);
                return Task.FromResult(new HttpResponseMessage(HttpStatusCode.OK)
                {
                    Content = new StringContent(!resume && requests.Count == 1
                        ? $$"""{"value":[{"id":"first","AccountNum":"1001"}],"@odata.nextLink":"{{nextUrl}}"}"""
                        : """{"value":[{"id":"second","AccountNum":"1002"}]}"""),
                });
            });
            var identifiers = new List<string>();
            var accounts = new List<string>();
            var continuations = new List<string?>();
            await foreach (var page in client.GetItemsAsync(instance: "instance", entityName: "Customers")
                .AsPages(continuationToken: resume ? nextUrl : null)
                .ConfigureAwait(continueOnCapturedContext: false))
            {
                Assert.AreEqual(expected: 1, actual: page.Values.Count);
                var item = page.Values[0];
                identifiers.Add(item.AdditionalProperties["id"].GetString()!);
                accounts.Add(item.AdditionalProperties["AccountNum"].GetString()!);
                continuations.Add(page.ContinuationToken);
            }

            CollectionAssert.AreEqual(expected: resume ? new[] { "second" } : new[] { "first", "second" }, actual: identifiers);
            CollectionAssert.AreEqual(expected: resume ? new[] { "1002" } : new[] { "1001", "1002" }, actual: accounts);
            CollectionAssert.AreEqual(expected: resume ? new[] { nextUrl } : new[] { firstUrl, nextUrl }, actual: requests);
            CollectionAssert.AreEqual(expected: resume ? new string?[] { null } : new string?[] { nextUrl, null }, actual: continuations);
        }

        [TestMethod]
        [Owner("daviburg")]
        public async Task GetItemsAsync_WithSecondPageError_ThrowsConnectorException()
        {
            const string firstUrl = "https://test.azure.com/connection/datasets/instance/tables/Customers/items";
            const string nextUrl = firstUrl + "?$skiptoken=second";
            var attempts = 0;
            using var client = CreateMockedClient((request, cancellationToken) =>
            {
                Assert.AreEqual(expected: new Uri(attempts == 0 ? firstUrl : nextUrl), actual: request.RequestUri);
                attempts++;
                return Task.FromResult(new HttpResponseMessage(attempts == 1 ? HttpStatusCode.OK : HttpStatusCode.BadRequest)
                {
                    Content = new StringContent(attempts == 1
                        ? $$"""{"value":[{"id":"first"}],"@odata.nextLink":"{{nextUrl}}"}"""
                        : """{"error":{"code":"InvalidContinuation","message":"Rejected."}}"""),
                });
            });

            await Assert.ThrowsExactlyAsync<ConnectorException>(async () =>
            {
                await foreach (var item in client.GetItemsAsync(instance: "instance", entityName: "Customers")
                    .ConfigureAwait(continueOnCapturedContext: false))
                {
                    Assert.AreEqual(expected: "first", actual: item.AdditionalProperties["id"].GetString());
                }
            }).ConfigureAwait(continueOnCapturedContext: false);

            Assert.AreEqual(expected: 2, actual: attempts);
        }

        [TestMethod]
        public async Task GetProcedureAsync_WithMockedResponse_ReturnsExpected()
        {
            using var responseMessage = new HttpResponseMessage
            {
                StatusCode = HttpStatusCode.OK,
                Content = new StringContent("{}")
            };

            using var client = CreateMockedClient(responseMessage);

            var result = await client
                .GetProcedureAsync(instance: "inst1",
                    action: "act1",
                    cancellationToken: CancellationToken.None)
                .ConfigureAwait(continueOnCapturedContext: false);

            Assert.IsNotNull(result);
        }

        [TestMethod]
        public async Task GetProcedureAsync_WithErrorResponse_ThrowsConnectorException()
        {
            using var responseMessage = new HttpResponseMessage
            {
                StatusCode = HttpStatusCode.BadRequest,
                Content = new StringContent("{\"error\": \"Bad request\"}")
            };

            using var client = CreateMockedClient(responseMessage);

            await Assert.ThrowsExactlyAsync<ConnectorException>(() =>
                client.GetProcedureAsync(instance: "inst1",
                    action: "act1",
                    cancellationToken: CancellationToken.None))
                .ConfigureAwait(continueOnCapturedContext: false);
        }

    }
}
