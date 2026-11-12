//------------------------------------------------------------
// Copyright (c) Microsoft Corporation.  All rights reserved.
//------------------------------------------------------------

using System;
using System.Collections.Generic;
using System.Net;
using System.Net.Http;
using System.Threading;
using System.Threading.Tasks;
using Azure.Connectors.Sdk.Zendesk;
using global::Azure.Core;
using global::Azure.Core.Pipeline;
using Microsoft.VisualStudio.TestTools.UnitTesting;
using Moq;
using Moq.Protected;

namespace Azure.Connectors.Sdk.Tests
{
    [TestClass]
    public class ZendeskClientTests
    {
        private static readonly Mock<TokenCredential> SharedMockCredential = CreateMockCredential();

        private static Mock<TokenCredential> CreateMockCredential()
        {
            var mock = new Mock<TokenCredential>();
            mock.Setup(credential => credential.GetTokenAsync(It.IsAny<TokenRequestContext>(), It.IsAny<CancellationToken>()))
                .ReturnsAsync(new AccessToken("mock-token", DateTimeOffset.UtcNow.AddHours(1)));
            return mock;
        }

        private static ZendeskClient CreateMockedClient(HttpResponseMessage response)
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

            return new ZendeskClient(
                connectionRuntimeUrl: new Uri("https://test.azure.com/connection"),
                credential: SharedMockCredential.Object,
                options: options);
        }

        [TestMethod]
        public void Constructor_WithValidUrl_ShouldCreateInstance()
        {
            using var client = new ZendeskClient("https://test.azure.com/connection");
            Assert.IsNotNull(client);
        }

        [TestMethod]
        public void Constructor_WithNullUrl_ShouldThrowArgumentNullException()
        {
            Assert.ThrowsExactly<ArgumentNullException>(() => new ZendeskClient((string)null!));
        }

        [TestMethod]
        public void Dispose_ShouldNotThrow()
        {
            var client = new ZendeskClient("https://test.azure.com/connection");
            client.Dispose();
        }

        [TestMethod]
        public void Dispose_CalledTwice_ShouldNotThrow()
        {
            var client = new ZendeskClient(
                connectionRuntimeUrl: new Uri("https://test.azure.com/connection"),
                credential: SharedMockCredential.Object);
            client.Dispose();
            client.Dispose();
        }

        [TestMethod]
        public async Task SearchArticlesAsync_WithMockedResponse_ReturnsExpected()
        {
            using var responseMessage = new HttpResponseMessage
            {
                StatusCode = HttpStatusCode.OK,
                Content = new StringContent("{}")
            };

            using var client = CreateMockedClient(responseMessage);

            var result = await client
                .SearchArticlesAsync(query: "test",
                    cancellationToken: CancellationToken.None)
                .ConfigureAwait(continueOnCapturedContext: false);

            Assert.IsNotNull(result);
        }

        [TestMethod]
        public async Task SearchArticlesAsync_WithErrorResponse_ThrowsConnectorException()
        {
            using var responseMessage = new HttpResponseMessage
            {
                StatusCode = HttpStatusCode.BadRequest,
                Content = new StringContent("{\"error\": \"Bad request\"}")
            };

            using var client = CreateMockedClient(responseMessage);

            await Assert.ThrowsExactlyAsync<ConnectorException>(() =>
                client.SearchArticlesAsync(query: "test",
                    cancellationToken: CancellationToken.None))
                .ConfigureAwait(continueOnCapturedContext: false);
        }

        [TestMethod]
        public async Task PostItemAsync_TableNameWithSlash_DoubleEncodesPathSegment()
        {
            var clientSetup = ConnectorTestHelpers.CreateCapturingClientSetup(
                () => new HttpResponseMessage
                {
                    StatusCode = HttpStatusCode.OK,
                    Content = new StringContent("{}")
                });
            using var client = new ZendeskClient(
                connectionRuntimeUrl: new Uri("https://test.azure.com/connection"),
                credential: clientSetup.Credential,
                options: clientSetup.Options);

            await client
                .PostItemAsync(tableName: "a/b", input: new Zendesk.Models.Item(), cancellationToken: CancellationToken.None)
                .ConfigureAwait(continueOnCapturedContext: false);

            var request = clientSetup.GetLastRequest();
            Assert.IsNotNull(request);
            Assert.IsTrue(request.RequestUri!.AbsolutePath.Contains("/tables/a%252Fb/items", StringComparison.Ordinal));
        }

        [TestMethod]
        public async Task GetTablesAsync_TargetsDefaultDatasetRoute()
        {
            var request = await CaptureRequestAsync(
                    responseJson: "{\"value\":[]}",
                    invoke: client => client.GetTablesAsync(cancellationToken: CancellationToken.None))
                .ConfigureAwait(continueOnCapturedContext: false);

            Assert.AreEqual(
                expected: "/connection/datasets/default/tables",
                actual: request.RequestUri!.AbsolutePath,
                message: "GetTables is a public advanced operation on the default dataset. It was previously lost to its x-ms-visibility internal GetTablesV2 sibling during version-collision resolution.");
        }

        [TestMethod]
        public async Task GetItemsAsync_TargetsDefaultDatasetRoute()
        {
            const string firstUrl = "https://test.azure.com/connection/datasets/default/tables/tickets/items";
            const string nextUrl = "https://test.azure.com/connection/datasets/default/tables/tickets/items?$skiptoken=second";
            var responses = new Queue<string>(new[]
            {
                $$"""{"value":[{"ticketId":"first"}],"@odata.nextLink":"{{nextUrl}}"}""",
                """{"value":[{"ticketId":"second"}]}""",
            });
            var setup = ConnectorTestHelpers.CreateCapturingClientSetup(
                () => new HttpResponseMessage(HttpStatusCode.OK)
                {
                    Content = new StringContent(responses.Dequeue()),
                });
            using var client = new ZendeskClient(
                connectionRuntimeUrl: new Uri("https://test.azure.com/connection"),
                credential: setup.Credential,
                options: setup.Options);
            var pageable = client.GetItemsAsync(tableName: "tickets", cancellationToken: CancellationToken.None);
            Assert.IsNull(setup.GetLastRequest(), "Page requests should be deferred until enumeration.");

            var requestedUrls = new List<string>();
            var identifiers = new List<string>();
            var continuations = new List<string?>();
            await foreach (var page in pageable.AsPages().ConfigureAwait(continueOnCapturedContext: false))
            {
                var request = setup.GetLastRequest();
                Assert.IsNotNull(request);
                Assert.AreEqual(expected: HttpMethod.Get, actual: request.Method);
                requestedUrls.Add(request.RequestUri!.AbsoluteUri);
                Assert.HasCount(1, page.Values);
                identifiers.Add(page.Values[0].AdditionalProperties["ticketId"].GetString()!);
                continuations.Add(page.ContinuationToken);
            }

            CollectionAssert.AreEqual(
                expected: new[] { firstUrl, nextUrl },
                actual: requestedUrls,
                message: "GetItems is a public important operation on the default dataset, previously lost to its internal GetItemsV2 sibling.");
            CollectionAssert.AreEqual(expected: new[] { "first", "second" }, actual: identifiers);
            CollectionAssert.AreEqual(expected: new string?[] { nextUrl, null }, actual: continuations);
            Assert.AreEqual(expected: 0, actual: responses.Count);
        }

        [TestMethod]
        public async Task GetItemAsync_TargetsDefaultDatasetRoute()
        {
            var request = await CaptureRequestAsync(
                    responseJson: "{}",
                    invoke: client => client.GetItemAsync(tableName: "tickets", itemKey: "42", cancellationToken: CancellationToken.None))
                .ConfigureAwait(continueOnCapturedContext: false);

            Assert.AreEqual(
                expected: "/connection/datasets/default/tables/tickets/items/42",
                actual: request.RequestUri!.AbsolutePath,
                message: "GetItem is a public important operation on the default dataset, previously lost to its internal GetItemV2 sibling.");
        }

        [TestMethod]
        public async Task GetTableAsync_TargetsDefaultDatasetMetadataRoute()
        {
            var request = await CaptureRequestAsync(
                    responseJson: "{}",
                    invoke: client => client.GetTableAsync(tableName: "tickets", cancellationToken: CancellationToken.None))
                .ConfigureAwait(continueOnCapturedContext: false);

            Assert.AreEqual(
                expected: "/connection/$metadata.json/datasets/default/tables/tickets",
                actual: request.RequestUri!.AbsolutePath,
                message: "The retained discovery route must address the default dataset, not its internal GetTableV2 sibling.");
        }

        [TestMethod]
        public void TriggerOperations_IncludeThePublicOnNewItemsTriggerWithItsOwnPayloadType()
        {
            // NOTE(daviburg): The public important GetOnNewItems trigger was previously displaced by its
            // x-ms-visibility internal GetOnNewItemsV2 sibling, which the discovery filter then dropped,
            // leaving the connector with a single registered trigger. Look the registry up by literal
            // operation id so a coordinated rename of the constant and the registry key cannot pass.
            Assert.IsTrue(
                Zendesk.Models.ZendeskTriggers.Operations.ContainsKey("GetOnNewItems"),
                message: "The public GetOnNewItems trigger must be registered with a typed callback payload.");
            Assert.AreEqual(
                expected: typeof(Zendesk.Models.ZendeskOnNewItemsTriggerPayload),
                actual: Zendesk.Models.ZendeskTriggers.Operations["GetOnNewItems"],
                message: "The public trigger must map to its own payload type.");
            Assert.AreEqual(
                expected: typeof(Zendesk.Models.ZendeskOnUpdatedItemsTriggerPayload),
                actual: Zendesk.Models.ZendeskTriggers.Operations["GetOnUpdatedItemsV2"],
                message: "The versioned trigger keeps its own distinct payload type.");
        }

        private static async Task<HttpRequestMessage> CaptureRequestAsync(
            string responseJson,
            Func<ZendeskClient, Task> invoke)
        {
            var clientSetup = ConnectorTestHelpers.CreateCapturingClientSetup(
                () => new HttpResponseMessage
                {
                    StatusCode = HttpStatusCode.OK,
                    Content = new StringContent(responseJson)
                });

            using var client = new ZendeskClient(
                connectionRuntimeUrl: new Uri("https://test.azure.com/connection"),
                credential: clientSetup.Credential,
                options: clientSetup.Options);

            await invoke(client).ConfigureAwait(continueOnCapturedContext: false);

            var request = clientSetup.GetLastRequest();
            Assert.IsNotNull(request);
            return request!;
        }
    }
}
