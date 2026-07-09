//------------------------------------------------------------
// Copyright (c) Microsoft Corporation.  All rights reserved.
//------------------------------------------------------------

using System;
using System.Net;
using System.Net.Http;
using System.Text.Json;
using System.Threading;
using System.Threading.Tasks;
using Azure.Connectors.Sdk.Commondataservice;
using Azure.Connectors.Sdk.Commondataservice.Models;
using global::Azure.Core;
using Microsoft.VisualStudio.TestTools.UnitTesting;
using Moq;

namespace Azure.Connectors.Sdk.Tests
{
    /// <summary>
    /// Tests for the generated <see cref="CommondataserviceClient"/> class (Microsoft Dataverse).
    /// </summary>
    [TestClass]
    public class CommondataserviceClientTests
    {
        private static CommondataserviceClient CreateMockedClient(Func<HttpResponseMessage> responseFactory)
        {
            var (credential, options) = ConnectorTestHelpers.CreateMockedClientSetup(responseFactory);
            return new CommondataserviceClient(
                connectionRuntimeUrl: new Uri("https://test.azure.com/connection"),
                credential: credential,
                options: options);
        }

        [TestMethod]
        public void Constructor_WithValidConnectionRuntimeUrl_ShouldCreateInstance()
        {
            // Arrange & Act
            using var client = new CommondataserviceClient("https://test.azure.com/connection");

            // Assert
            Assert.IsNotNull(client);
        }

        [TestMethod]
        public void Constructor_WithNullConnectionRuntimeUrl_ShouldThrowArgumentNullException()
        {
            // Arrange & Act & Assert
            Assert.ThrowsExactly<ArgumentNullException>(() => new CommondataserviceClient((string)null!));
        }

        [TestMethod]
        public void ConnectorName_ReturnsCommondataservice()
        {
            // Arrange
            using var client = new CommondataserviceClient("https://test.azure.com/connection");

            // Act & Assert
            Assert.AreEqual("commondataservice", client.ConnectorName, ignoreCase: false);
        }

        [TestMethod]
        public void Dispose_ShouldNotThrow()
        {
            // Arrange
            var client = new CommondataserviceClient("https://test.azure.com/connection");

            // Act & Assert - should not throw
            client.Dispose();
        }

        [TestMethod]
        public void Dispose_CalledTwice_ShouldNotThrow()
        {
            // Arrange
            var mockCredential = new Mock<TokenCredential>();
            var client = new CommondataserviceClient(
                connectionRuntimeUrl: new Uri("https://test.azure.com/connection"),
                credential: mockCredential.Object);

            // Act & Assert - calling Dispose twice should not throw (idempotent)
            client.Dispose();
            client.Dispose();
        }

        [TestMethod]
        public async Task GetDataSetsAsync_WithMockedResponse_ReturnsExpectedResult()
        {
            // Arrange
            var expectedResponse = new DataSetsList
            {
                Value =
                [
                    new DataSet
                    {
                        Name = "https://contoso.crm.dynamics.com",
                        DisplayName = "Contoso (contoso)"
                    },
                    new DataSet
                    {
                        Name = "https://fabrikam.crm.dynamics.com",
                        DisplayName = "Fabrikam (fabrikam)"
                    }
                ]
            };

            using var client = CreateMockedClient(() => new HttpResponseMessage
            {
                StatusCode = HttpStatusCode.OK,
                Content = new StringContent(JsonSerializer.Serialize(expectedResponse))
            });

            // Act
            var result = await client
                .GetDataSetsAsync(cancellationToken: CancellationToken.None)
                .ConfigureAwait(continueOnCapturedContext: false);

            // Assert
            Assert.IsNotNull(result);
            Assert.IsNotNull(result.Value);
            Assert.AreEqual(expected: 2, actual: result.Value.Count);
            Assert.AreEqual(expected: "Contoso (contoso)", actual: result.Value[0].DisplayName);
            Assert.AreEqual(expected: "https://fabrikam.crm.dynamics.com", actual: result.Value[1].Name);
        }

        [TestMethod]
        public async Task GetItemAsync_WithErrorResponse_ThrowsConnectorException()
        {
            // Arrange
            using var client = CreateMockedClient(() => new HttpResponseMessage
            {
                StatusCode = HttpStatusCode.BadRequest,
                Content = new StringContent("{\"error\": \"Invalid request\"}")
            });

            // Act & Assert
            var exception = await Assert
                .ThrowsExactlyAsync<ConnectorException>(async () =>
                    await client
                        .GetItemAsync(
                            environment: "https://contoso.crm.dynamics.com",
                            tableName: "accounts",
                            itemIdentifier: "00000000-0000-0000-0000-000000000000",
                            cancellationToken: CancellationToken.None)
                        .ConfigureAwait(continueOnCapturedContext: false))
                .ConfigureAwait(continueOnCapturedContext: false);

            Assert.AreEqual(expected: 400, actual: exception.Status);
            Assert.IsTrue(exception.ResponseBody.Contains("Invalid request", StringComparison.Ordinal));
        }

        [TestMethod]
        public async Task GetTablesAsync_DoubleEncodesDatasetUrlInRequestPath()
        {
            // The commondataservice dataset value is a full environment URL. The connector's
            // x-ms-url-encoding: "double" contract requires the path segment to be double-encoded
            // so it survives one round of server-side URL decoding. This asserts the GENERATED client
            // performs that double-encoding — guarding against a regression to single encoding or a
            // hand-edited generated file.
            var clientSetup = ConnectorTestHelpers.CreateCapturingClientSetup(
                () => new HttpResponseMessage
                {
                    StatusCode = HttpStatusCode.OK,
                    Content = new StringContent("{\"value\":[]}")
                });

            using var client = new CommondataserviceClient(
                connectionRuntimeUrl: new Uri("https://test.azure.com/connection"),
                credential: clientSetup.Credential,
                options: clientSetup.Options);

            await client
                .GetTablesAsync(dataset: "https://contoso.crm.dynamics.com", cancellationToken: CancellationToken.None)
                .ConfigureAwait(continueOnCapturedContext: false);

            var request = clientSetup.GetLastRequest();
            Assert.IsNotNull(request, message: "Expected the client to issue an HTTP request.");
            var requestUri = request!.RequestUri!.AbsoluteUri;

            // "https://contoso.crm.dynamics.com" single-encoded is "https%3A%2F%2Fcontoso...";
            // double-encoded is "https%253A%252F%252Fcontoso...".
            Assert.IsTrue(
                requestUri.Contains("https%253A%252F%252Fcontoso.crm.dynamics.com", StringComparison.Ordinal),
                message: $"Dataset path segment must be double URL-encoded. Actual request URI: {requestUri}");
        }

        [TestMethod]
        public async Task CreateAttachmentAsync_WithStringItemId_DoubleEncodesItemIdInRequestPath()
        {
            var clientSetup = ConnectorTestHelpers.CreateCapturingClientSetup(
                () => new HttpResponseMessage
                {
                    StatusCode = HttpStatusCode.OK,
                    Content = new StringContent("{}")
                });

            using var client = new CommondataserviceClient(
                connectionRuntimeUrl: new Uri("https://test.azure.com/connection"),
                credential: clientSetup.Credential,
                options: clientSetup.Options);

            await client
                .CreateAttachmentAsync(
                    dataset: "https://contoso.crm.dynamics.com",
                    table: "accounts",
                    id: "account/id",
                    input: [],
                    fileName: "note.txt",
                    cancellationToken: CancellationToken.None)
                .ConfigureAwait(continueOnCapturedContext: false);

            var request = clientSetup.GetLastRequest();
            Assert.IsNotNull(request, message: "Expected the client to issue an HTTP request.");
            var requestUri = request!.RequestUri!.AbsoluteUri;

            Assert.IsTrue(
                requestUri.Contains("/items/account%252Fid/attachments", StringComparison.Ordinal),
                message: $"Attachment item id path segment must be double URL-encoded. Actual request URI: {requestUri}");
        }

        [TestMethod]
        public async Task GetNextPageAsync_WithReservedAndPreEncodedNextLink_EncodesNextLinkInRequestPath()
        {
            var clientSetup = ConnectorTestHelpers.CreateCapturingClientSetup(
                () => new HttpResponseMessage
                {
                    StatusCode = HttpStatusCode.OK,
                    Content = new StringContent("{\"value\":[]}")
                });

            using var client = new CommondataserviceClient(
                connectionRuntimeUrl: new Uri("https://test.azure.com/connection"),
                credential: clientSetup.Credential,
                options: clientSetup.Options);

            await client
                .GetNextPageAsync(
                    nextLinkValue: "accounts?$select=name%2Crevenue&$skiptoken=%253cpage%253e%2Frow 1",
                    cancellationToken: CancellationToken.None)
                .ConfigureAwait(continueOnCapturedContext: false);

            var request = clientSetup.GetLastRequest();
            Assert.IsNotNull(request, message: "Expected the client to issue an HTTP request.");
            var requestUri = request!.RequestUri!.AbsoluteUri;

            Assert.IsTrue(
                requestUri.Contains("/nextLink/accounts%3F%24select%3Dname%252Crevenue%26%24skiptoken%3D%25253cpage%25253e%252Frow%201", StringComparison.Ordinal),
                message: $"nextLink path segment must URL-encode reserved characters and pre-encoded query state. Actual request URI: {requestUri}");
        }

        [TestMethod]
        public void DataSet_JsonSerialization_RoundTrips()
        {
            // Arrange
            var dataSet = new DataSet
            {
                Name = "https://contoso.crm.dynamics.com",
                DisplayName = "Contoso (contoso)"
            };

            // Act
            var json = JsonSerializer.Serialize(dataSet);
            var deserialized = JsonSerializer.Deserialize<DataSet>(json);

            // Assert
            Assert.IsNotNull(deserialized);
            Assert.AreEqual(expected: dataSet.Name, actual: deserialized.Name);
            Assert.AreEqual(expected: dataSet.DisplayName, actual: deserialized.DisplayName);
        }

        [TestMethod]
        public void PostItemInput_JsonSerialization_RoundTrips()
        {
            // Arrange
            var input = new PostItemInput
            {
                AdditionalProperties =
                {
                    ["name"] = JsonSerializer.SerializeToElement("Demo Account"),
                    ["revenue"] = JsonSerializer.SerializeToElement(123.45)
                }
            };

            // Act
            var json = JsonSerializer.Serialize(input);
            var deserialized = JsonSerializer.Deserialize<PostItemInput>(json);

            // Assert
            Assert.IsNotNull(deserialized);
            Assert.IsTrue(deserialized.AdditionalProperties.ContainsKey("name"));
            Assert.IsTrue(deserialized.AdditionalProperties.ContainsKey("revenue"));
            Assert.AreEqual(expected: "Demo Account", actual: deserialized.AdditionalProperties["name"].GetString());
            Assert.AreEqual(expected: 123.45, actual: deserialized.AdditionalProperties["revenue"].GetDouble());
        }
    }
}
