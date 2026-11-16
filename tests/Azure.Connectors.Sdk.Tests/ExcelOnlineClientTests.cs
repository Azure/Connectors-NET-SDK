//------------------------------------------------------------
// Copyright (c) Microsoft Corporation.  All rights reserved.
//------------------------------------------------------------

using System;
using System.Collections.Generic;
using System.Net;
using System.Net.Http;
using System.Text.Json;
using System.Threading;
using System.Threading.Tasks;
using Azure.Connectors.Sdk.ExcelOnline;
using Azure.Connectors.Sdk.ExcelOnline.Models;
using global::Azure.Core;
using global::Azure.Core.Pipeline;
using Microsoft.VisualStudio.TestTools.UnitTesting;
using Moq;
using Moq.Protected;

namespace Azure.Connectors.Sdk.Tests
{
    /// <summary>
    /// Tests for the generated ExcelOnlineClient class.
    /// </summary>
    [TestClass]
    public class ExcelOnlineClientTests
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

        private static ExcelOnlineClient CreateMockedClient(HttpResponseMessage response)
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

            return new ExcelOnlineClient(
                connectionRuntimeUrl: new Uri("https://test.azure.com/connection"),
                credential: SharedMockCredential.Object,
                options: options);
        }

        [TestMethod]
        public void Constructor_WithValidConnectionRuntimeUrl_ShouldCreateInstance()
        {
            using var client = new ExcelOnlineClient("https://test.azure.com/connection");
            Assert.IsNotNull(client);
        }

        [TestMethod]
        public void Constructor_WithNullConnectionRuntimeUrl_ShouldThrowArgumentNullException()
        {
            Assert.ThrowsExactly<ArgumentNullException>(() => new ExcelOnlineClient((string)null!));
        }

        [TestMethod]
        public void Dispose_ShouldNotThrow()
        {
            var client = new ExcelOnlineClient("https://test.azure.com/connection");
            client.Dispose();
        }

        [TestMethod]
        public void Dispose_CalledTwice_ShouldNotThrow()
        {
            var client = new ExcelOnlineClient(
                connectionRuntimeUrl: new Uri("https://test.azure.com/connection"),
                credential: SharedMockCredential.Object);
            client.Dispose();
            client.Dispose();
        }

        [TestMethod]
        public void TableMetadata_Serialization_RoundTrips()
        {
            var table = new TableMetadata
            {
                Name = "Table1",
                Title = "My Test Table",
            };

            var json = JsonSerializer.Serialize(table);
            var deserialized = JsonSerializer.Deserialize<TableMetadata>(json);

            Assert.IsNotNull(deserialized);
            Assert.AreEqual("Table1", deserialized!.Name);
            Assert.AreEqual("My Test Table", deserialized.Title);
        }

        [TestMethod]
        public void WorksheetMetadata_Serialization_RoundTrips()
        {
            var worksheet = new WorksheetMetadata
            {
                Id = "ws-1",
                Name = "Sheet1",
            };

            var json = JsonSerializer.Serialize(worksheet);
            var deserialized = JsonSerializer.Deserialize<WorksheetMetadata>(json);

            Assert.IsNotNull(deserialized);
            Assert.AreEqual("ws-1", deserialized!.Id);
            Assert.AreEqual("Sheet1", deserialized.Name);
        }

        [TestMethod]
        public async Task GetItemsAsync_WithMockedResponse_ReturnsExpectedResult()
        {
            const string firstUrl = "https://test.azure.com/connection/drives/testlib/files/testfile.xlsx/tables/Table1/items?source=me";
            const string nextUrl = "https://test.azure.com/connection/drives/testlib/files/testfile.xlsx/tables/Table1/items?$skiptoken=second";
            var responses = new Queue<string>(new[]
            {
                $$"""{"value":[{"Name":"first"}],"@odata.nextLink":"{{nextUrl}}"}""",
                """{"value":[{"Name":"second"}]}""",
            });
            var setup = ConnectorTestHelpers.CreateCapturingClientSetup(
                () => new HttpResponseMessage(HttpStatusCode.OK)
                {
                    Content = new StringContent(responses.Dequeue()),
                });
            using var client = new ExcelOnlineClient(
                connectionRuntimeUrl: new Uri("https://test.azure.com/connection"),
                credential: setup.Credential,
                options: setup.Options);

            var pageable = client.GetItemsAsync(
                documentLibrary: "testlib",
                file: "testfile.xlsx",
                table: "Table1",
                cancellationToken: CancellationToken.None);
            Assert.IsNull(setup.GetLastRequest(), "Page requests should be deferred until enumeration.");

            var names = new List<string>();
            var requestedUrls = new List<string>();
            var continuations = new List<string?>();
            await foreach (var page in pageable.AsPages().ConfigureAwait(continueOnCapturedContext: false))
            {
                var request = setup.GetLastRequest();
                Assert.IsNotNull(request);
                Assert.AreEqual(expected: HttpMethod.Get, actual: request.Method);
                requestedUrls.Add(request.RequestUri!.AbsoluteUri);
                Assert.HasCount(1, page.Values);
                names.Add(page.Values[0].AdditionalProperties["Name"].GetString()!);
                continuations.Add(page.ContinuationToken);
            }

            CollectionAssert.AreEqual(expected: new[] { firstUrl, nextUrl }, actual: requestedUrls);
            CollectionAssert.AreEqual(expected: new[] { "first", "second" }, actual: names);
            CollectionAssert.AreEqual(expected: new string?[] { nextUrl, null }, actual: continuations);
            Assert.AreEqual(expected: 0, actual: responses.Count);
        }

        [TestMethod]
        public async Task GetItemsAsync_WithErrorResponse_ThrowsConnectorException()
        {
            using var responseMessage = new HttpResponseMessage
            {
                StatusCode = HttpStatusCode.NotFound,
                Content = new StringContent("{\"error\": \"Not found\"}")
            };

            using var client = CreateMockedClient(responseMessage);

            await Assert.ThrowsExactlyAsync<ConnectorException>(async () =>
            {
                await foreach (var page in client.GetItemsAsync(
                    documentLibrary: "testlib",
                    file: "testfile.xlsx",
                    table: "Table1",
                    cancellationToken: CancellationToken.None)
                    .AsPages()
                    .ConfigureAwait(continueOnCapturedContext: false))
                {
                    Assert.Fail($"Unexpected page with {page.Values.Count} items.");
                }
            }).ConfigureAwait(continueOnCapturedContext: false);
        }
    }
}
