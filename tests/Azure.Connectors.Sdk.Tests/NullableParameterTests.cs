//------------------------------------------------------------
// Copyright (c) Microsoft Corporation.  All rights reserved.
//------------------------------------------------------------

using System;
using System.Net;
using System.Net.Http;
using System.Threading;
using System.Threading.Tasks;
using Azure.Connectors.Sdk.Revai;
using Azure.Connectors.Sdk.Slack;
using Microsoft.VisualStudio.TestTools.UnitTesting;

namespace Azure.Connectors.Sdk.Tests
{
    /// <summary>
    /// Tests for nullable optional value-type parameters (#174).
    /// Verifies that nullable int? and bool? parameters correctly emit or omit query string values.
    /// </summary>
    [TestClass]
    public class NullableParameterTests
    {
        private static (TClient Client, Func<HttpRequestMessage?> GetLastRequest) CreateMockedClientWithCapture<TClient>(string responseContent = "[]")
            where TClient : ConnectorClientBase
        {
            var clientSetup = ConnectorTestHelpers.CreateCapturingClientSetup(
                () => new HttpResponseMessage
                {
                    StatusCode = HttpStatusCode.OK,
                    Content = new StringContent(responseContent),
                });

            var client = (TClient)Activator.CreateInstance(
                typeof(TClient),
                new Uri("https://test.azure.com/connection"),
                clientSetup.Credential,
                clientSetup.Options)!;

            return (client, clientSetup.GetLastRequest);
        }

        [TestMethod]
        public async Task NullableInt_WithNull_OmitsQueryParameter()
        {
            // Arrange
            var clientSetup = CreateMockedClientWithCapture<RevaiClient>();
            using (clientSetup.Client)
            {
                // Act — pass null for limit (int?)
                await clientSetup.Client
                    .TranscriptionsGetAsync(limit: null, cancellationToken: CancellationToken.None)
                    .ConfigureAwait(continueOnCapturedContext: false);

                // Assert — verify the request URL does not contain "limit="
                var request = clientSetup.GetLastRequest();
                Assert.IsNotNull(request, message: "Expected the client to issue an HTTP request.");
                Assert.IsFalse(request!.RequestUri!.Query.Contains("limit=", StringComparison.OrdinalIgnoreCase));
            }
        }

        [TestMethod]
        public async Task NullableInt_WithZero_EmitsQueryParameterWithZero()
        {
            // Arrange
            var clientSetup = CreateMockedClientWithCapture<RevaiClient>();
            using (clientSetup.Client)
            {
                // Act — pass 0 for limit (int?) — this is a valid distinct value, not "unspecified"
                await clientSetup.Client
                    .TranscriptionsGetAsync(limit: 0, cancellationToken: CancellationToken.None)
                    .ConfigureAwait(continueOnCapturedContext: false);

                // Assert — verify the request URL contains "limit=0"
                var request = clientSetup.GetLastRequest();
                Assert.IsNotNull(request, message: "Expected the client to issue an HTTP request.");
                Assert.IsTrue(request!.RequestUri!.Query.Contains("limit=0", StringComparison.OrdinalIgnoreCase));
            }
        }

        [TestMethod]
        public async Task NullableInt_WithPositiveValue_EmitsQueryParameter()
        {
            // Arrange
            var clientSetup = CreateMockedClientWithCapture<RevaiClient>();
            using (clientSetup.Client)
            {
                // Act
                await clientSetup.Client
                    .TranscriptionsGetAsync(limit: 25, cancellationToken: CancellationToken.None)
                    .ConfigureAwait(continueOnCapturedContext: false);

                // Assert — verify the request URL contains "limit=25"
                var request = clientSetup.GetLastRequest();
                Assert.IsNotNull(request, message: "Expected the client to issue an HTTP request.");
                Assert.IsTrue(request!.RequestUri!.Query.Contains("limit=25", StringComparison.OrdinalIgnoreCase));
            }
        }

        [TestMethod]
        public async Task NullableBool_WithNull_OmitsQueryParameter()
        {
            // Arrange
            var clientSetup = CreateMockedClientWithCapture<SlackClient>(responseContent: "{}");
            using (clientSetup.Client)
            {
                // Act — pass null for isPrivateChannel (bool?)
                await clientSetup.Client
                    .CreateChannelAsync(name: "test-channel", isPrivateChannel: null, cancellationToken: CancellationToken.None)
                    .ConfigureAwait(continueOnCapturedContext: false);

                // Assert — verify the request URL does not contain "is_private="
                var request = clientSetup.GetLastRequest();
                Assert.IsNotNull(request, message: "Expected the client to issue an HTTP request.");
                Assert.IsFalse(request!.RequestUri!.Query.Contains("is_private=", StringComparison.OrdinalIgnoreCase));
            }
        }

        [TestMethod]
        public async Task NullableBool_WithFalse_EmitsQueryParameterWithFalse()
        {
            // Arrange
            var clientSetup = CreateMockedClientWithCapture<SlackClient>(responseContent: "{}");
            using (clientSetup.Client)
            {
                // Act — pass false for isPrivateChannel (bool?) — this is a valid distinct value, not "unspecified"
                await clientSetup.Client
                    .CreateChannelAsync(name: "test-channel", isPrivateChannel: false, cancellationToken: CancellationToken.None)
                    .ConfigureAwait(continueOnCapturedContext: false);

                // Assert — verify the request URL contains "is_private=False"
                var request = clientSetup.GetLastRequest();
                Assert.IsNotNull(request, message: "Expected the client to issue an HTTP request.");
                Assert.IsTrue(request!.RequestUri!.Query.Contains("is_private=False", StringComparison.Ordinal));
            }
        }

        [TestMethod]
        public async Task NullableBool_WithTrue_EmitsQueryParameterWithTrue()
        {
            // Arrange
            var clientSetup = CreateMockedClientWithCapture<SlackClient>(responseContent: "{}");
            using (clientSetup.Client)
            {
                // Act
                await clientSetup.Client
                    .CreateChannelAsync(name: "test-channel", isPrivateChannel: true, cancellationToken: CancellationToken.None)
                    .ConfigureAwait(continueOnCapturedContext: false);

                // Assert — verify the request URL contains "is_private=True"
                var request = clientSetup.GetLastRequest();
                Assert.IsNotNull(request, message: "Expected the client to issue an HTTP request.");
                Assert.IsTrue(request!.RequestUri!.Query.Contains("is_private=True", StringComparison.Ordinal));
            }
        }
    }
}
