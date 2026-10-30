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
using Azure.Connectors.Sdk.Shifts;
using global::Azure.Core;
using global::Azure.Core.Pipeline;
using Microsoft.VisualStudio.TestTools.UnitTesting;
using Moq;
using Moq.Protected;

namespace Azure.Connectors.Sdk.Tests
{
    [TestClass]
    public class ShiftsClientTests
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
            using var client = new ShiftsClient("https://test.azure.com/conn");
            Assert.IsNotNull(client);
        }

        [TestMethod]
        public void Constructor_WithNull_ShouldThrow()
        {
            Assert.ThrowsExactly<ArgumentNullException>(() => new ShiftsClient((string)null!));
        }

        [TestMethod]
        public void Dispose_ShouldNotThrow()
        {
            var client = new ShiftsClient("https://test.azure.com/conn");
            client.Dispose();
        }

        [TestMethod]
        public void Dispose_CalledTwice_ShouldNotThrow()
        {
            var client = new ShiftsClient(new Uri("https://test.azure.com/conn"), SharedMockCredential.Object);
            client.Dispose();
            client.Dispose();
        }

        private static ShiftsClient CreateMockedClient(HttpResponseMessage response)
        {
            return CreateMockedClient((request, cancellationToken) => Task.FromResult(response));
        }

        private static ShiftsClient CreateMockedClient(Func<HttpRequestMessage, CancellationToken, Task<HttpResponseMessage>> sendAsync)
        {
            var mockHandler = new Mock<HttpMessageHandler>();
            mockHandler.Protected()
                .Setup<Task<HttpResponseMessage>>("SendAsync", ItExpr.IsAny<HttpRequestMessage>(), ItExpr.IsAny<CancellationToken>())
                .Returns(sendAsync).Verifiable();
            var options = new ConnectorClientOptions();
            options.Transport = new HttpClientTransport(new HttpClient(mockHandler.Object));
            options.Retry.MaxRetries = 0;
            return new ShiftsClient(new Uri("https://test.azure.com/conn"), SharedMockCredential.Object, options);
        }

        private static async Task<List<string>> ReadItemIdsAsync<T>(IAsyncEnumerable<T> items)
        {
            var identifiers = new List<string>();
            await foreach (var item in items.ConfigureAwait(continueOnCapturedContext: false))
            {
                identifiers.Add(JsonSerializer.SerializeToElement(item).GetProperty("id").GetString()
                    ?? throw new AssertFailedException("Expected a non-null item identifier."));
            }

            return identifiers;
        }

        [TestMethod]
        [Owner("daviburg")]
        [DataRow(nameof(ShiftsClient.ListTimesOffAsync), "/v1.0/teams/team1/schedule/timesoff")]
        [DataRow(nameof(ShiftsClient.ListShiftsAsync), "/v1.0/teams/team1/schedule/shifts")]
        [DataRow(nameof(ShiftsClient.ListOpenShiftsAsync), "/v1.0/teams/team1/schedule/openShifts")]
        [DataRow(nameof(ShiftsClient.ListTimeOffReasonsAsync), "/v1.0/teams/team1/schedule/timeOffReasons")]
        [DataRow(nameof(ShiftsClient.ListSchedulingGroupsAsync), "/v1.0/teams/team1/schedule/schedulinggroups")]
        [DataRow(nameof(ShiftsClient.ListTimeOffRequestsAsync), "/v1.0/teams/team1/schedule/timeOffRequests")]
        [DataRow(nameof(ShiftsClient.ListOfferShiftRequestsAsync), "/v1.0/teams/team1/schedule/offerShiftRequests")]
        [DataRow(nameof(ShiftsClient.ListSwapShiftsChangeRequestsAsync), "/v1.0/teams/team1/schedule/swapShiftsChangeRequests")]
        [DataRow(nameof(ShiftsClient.ListOpenShiftChangeRequestsAsync), "/v1.0/teams/team1/schedule/openShiftChangeRequests")]
        [DataRow(nameof(ShiftsClient.ListOpenShiftsCrossTeamAsync), "/beta/me/joinedTeams/getOpenShifts")]
        [DataRow(nameof(ShiftsClient.ListShiftsCrossTeamAsync), "/beta/me/joinedTeams/getShifts")]
        [DataRow(nameof(ShiftsClient.ListTimesOffCrossTeamAsync), "/beta/me/joinedTeams/getTimesOff")]
        public async Task ListOperations_WithContinuation_ReturnEveryPage(string operation, string path)
        {
            var firstUrl = "https://test.azure.com/conn" + path;
            var nextUrl = firstUrl + "?$skiptoken=second";
            var requests = new List<string>();
            using var client = CreateMockedClient((request, cancellationToken) =>
            {
                Assert.AreEqual(expected: HttpMethod.Get, actual: request.Method);
                Assert.AreEqual(expected: new Uri(requests.Count == 0 ? firstUrl : nextUrl), actual: request.RequestUri);
                requests.Add(request.RequestUri?.AbsoluteUri ?? string.Empty);
                return Task.FromResult(new HttpResponseMessage(HttpStatusCode.OK)
                {
                    Content = new StringContent(requests.Count == 1
                        ? $$"""{"value":[{"id":"first"}],"@odata.nextLink":"{{nextUrl}}"}"""
                        : """{"value":[{"id":"second"}]}"""),
                });
            });

            var read = operation switch
            {
                nameof(ShiftsClient.ListTimesOffAsync) => ReadItemIdsAsync(client.ListTimesOffAsync(team: "team1")),
                nameof(ShiftsClient.ListShiftsAsync) => ReadItemIdsAsync(client.ListShiftsAsync(team: "team1")),
                nameof(ShiftsClient.ListOpenShiftsAsync) => ReadItemIdsAsync(client.ListOpenShiftsAsync(team: "team1")),
                nameof(ShiftsClient.ListTimeOffReasonsAsync) => ReadItemIdsAsync(client.ListTimeOffReasonsAsync(team: "team1")),
                nameof(ShiftsClient.ListSchedulingGroupsAsync) => ReadItemIdsAsync(client.ListSchedulingGroupsAsync(team: "team1")),
                nameof(ShiftsClient.ListTimeOffRequestsAsync) => ReadItemIdsAsync(client.ListTimeOffRequestsAsync(team: "team1")),
                nameof(ShiftsClient.ListOfferShiftRequestsAsync) => ReadItemIdsAsync(client.ListOfferShiftRequestsAsync(team: "team1")),
                nameof(ShiftsClient.ListSwapShiftsChangeRequestsAsync) => ReadItemIdsAsync(client.ListSwapShiftsChangeRequestsAsync(team: "team1")),
                nameof(ShiftsClient.ListOpenShiftChangeRequestsAsync) => ReadItemIdsAsync(client.ListOpenShiftChangeRequestsAsync(team: "team1")),
                nameof(ShiftsClient.ListOpenShiftsCrossTeamAsync) => ReadItemIdsAsync(client.ListOpenShiftsCrossTeamAsync()),
                nameof(ShiftsClient.ListShiftsCrossTeamAsync) => ReadItemIdsAsync(client.ListShiftsCrossTeamAsync()),
                nameof(ShiftsClient.ListTimesOffCrossTeamAsync) => ReadItemIdsAsync(client.ListTimesOffCrossTeamAsync()),
                _ => throw new AssertFailedException("Unexpected paging operation."),
            };
            var identifiers = await read.ConfigureAwait(continueOnCapturedContext: false);

            CollectionAssert.AreEqual(expected: new[] { "first", "second" }, actual: identifiers);
            CollectionAssert.AreEqual(expected: new[] { firstUrl, nextUrl }, actual: requests);
        }

        [TestMethod]
        [Owner("daviburg")]
        public async Task ListShiftsAsync_WithSavedContinuation_ResumesWithoutFirstPage()
        {
            const string nextUrl = "https://test.azure.com/conn/v1.0/teams/team1/schedule/shifts?$skiptoken=second";
            var attempts = 0;
            using var client = CreateMockedClient((request, cancellationToken) =>
            {
                attempts++;
                Assert.AreEqual(expected: HttpMethod.Get, actual: request.Method);
                Assert.AreEqual(expected: new Uri(nextUrl), actual: request.RequestUri);
                return Task.FromResult(new HttpResponseMessage(HttpStatusCode.OK)
                {
                    Content = new StringContent("""{"value":[{"id":"second"}]}"""),
                });
            });
            var identifiers = new List<string>();
            await foreach (var page in client.ListShiftsAsync(team: "team1").AsPages(continuationToken: nextUrl)
                .ConfigureAwait(continueOnCapturedContext: false))
            {
                Assert.IsNull(page.ContinuationToken);
                Assert.AreEqual(expected: 1, actual: page.Values.Count);
                identifiers.Add(page.Values[0].Id);
            }

            Assert.AreEqual(expected: 1, actual: attempts);
            CollectionAssert.AreEqual(expected: new[] { "second" }, actual: identifiers);
        }

        [TestMethod]
        [Owner("daviburg")]
        public async Task ListShiftsAsync_WithSecondPageError_ThrowsConnectorException()
        {
            const string firstUrl = "https://test.azure.com/conn/v1.0/teams/team1/schedule/shifts";
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

            await Assert.ThrowsExactlyAsync<ConnectorException>(() => ReadItemIdsAsync(client.ListShiftsAsync(team: "team1")))
                .ConfigureAwait(continueOnCapturedContext: false);

            Assert.AreEqual(expected: 2, actual: attempts);
        }

        [TestMethod]
        public async Task GetAllTeamsAsync_WithErrorResponse_ThrowsConnectorException()
        {
            using var responseMessage = new HttpResponseMessage
            {
                StatusCode = HttpStatusCode.BadRequest,
                Content = new StringContent("{\"error\": \"Bad request\"}")
            };
            using var client = CreateMockedClient(responseMessage);
            await Assert.ThrowsExactlyAsync<ConnectorException>(() =>
                client.GetAllTeamsAsync(cancellationToken: CancellationToken.None))
                .ConfigureAwait(continueOnCapturedContext: false);
        }
    }
}
