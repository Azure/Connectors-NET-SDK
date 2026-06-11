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
        public async Task GetOrganizationsAsync_WithMockedResponse_ReturnsExpectedResult()
        {
            // Arrange
            var expectedResponse = new OrganizationsDynamicValuesList
            {
                Value =
                [
                    new OrganizationsDynamicValuesListItem
                    {
                        Id = "org-1",
                        FriendlyName = "Contoso",
                        Url = "https://contoso.crm.dynamics.com"
                    },
                    new OrganizationsDynamicValuesListItem
                    {
                        Id = "org-2",
                        FriendlyName = "Fabrikam",
                        Url = "https://fabrikam.crm.dynamics.com"
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
                .GetOrganizationsAsync(cancellationToken: CancellationToken.None)
                .ConfigureAwait(continueOnCapturedContext: false);

            // Assert
            Assert.IsNotNull(result);
            Assert.IsNotNull(result.Value);
            Assert.AreEqual(2, result.Value.Count);
            Assert.AreEqual("Contoso", result.Value[0].FriendlyName);
            Assert.AreEqual("https://fabrikam.crm.dynamics.com", result.Value[1].Url);
        }

        [TestMethod]
        public async Task GetItemCodelessAsync_WithErrorResponse_ThrowsConnectorException()
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
                        .GetItemCodelessAsync(
                            tableName: "accounts",
                            rowId: "00000000-0000-0000-0000-000000000000",
                            cancellationToken: CancellationToken.None)
                        .ConfigureAwait(continueOnCapturedContext: false))
                .ConfigureAwait(continueOnCapturedContext: false);

            Assert.AreEqual(400, exception.Status);
            Assert.IsTrue(exception.ResponseBody.Contains("Invalid request", StringComparison.Ordinal));
        }

        [TestMethod]
        public void OrganizationsDynamicValuesListItem_JsonSerialization_RoundTrips()
        {
            // Arrange
            var organization = new OrganizationsDynamicValuesListItem
            {
                Id = "org-abc",
                FriendlyName = "Contoso",
                Url = "https://contoso.crm.dynamics.com"
            };

            // Act
            var json = JsonSerializer.Serialize(organization);
            var deserialized = JsonSerializer.Deserialize<OrganizationsDynamicValuesListItem>(json);

            // Assert
            Assert.IsNotNull(deserialized);
            Assert.AreEqual(organization.Id, deserialized.Id);
            Assert.AreEqual(organization.FriendlyName, deserialized.FriendlyName);
            Assert.AreEqual(organization.Url, deserialized.Url);
        }

        [TestMethod]
        public void SearchRequestBody_JsonSerialization_RoundTrips()
        {
            // Arrange
            var request = new SearchRequestBody
            {
                SearchTerm = "Contoso",
                SearchType = "simple",
                RowCount = 25,
                ReturnRowCount = true
            };

            // Act
            var json = JsonSerializer.Serialize(request);
            var deserialized = JsonSerializer.Deserialize<SearchRequestBody>(json);

            // Assert
            Assert.IsNotNull(deserialized);
            Assert.AreEqual(request.SearchTerm, deserialized.SearchTerm);
            Assert.AreEqual(request.SearchType, deserialized.SearchType);
            Assert.AreEqual(request.RowCount, deserialized.RowCount);
            Assert.AreEqual(request.ReturnRowCount, deserialized.ReturnRowCount);
        }
    }
}
