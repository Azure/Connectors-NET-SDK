//------------------------------------------------------------
// Copyright (c) Microsoft Corporation.  All rights reserved.
//------------------------------------------------------------

using System;
using System.Net;
using System.Net.Http;
using System.Threading;
using System.Threading.Tasks;
using Azure.Connectors.Sdk;
using Azure.Connectors.Sdk.Office365Groups;
using global::Azure.Core;
using Microsoft.VisualStudio.TestTools.UnitTesting;
using Moq;

namespace Azure.Connectors.Sdk.Tests
{
    [TestClass]
    public class Office365GroupsClientTests
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
            using var client = new Office365GroupsClient("https://test.azure.com/conn");
            Assert.IsNotNull(client);
        }

        [TestMethod]
        public void Constructor_WithNull_ShouldThrow()
        {
            Assert.ThrowsExactly<ArgumentNullException>(() => new Office365GroupsClient((string)null!));
        }

        [TestMethod]
        public void Dispose_ShouldNotThrow()
        {
            var client = new Office365GroupsClient("https://test.azure.com/conn");
            client.Dispose();
        }

        [TestMethod]
        public void Dispose_CalledTwice_ShouldNotThrow()
        {
            var client = new Office365GroupsClient(new Uri("https://test.azure.com/conn"), SharedMockCredential.Object);
            client.Dispose();
            client.Dispose();
        }

        [TestMethod]
        public async Task ListOwnedGroupsV2Async_TargetsOwnedObjectsRoute()
        {
            var request = await CaptureRequestAsync(
                    client => client.ListOwnedGroupsV2Async(cancellationToken: CancellationToken.None))
                .ConfigureAwait(continueOnCapturedContext: false);

            Assert.AreEqual(
                expected: "/conn/v1.0/me/ownedObjects/$/microsoft.graph.group",
                actual: request.RequestUri!.AbsolutePath);
        }

        [TestMethod]
        public async Task ListOwnedGroupsV0Async_TargetsLegacyMemberOfRoute()
        {
            var request = await CaptureRequestAsync(
                    client => client.ListOwnedGroupsV0Async(cancellationToken: CancellationToken.None))
                .ConfigureAwait(continueOnCapturedContext: false);

            Assert.AreEqual(
                expected: "/conn/v1.0/me/memberOf/$/microsoft.graph.group",
                actual: request.RequestUri!.AbsolutePath);
        }

        [TestMethod]
        public async Task ListOwnedGroupsAsync_TargetsCurrentMemberOfRoute()
        {
            var request = await CaptureRequestAsync(
                    client => client.ListOwnedGroupsAsync(cancellationToken: CancellationToken.None))
                .ConfigureAwait(continueOnCapturedContext: false);

            Assert.AreEqual(
                expected: "/conn/v2/v1.0/me/memberOf/$/microsoft.graph.group",
                actual: request.RequestUri!.AbsolutePath);
        }

        private static async Task<HttpRequestMessage> CaptureRequestAsync(Func<Office365GroupsClient, Task> invoke)
        {
            var clientSetup = ConnectorTestHelpers.CreateCapturingClientSetup(
                () => new HttpResponseMessage
                {
                    StatusCode = HttpStatusCode.OK,
                    Content = new StringContent("{\"value\":[]}")
                });
            using var client = new Office365GroupsClient(
                connectionRuntimeUrl: new Uri("https://test.azure.com/conn"),
                credential: clientSetup.Credential,
                options: clientSetup.Options);

            await invoke(client).ConfigureAwait(continueOnCapturedContext: false);

            var request = clientSetup.GetLastRequest();
            Assert.IsNotNull(request);
            return request!;
        }
    }
}
