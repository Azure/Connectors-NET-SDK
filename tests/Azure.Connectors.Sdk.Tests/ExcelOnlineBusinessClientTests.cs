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
using Azure.Connectors.Sdk.ExcelOnlineBusiness;
using Azure.Connectors.Sdk.ExcelOnlineBusiness.Models;
using global::Azure.Core;
using global::Azure.Core.Pipeline;
using Microsoft.VisualStudio.TestTools.UnitTesting;
using Moq;
using Moq.Protected;

namespace Azure.Connectors.Sdk.Tests
{
    /// <summary>
    /// Tests for the generated ExcelOnlineBusinessClient class.
    /// </summary>
    [TestClass]
    public class ExcelOnlineBusinessClientTests
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

        private static ExcelOnlineBusinessClient CreateMockedClient(HttpResponseMessage response, Action<HttpRequestMessage>? requestCallback = null)
        {
            var mockHandler = new Mock<HttpMessageHandler>();
            mockHandler.Protected()
                .Setup<Task<HttpResponseMessage>>(
                    "SendAsync",
                    ItExpr.IsAny<HttpRequestMessage>(),
                    ItExpr.IsAny<CancellationToken>())
                .ReturnsAsync(response)
                .Callback<HttpRequestMessage, CancellationToken>((request, _) => requestCallback?.Invoke(request))
                .Verifiable();

            var options = new ConnectorClientOptions();
            options.Transport = new HttpClientTransport(new HttpClient(mockHandler.Object));
            options.Retry.MaxRetries = 0;

            return new ExcelOnlineBusinessClient(
                connectionRuntimeUrl: new Uri("https://test.azure.com/connection"),
                credential: SharedMockCredential.Object,
                options: options);
        }

        [TestMethod]
        public void Constructor_WithValidConnectionRuntimeUrl_ShouldCreateInstance()
        {
            using var client = new ExcelOnlineBusinessClient("https://test.azure.com/connection");
            Assert.IsNotNull(client);
        }

        [TestMethod]
        public void Constructor_WithNullConnectionRuntimeUrl_ShouldThrowArgumentNullException()
        {
            Assert.ThrowsExactly<ArgumentNullException>(() => new ExcelOnlineBusinessClient((string)null!));
        }

        [TestMethod]
        public void Dispose_ShouldNotThrow()
        {
            var client = new ExcelOnlineBusinessClient("https://test.azure.com/connection");
            client.Dispose();
        }

        [TestMethod]
        public void Dispose_CalledTwice_ShouldNotThrow()
        {
            var client = new ExcelOnlineBusinessClient(
                connectionRuntimeUrl: new Uri("https://test.azure.com/connection"),
                credential: SharedMockCredential.Object);
            client.Dispose();
            client.Dispose();
        }

        [TestMethod]
        public async Task CreateTableAsync_WithMockedResponse_ReturnsExpectedResult()
        {
            var expectedResponse = new TableMetadata
            {
                Name = "Sheet1!A1:C10",
                Title = "MyTable"
            };

            using var responseMessage = new HttpResponseMessage
            {
                StatusCode = HttpStatusCode.OK,
                Content = new StringContent(JsonSerializer.Serialize(expectedResponse))
            };

            using var client = CreateMockedClient(responseMessage);

            var result = await client
                .CreateTableAsync(
                    documentLibrary: "OneDrive",
                    file: "workbook.xlsx",
                    input: new TableToCreate(),
                    location: "me",
                    cancellationToken: CancellationToken.None)
                .ConfigureAwait(continueOnCapturedContext: false);

            Assert.IsNotNull(result);
            Assert.AreEqual("Sheet1!A1:C10", result.Name);
            Assert.AreEqual("MyTable", result.Title);
        }

        [TestMethod]
        public async Task CreateTableAsync_WithErrorResponse_ThrowsConnectorException()
        {
            using var responseMessage = new HttpResponseMessage
            {
                StatusCode = HttpStatusCode.BadRequest,
                Content = new StringContent("{\"error\": \"Bad Request\"}")
            };

            using var client = CreateMockedClient(responseMessage);

            await Assert.ThrowsExactlyAsync<ConnectorException>(() =>
                client.CreateTableAsync(
                    documentLibrary: "OneDrive",
                    file: "workbook.xlsx",
                    input: new TableToCreate(),
                    location: "me",
                    cancellationToken: CancellationToken.None))
                .ConfigureAwait(continueOnCapturedContext: false);
        }

        [TestMethod]
        public void TableMetadata_SerializationRoundTrip()
        {
            var original = new TableMetadata
            {
                Name = "Sheet1!A1:D20",
                Title = "SalesTable"
            };

            var json = JsonSerializer.Serialize(original);
            var deserialized = JsonSerializer.Deserialize<TableMetadata>(json);

            Assert.IsNotNull(deserialized);
            Assert.AreEqual("Sheet1!A1:D20", deserialized.Name);
            Assert.AreEqual("SalesTable", deserialized.Title);
        }
        [TestMethod]
        public async Task GetSingleScriptAsync_WithMockedResponse_ReturnsExpected()
        {
            HttpRequestMessage? capturedRequest = null;
            using var responseMessage = new HttpResponseMessage
            {
                StatusCode = HttpStatusCode.OK,
                Content = new StringContent("{}")
            };

            using var client = CreateMockedClient(responseMessage, request => capturedRequest = request);

            var result = await client
                .GetSingleScriptAsync(chosenScriptSource: "me",
                    chosenScriptDrive: "drive1",
                    chosenScript: "script1",
                    cancellationToken: CancellationToken.None)
                .ConfigureAwait(continueOnCapturedContext: false);

            Assert.IsNotNull(result);
            Assert.AreEqual("/connection/v2/officescripting/api/storage/script", capturedRequest!.RequestUri!.AbsolutePath);
        }

        [TestMethod]
        public async Task GetSingleScriptAsync_WithErrorResponse_ThrowsConnectorException()
        {
            using var responseMessage = new HttpResponseMessage
            {
                StatusCode = HttpStatusCode.BadRequest,
                Content = new StringContent("{\"error\": \"Bad request\"}")
            };

            using var client = CreateMockedClient(responseMessage);

            await Assert.ThrowsExactlyAsync<ConnectorException>(() =>
                client.GetSingleScriptAsync(chosenScriptSource: "me",
                    chosenScriptDrive: "drive1",
                    chosenScript: "script1",
                    cancellationToken: CancellationToken.None))
                .ConfigureAwait(continueOnCapturedContext: false);
        }


        [TestMethod]
        public async Task GetItemsAsync_WithSensitivityLabels_PreservesPageModelAndRawResponseAcrossPages()
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
                    if (requestedUris.Count == 1)
                    {
                        var page1 = new HttpResponseMessage(HttpStatusCode.OK)
                        {
                            Content = new StringContent("""
                                {
                                    "value": [{ "RowId": "row-1" }],
                                    "sensitivityLabelInfo": [{
                                        "sensitivityLabelId": "label-confidential",
                                        "name": "Confidential",
                                        "displayName": "Confidential - Internal",
                                        "tooltip": "Internal data",
                                        "priority": 2,
                                        "color": "#FF0000",
                                        "isEncrypted": true,
                                        "isEnabled": true,
                                        "isParent": false,
                                        "parentSensitivityLabelId": "root-label"
                                    }],
                                    "@odata.nextLink": "https://test.azure.com/connection/drives/OneDrive/files/folder%252Fbook.xlsx/tables/Table1/items?$skiptoken=page-2"
                                }
                                """)
                        };
                        page1.Headers.Add("x-ms-request-id", "req-page-1");
                        return Task.FromResult(page1);
                    }

                    var page2 = new HttpResponseMessage(HttpStatusCode.OK)
                    {
                        Content = new StringContent("""
                            {
                                "value": [],
                                "sensitivityLabelInfo": [{
                                    "sensitivityLabelId": "label-public",
                                    "name": "Public",
                                    "displayName": "Public Data",
                                    "isEncrypted": false
                                }]
                            }
                            """)
                    };
                    page2.Headers.Add("x-ms-request-id", "req-page-2");
                    return Task.FromResult(page2);
                });

            var options = new ConnectorClientOptions
            {
                Transport = new HttpClientTransport(new HttpClient(handler.Object))
            };
            options.Retry.MaxRetries = 0;

            using var client = new ExcelOnlineBusinessClient(
                connectionRuntimeUrl: new Uri("https://test.azure.com/connection"),
                credential: SharedMockCredential.Object,
                options: options);

            var pageable = client.GetItemsAsync(
                documentLibrary: "OneDrive",
                file: "folder/book.xlsx",
                table: "Table1",
                location: "me",
                extractSensitivityLabel: true,
                sensitivityLabelMetadata: true);

            Assert.AreEqual(expected: 0, actual: requestedUris.Count);

            var pages = new List<global::Azure.Page<Item>>();
            await foreach (var page in pageable.AsPages().ConfigureAwait(continueOnCapturedContext: false))
            {
                pages.Add(page);
            }

            Assert.AreEqual(expected: 2, actual: pages.Count);
            Assert.AreEqual(
                expected: "https://test.azure.com/connection/drives/OneDrive/files/folder%252Fbook.xlsx/tables/Table1/items?source=me&extractSensitivityLabel=True&fetchSensitivityLabelMetadata=True",
                actual: requestedUris[0]);
            Assert.AreEqual(
                expected: "https://test.azure.com/connection/drives/OneDrive/files/folder%252Fbook.xlsx/tables/Table1/items?$skiptoken=page-2",
                actual: requestedUris[1]);

            var typedPage1 = pages[0] as ConnectorPage<ItemsList, Item>;
            Assert.IsNotNull(typedPage1);
            Assert.AreEqual(expected: 1, actual: typedPage1.Values.Count);
            Assert.AreEqual(expected: "row-1", actual: typedPage1.Values[0].AdditionalProperties["RowId"].GetString());
            Assert.IsNotNull(typedPage1.PageModel.SensitivityLabelInfo);
            Assert.AreEqual(expected: 1, actual: typedPage1.PageModel.SensitivityLabelInfo.Count);
            Assert.AreEqual(expected: "label-confidential", actual: typedPage1.PageModel.SensitivityLabelInfo[0].SensitivityLabelId);
            Assert.AreEqual(expected: "Confidential - Internal", actual: typedPage1.PageModel.SensitivityLabelInfo[0].SensitivityLabelDisplayNameInfo);
            Assert.AreEqual(expected: true, actual: typedPage1.PageModel.SensitivityLabelInfo[0].IsEncryptedStatusOfSensitivityLabel);

            var rawEnvelope1 = pages[0].GetRawResponse().Content.ToObjectFromJson<ItemsList>();
            Assert.IsNotNull(rawEnvelope1);
            Assert.AreEqual(expected: "label-confidential", actual: rawEnvelope1.SensitivityLabelInfo[0].SensitivityLabelId);
            Assert.IsTrue(pages[0].GetRawResponse().Headers.TryGetValue("x-ms-request-id", out var requestId1));
            Assert.AreEqual(expected: "req-page-1", actual: requestId1);

            var typedPage2 = pages[1] as ConnectorPage<ItemsList, Item>;
            Assert.IsNotNull(typedPage2);
            Assert.AreEqual(expected: 0, actual: typedPage2.Values.Count);
            Assert.AreEqual(expected: "label-public", actual: typedPage2.PageModel.SensitivityLabelInfo[0].SensitivityLabelId);
            Assert.IsTrue(pages[1].GetRawResponse().Headers.TryGetValue("x-ms-request-id", out var requestId2));
            Assert.AreEqual(expected: "req-page-2", actual: requestId2);
        }
    }
}
