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
using Azure.Connectors.Sdk.Office365users;
using Azure.Connectors.Sdk.Office365users.Models;
using global::Azure.Core;
using global::Azure.Core.Pipeline;
using Microsoft.VisualStudio.TestTools.UnitTesting;
using Moq;
using Moq.Protected;

namespace Azure.Connectors.Sdk.Tests
{
    /// <summary>
    /// Tests for the generated Office365usersClient class.
    /// </summary>
    [TestClass]
    public class Office365usersClientTests
    {
        [TestMethod]
        public void Constructor_WithValidConnectionRuntimeUrl_ShouldCreateInstance()
        {
            // Arrange & Act
            using var client = new Office365usersClient("https://test.azure.com/connection");

            // Assert
            Assert.IsNotNull(client);
        }

        [TestMethod]
        public void Constructor_WithNullConnectionRuntimeUrl_ShouldThrowArgumentNullException()
        {
            // Arrange & Act & Assert
            Assert.ThrowsExactly<ArgumentNullException>(() => new Office365usersClient((string)null!));
        }

        [TestMethod]
        public void Dispose_ShouldNotThrow()
        {
            // Arrange
            var client = new Office365usersClient("https://test.azure.com/connection");

            // Act & Assert - should not throw
            client.Dispose();
        }

        [TestMethod]
        public void Dispose_CalledTwice_ShouldNotThrow()
        {
            // Arrange
            var mockCredential = new Mock<TokenCredential>();
            var client = new Office365usersClient(
                connectionRuntimeUrl: new Uri("https://test.azure.com/connection"),
                credential: mockCredential.Object);

            // Act & Assert - calling Dispose twice should not throw (idempotent)
            client.Dispose();
            client.Dispose();
        }

        [TestMethod]
        public void Dispose_WithInternallyCreatedHttpClient_ShouldDisposeIt()
        {
            // Arrange - no httpClient provided, so client creates its own
            var mockCredential = new Mock<TokenCredential>();
            var client = new Office365usersClient(
                connectionRuntimeUrl: new Uri("https://test.azure.com/connection"),
                credential: mockCredential.Object);

            // Act
            client.Dispose();

            // Assert - calling Dispose again should not throw (idempotent)
            client.Dispose();
        }

        [TestMethod]
        public async Task MyProfileAsync_WithMockedResponse_ReturnsExpectedResult()
        {
            // Arrange
            var mockHandler = new Mock<HttpMessageHandler>();
            var expectedResponse = new GraphUser
            {
                DisplayName = "Test User",
                GivenName = "Test",
                Surname = "User",
                UserPrincipalName = "testuser@contoso.com",
                Department = "Engineering",
                City = "Redmond"
            };

            mockHandler.Protected()
                .Setup<Task<HttpResponseMessage>>(
                    "SendAsync",
                    ItExpr.IsAny<HttpRequestMessage>(),
                    ItExpr.IsAny<CancellationToken>())
                .ReturnsAsync(new HttpResponseMessage
                {
                    StatusCode = HttpStatusCode.OK,
                    Content = new StringContent(JsonSerializer.Serialize(expectedResponse))
                });

            var mockCredential = new Mock<TokenCredential>();
            mockCredential
                .Setup(credential => credential.GetTokenAsync(It.IsAny<TokenRequestContext>(), It.IsAny<CancellationToken>()))
                .ReturnsAsync(new AccessToken("mock-token", DateTimeOffset.UtcNow.AddHours(1)));

            var options = new ConnectorClientOptions();


            options.Transport = new HttpClientTransport(new HttpClient(mockHandler.Object));


            options.Retry.MaxRetries = 0;

            using var client = new Office365usersClient(
                connectionRuntimeUrl: new Uri("https://test.azure.com/connection"),
                credential: mockCredential.Object,
                options: options);

            // Act
            var result = await client
                .MyProfileAsync(cancellationToken: CancellationToken.None)
                .ConfigureAwait(continueOnCapturedContext: false);

            // Assert
            Assert.IsNotNull(result);
            Assert.AreEqual("Test User", result.DisplayName);
            Assert.AreEqual("testuser@contoso.com", result.UserPrincipalName);
            Assert.AreEqual("Engineering", result.Department);
        }

        [TestMethod]
        public async Task UserProfileAsync_WithMockedResponse_ReturnsExpectedResult()
        {
            // Arrange
            var mockHandler = new Mock<HttpMessageHandler>();
            var expectedResponse = new GraphUser
            {
                DisplayName = "Another User",
                UserPrincipalName = "anotheruser@contoso.com",
                JobTitle = "Software Engineer"
            };

            mockHandler.Protected()
                .Setup<Task<HttpResponseMessage>>(
                    "SendAsync",
                    ItExpr.IsAny<HttpRequestMessage>(),
                    ItExpr.IsAny<CancellationToken>())
                .ReturnsAsync(new HttpResponseMessage
                {
                    StatusCode = HttpStatusCode.OK,
                    Content = new StringContent(JsonSerializer.Serialize(expectedResponse))
                });

            var mockCredential = new Mock<TokenCredential>();
            mockCredential
                .Setup(credential => credential.GetTokenAsync(It.IsAny<TokenRequestContext>(), It.IsAny<CancellationToken>()))
                .ReturnsAsync(new AccessToken("mock-token", DateTimeOffset.UtcNow.AddHours(1)));

            var options = new ConnectorClientOptions();


            options.Transport = new HttpClientTransport(new HttpClient(mockHandler.Object));


            options.Retry.MaxRetries = 0;

            using var client = new Office365usersClient(
                connectionRuntimeUrl: new Uri("https://test.azure.com/connection"),
                credential: mockCredential.Object,
                options: options);

            // Act
            var result = await client
                .UserProfileAsync(userUPN: "anotheruser@contoso.com", cancellationToken: CancellationToken.None)
                .ConfigureAwait(continueOnCapturedContext: false);

            // Assert
            Assert.IsNotNull(result);
            Assert.AreEqual("Another User", result.DisplayName);
            Assert.AreEqual("Software Engineer", result.JobTitle);
        }

        [TestMethod]
        public async Task ManagerAsync_WithMockedResponse_ReturnsExpectedResult()
        {
            // Arrange
            var mockHandler = new Mock<HttpMessageHandler>();
            var expectedResponse = new GraphUser
            {
                DisplayName = "Manager User",
                UserPrincipalName = "manager@contoso.com",
                JobTitle = "Engineering Manager"
            };

            mockHandler.Protected()
                .Setup<Task<HttpResponseMessage>>(
                    "SendAsync",
                    ItExpr.IsAny<HttpRequestMessage>(),
                    ItExpr.IsAny<CancellationToken>())
                .ReturnsAsync(new HttpResponseMessage
                {
                    StatusCode = HttpStatusCode.OK,
                    Content = new StringContent(JsonSerializer.Serialize(expectedResponse))
                });

            var mockCredential = new Mock<TokenCredential>();
            mockCredential
                .Setup(credential => credential.GetTokenAsync(It.IsAny<TokenRequestContext>(), It.IsAny<CancellationToken>()))
                .ReturnsAsync(new AccessToken("mock-token", DateTimeOffset.UtcNow.AddHours(1)));

            var options = new ConnectorClientOptions();


            options.Transport = new HttpClientTransport(new HttpClient(mockHandler.Object));


            options.Retry.MaxRetries = 0;

            using var client = new Office365usersClient(
                connectionRuntimeUrl: new Uri("https://test.azure.com/connection"),
                credential: mockCredential.Object,
                options: options);

            // Act
            var result = await client
                .ManagerAsync(userUPN: "testuser@contoso.com", cancellationToken: CancellationToken.None)
                .ConfigureAwait(continueOnCapturedContext: false);

            // Assert
            Assert.IsNotNull(result);
            Assert.AreEqual("Manager User", result.DisplayName);
            Assert.AreEqual("Engineering Manager", result.JobTitle);
        }

        [TestMethod]
        public async Task DirectReportsAsync_WithMockedResponse_ReturnsExpectedResult()
        {
            // Arrange
            var mockHandler = new Mock<HttpMessageHandler>();
            var expectedResponse = new DirectReportsResponse
            {
                Value = new List<GraphUser>
                {
                    new GraphUser
                    {
                        DisplayName = "Report 1",
                        UserPrincipalName = "report1@contoso.com"
                    },
                    new GraphUser
                    {
                        DisplayName = "Report 2",
                        UserPrincipalName = "report2@contoso.com"
                    }
                }
            };

            mockHandler.Protected()
                .Setup<Task<HttpResponseMessage>>(
                    "SendAsync",
                    ItExpr.IsAny<HttpRequestMessage>(),
                    ItExpr.IsAny<CancellationToken>())
                .ReturnsAsync(new HttpResponseMessage
                {
                    StatusCode = HttpStatusCode.OK,
                    Content = new StringContent(JsonSerializer.Serialize(expectedResponse))
                });

            var mockCredential = new Mock<TokenCredential>();
            mockCredential
                .Setup(credential => credential.GetTokenAsync(It.IsAny<TokenRequestContext>(), It.IsAny<CancellationToken>()))
                .ReturnsAsync(new AccessToken("mock-token", DateTimeOffset.UtcNow.AddHours(1)));

            var options = new ConnectorClientOptions();


            options.Transport = new HttpClientTransport(new HttpClient(mockHandler.Object));


            options.Retry.MaxRetries = 0;

            using var client = new Office365usersClient(
                connectionRuntimeUrl: new Uri("https://test.azure.com/connection"),
                credential: mockCredential.Object,
                options: options);

            // Act
            var result = await client
                .DirectReportsAsync(userUPN: "manager@contoso.com", cancellationToken: CancellationToken.None)
                .ConfigureAwait(continueOnCapturedContext: false);

            // Assert
            Assert.IsNotNull(result);
            Assert.AreEqual(2, result.Value.Count);
            Assert.AreEqual("Report 1", result.Value[0].DisplayName);
        }

        [TestMethod]
        public async Task MyProfileAsync_WithErrorResponse_ThrowsConnectorException()
        {
            // Arrange
            var mockHandler = new Mock<HttpMessageHandler>();

            mockHandler.Protected()
                .Setup<Task<HttpResponseMessage>>(
                    "SendAsync",
                    ItExpr.IsAny<HttpRequestMessage>(),
                    ItExpr.IsAny<CancellationToken>())
                .ReturnsAsync(new HttpResponseMessage
                {
                    StatusCode = HttpStatusCode.BadRequest,
                    Content = new StringContent("{\"error\": \"Invalid request\"}")
                });

            var mockCredential = new Mock<TokenCredential>();
            mockCredential
                .Setup(credential => credential.GetTokenAsync(It.IsAny<TokenRequestContext>(), It.IsAny<CancellationToken>()))
                .ReturnsAsync(new AccessToken("mock-token", DateTimeOffset.UtcNow.AddHours(1)));

            var options = new ConnectorClientOptions();


            options.Transport = new HttpClientTransport(new HttpClient(mockHandler.Object));


            options.Retry.MaxRetries = 0;

            using var client = new Office365usersClient(
                connectionRuntimeUrl: new Uri("https://test.azure.com/connection"),
                credential: mockCredential.Object,
                options: options);

            // Act & Assert
            var exception = await Assert
                .ThrowsExactlyAsync<ConnectorException>(async () =>
                    await client
                        .MyProfileAsync(cancellationToken: CancellationToken.None)
                        .ConfigureAwait(continueOnCapturedContext: false))
                .ConfigureAwait(continueOnCapturedContext: false);

            Assert.AreEqual(400, exception.Status);
            Assert.IsTrue(exception.ResponseBody.Contains("Invalid request"));
        }

        [TestMethod]
        public void GraphUser_JsonSerialization_RoundTrips()
        {
            // Arrange
            var user = new GraphUser
            {
                DisplayName = "Test User",
                GivenName = "Test",
                Surname = "User",
                UserPrincipalName = "test@contoso.com",
                Department = "Engineering",
                City = "Redmond",
                Country = "US",
                JobTitle = "Software Engineer",
                AccountEnabled = true
            };

            // Act
            var json = JsonSerializer.Serialize(user);
            var deserialized = JsonSerializer.Deserialize<GraphUser>(json);

            // Assert
            Assert.IsNotNull(deserialized);
            Assert.AreEqual(user.DisplayName, deserialized.DisplayName);
            Assert.AreEqual(user.GivenName, deserialized.GivenName);
            Assert.AreEqual(user.Surname, deserialized.Surname);
            Assert.AreEqual(user.UserPrincipalName, deserialized.UserPrincipalName);
            Assert.AreEqual(user.Department, deserialized.Department);
            Assert.AreEqual(user.City, deserialized.City);
            Assert.AreEqual(user.Country, deserialized.Country);
            Assert.AreEqual(user.JobTitle, deserialized.JobTitle);
            Assert.AreEqual(user.AccountEnabled, deserialized.AccountEnabled);
        }

        [TestMethod]
        public void User_JsonSerialization_RoundTrips()
        {
            // Arrange
            var user = new User
            {
                UserId = "user-123",
                DisplayName = "Searched User",
                Email = "searched@contoso.com",
                Department = "Sales",
                UserPrincipalNameUPN = "searched@contoso.com"
            };

            // Act
            var json = JsonSerializer.Serialize(user);
            var deserialized = JsonSerializer.Deserialize<User>(json);

            // Assert
            Assert.IsNotNull(deserialized);
            Assert.AreEqual(user.UserId, deserialized.UserId);
            Assert.AreEqual(user.DisplayName, deserialized.DisplayName);
            Assert.AreEqual(user.Email, deserialized.Email);
            Assert.AreEqual(user.Department, deserialized.Department);
            Assert.AreEqual(user.UserPrincipalNameUPN, deserialized.UserPrincipalNameUPN);
        }

        [TestMethod]
        public void Person_JsonSerialization_RoundTrips()
        {
            // Arrange
            var person = new Person
            {
                PersonId = "person-456",
                DisplayName = "Relevant Person",
                GivenName = "Relevant",
                Surname = "Person",
                Department = "Marketing"
            };

            // Act
            var json = JsonSerializer.Serialize(person);
            var deserialized = JsonSerializer.Deserialize<Person>(json);

            // Assert
            Assert.IsNotNull(deserialized);
            Assert.AreEqual(person.PersonId, deserialized.PersonId);
            Assert.AreEqual(person.DisplayName, deserialized.DisplayName);
            Assert.AreEqual(person.GivenName, deserialized.GivenName);
            Assert.AreEqual(person.Surname, deserialized.Surname);
            Assert.AreEqual(person.Department, deserialized.Department);
        }

        [TestMethod]
        public void EntityListResponseIReadOnlyListUser_ShouldImplementIPageable()
        {
            // Arrange & Act
            var response = new EntityListResponseIReadOnlyListUser
            {
                Value = new List<User>
                {
                    new User { DisplayName = "User 1" },
                    new User { DisplayName = "User 2" }
                },
                NextLink = "https://test.azure.com/nextpage"
            };

            // Assert
            Assert.IsInstanceOfType<IPageable<User>>(response);
            Assert.AreEqual(2, response.Value.Count);
            Assert.AreEqual("https://test.azure.com/nextpage", response.NextLink);
        }
    }
}
