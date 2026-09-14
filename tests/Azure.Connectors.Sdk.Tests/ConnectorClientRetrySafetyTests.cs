//------------------------------------------------------------
// Copyright (c) Microsoft Corporation.  All rights reserved.
//------------------------------------------------------------

using System.Net;
using global::Azure.Core;
using global::Azure.Core.Pipeline;
using Microsoft.VisualStudio.TestTools.UnitTesting;
using Moq;

namespace Azure.Connectors.Sdk.Tests
{
    [TestClass]
    public class ConnectorClientRetrySafetyTests
    {
        private static readonly Uri ConnectionRuntimeUrl = new("https://test.azure.com/connection");

        [TestMethod]
        public void ConnectorClientOptions_RetryUnsafeHttpMethods_DefaultsToFalse()
        {
            var options = new ConnectorClientOptions();

            Assert.IsFalse(options.RetryUnsafeHttpMethods);
        }

        [TestMethod]
        public async Task CallConnectorAsync_GetWithTransientResponse_RetriesAndSucceeds()
        {
            var handler = ConnectorClientRetrySafetyTests.CreateResponseHandler(
                HttpStatusCode.InternalServerError,
                HttpStatusCode.OK);
            using var client = ConnectorClientRetrySafetyTests.CreateClient(handler, maxRetries: 1);

            await client
                .SendAsync(HttpMethod.Get)
                .ConfigureAwait(continueOnCapturedContext: false);

            Assert.AreEqual(expected: 2, actual: handler.AttemptCount);
        }

        [TestMethod]
        [DataRow("HEAD")]
        [DataRow("OPTIONS")]
        [DataRow("TRACE")]
        public async Task CallConnectorAsync_SafeMethodWithTransientResponse_RetriesAndSucceeds(string methodName)
        {
            var handler = ConnectorClientRetrySafetyTests.CreateResponseHandler(
                HttpStatusCode.InternalServerError,
                HttpStatusCode.OK);
            using var client = ConnectorClientRetrySafetyTests.CreateClient(handler, maxRetries: 1);

            await client
                .SendAsync(new HttpMethod(methodName))
                .ConfigureAwait(continueOnCapturedContext: false);

            Assert.AreEqual(expected: 2, actual: handler.AttemptCount);
        }

        [TestMethod]
        [DataRow("POST")]
        [DataRow("PUT")]
        [DataRow("PATCH")]
        [DataRow("DELETE")]
        [DataRow("CUSTOM")]
        public async Task CallConnectorAsync_UnsafeMethodWithTransientResponse_AttemptsOnceByDefault(string methodName)
        {
            var handler = ConnectorClientRetrySafetyTests.CreateResponseHandler(
                HttpStatusCode.InternalServerError,
                HttpStatusCode.OK);
            using var client = ConnectorClientRetrySafetyTests.CreateClient(handler, maxRetries: 1);

            await Assert
                .ThrowsExactlyAsync<ConnectorException>(() => client.SendAsync(new HttpMethod(methodName)))
                .ConfigureAwait(continueOnCapturedContext: false);

            Assert.AreEqual(expected: 1, actual: handler.AttemptCount);
        }

        [TestMethod]
        public async Task CallConnectorAsync_PostWithTransientResponseAndUnsafeRetryOptIn_RetriesAndSucceeds()
        {
            var handler = ConnectorClientRetrySafetyTests.CreateResponseHandler(
                HttpStatusCode.InternalServerError,
                HttpStatusCode.OK);
            using var client = ConnectorClientRetrySafetyTests.CreateClient(
                handler,
                maxRetries: 1,
                retryUnsafeHttpMethods: true);

            await client
                .SendAsync(HttpMethod.Post)
                .ConfigureAwait(continueOnCapturedContext: false);

            Assert.AreEqual(expected: 2, actual: handler.AttemptCount);
        }

        [TestMethod]
        [DataRow((int)HttpStatusCode.TooManyRequests)]
        [DataRow((int)HttpStatusCode.InternalServerError)]
        [DataRow((int)HttpStatusCode.BadGateway)]
        [DataRow((int)HttpStatusCode.ServiceUnavailable)]
        [DataRow((int)HttpStatusCode.GatewayTimeout)]
        public async Task CallConnectorAsync_EligibleMethodWithAzureCoreTransientStatus_RetriesAndSucceeds(int statusCode)
        {
            var handler = ConnectorClientRetrySafetyTests.CreateResponseHandler(
                (HttpStatusCode)statusCode,
                HttpStatusCode.OK);
            using var client = ConnectorClientRetrySafetyTests.CreateClient(handler, maxRetries: 1);

            await client
                .SendAsync(HttpMethod.Get)
                .ConfigureAwait(continueOnCapturedContext: false);

            Assert.AreEqual(expected: 2, actual: handler.AttemptCount);
        }

        [TestMethod]
        public async Task CallConnectorAsync_ConfiguredMaxRetries_UsesExactAttemptCount()
        {
            var handler = ConnectorClientRetrySafetyTests.CreateResponseHandler(
                HttpStatusCode.InternalServerError,
                HttpStatusCode.InternalServerError,
                HttpStatusCode.OK);
            using var client = ConnectorClientRetrySafetyTests.CreateClient(handler, maxRetries: 2);

            await client
                .SendAsync(HttpMethod.Get)
                .ConfigureAwait(continueOnCapturedContext: false);

            Assert.AreEqual(expected: 3, actual: handler.AttemptCount);
        }

        [TestMethod]
        public async Task CallConnectorAsync_GetWithTransportException_RetriesAndSucceeds()
        {
            var handler = ConnectorClientRetrySafetyTests.CreateExceptionThenSuccessHandler();
            using var client = ConnectorClientRetrySafetyTests.CreateClient(handler, maxRetries: 1);

            await client
                .SendAsync(HttpMethod.Get)
                .ConfigureAwait(continueOnCapturedContext: false);

            Assert.AreEqual(expected: 2, actual: handler.AttemptCount);
        }

        [TestMethod]
        [DataRow("POST")]
        [DataRow("PUT")]
        [DataRow("PATCH")]
        [DataRow("DELETE")]
        public async Task CallConnectorAsync_UnsafeMethodWithTransportException_AttemptsOnceByDefault(string methodName)
        {
            var handler = ConnectorClientRetrySafetyTests.CreateExceptionThenSuccessHandler();
            using var client = ConnectorClientRetrySafetyTests.CreateClient(handler, maxRetries: 1);

            await Assert
                .ThrowsExactlyAsync<RequestFailedException>(() => client.SendAsync(new HttpMethod(methodName)))
                .ConfigureAwait(continueOnCapturedContext: false);

            Assert.AreEqual(expected: 1, actual: handler.AttemptCount);
        }

        [TestMethod]
        public async Task CallConnectorAsync_PostWithTransportExceptionAndUnsafeRetryOptIn_RetriesAndSucceeds()
        {
            var handler = ConnectorClientRetrySafetyTests.CreateExceptionThenSuccessHandler();
            using var client = ConnectorClientRetrySafetyTests.CreateClient(
                handler,
                maxRetries: 1,
                retryUnsafeHttpMethods: true);

            await client
                .SendAsync(HttpMethod.Post)
                .ConfigureAwait(continueOnCapturedContext: false);

            Assert.AreEqual(expected: 2, actual: handler.AttemptCount);
        }

        [TestMethod]
        public async Task CallConnectorAsync_GetWithPerRetryPolicyException_RetriesAndSucceeds()
        {
            var handler = ConnectorClientRetrySafetyTests.CreateResponseHandler(HttpStatusCode.OK);
            var policy = new ThrowOncePolicy();
            var options = ConnectorClientRetrySafetyTests.CreateOptions(handler, maxRetries: 1);
            options.AddPolicy(policy, HttpPipelinePosition.PerRetry);
            using var client = ConnectorClientRetrySafetyTests.CreateClient(options);

            await client
                .SendAsync(HttpMethod.Get)
                .ConfigureAwait(continueOnCapturedContext: false);

            Assert.AreEqual(expected: 2, actual: policy.AsyncProcessCount);
            Assert.AreEqual(expected: 1, actual: handler.AttemptCount);
        }

        [TestMethod]
        public async Task CallConnectorAsync_PostWithPerRetryPolicyException_AttemptsOnceByDefault()
        {
            var handler = ConnectorClientRetrySafetyTests.CreateResponseHandler(HttpStatusCode.OK);
            var policy = new ThrowOncePolicy();
            var options = ConnectorClientRetrySafetyTests.CreateOptions(handler, maxRetries: 1);
            options.AddPolicy(policy, HttpPipelinePosition.PerRetry);
            using var client = ConnectorClientRetrySafetyTests.CreateClient(options);

            await Assert
                .ThrowsExactlyAsync<IOException>(() => client.SendAsync(HttpMethod.Post))
                .ConfigureAwait(continueOnCapturedContext: false);

            Assert.AreEqual(expected: 1, actual: policy.AsyncProcessCount);
            Assert.AreEqual(expected: 0, actual: handler.AttemptCount);
        }

        [TestMethod]
        public async Task CallConnectorAsync_Cancellation_IsNotRetried()
        {
            var handler = new SequenceHttpMessageHandler(
                (_, cancellationToken) => Task.FromCanceled<HttpResponseMessage>(cancellationToken));
            using var client = ConnectorClientRetrySafetyTests.CreateClient(handler, maxRetries: 2);
            using var cancellationTokenSource = new CancellationTokenSource();
            cancellationTokenSource.Cancel();

            await Assert
                .ThrowsExactlyAsync<TaskCanceledException>(
                    () => client.SendAsync(HttpMethod.Get, cancellationTokenSource.Token))
                .ConfigureAwait(continueOnCapturedContext: false);

            Assert.AreEqual(expected: 1, actual: handler.AttemptCount);
        }

        [TestMethod]
        public async Task CallConnectorAsync_CustomTransport_ReceivesRequest()
        {
            HttpRequestMessage? receivedRequest = null;
            var handler = new SequenceHttpMessageHandler(
                (request, _) =>
                {
                    receivedRequest = request;
                    return Task.FromResult(new HttpResponseMessage(HttpStatusCode.OK));
                });
            using var client = ConnectorClientRetrySafetyTests.CreateClient(handler, maxRetries: 1);

            await client
                .SendAsync(HttpMethod.Get)
                .ConfigureAwait(continueOnCapturedContext: false);

            Assert.IsNotNull(receivedRequest);
            Assert.AreEqual(expected: HttpMethod.Get, actual: receivedRequest.Method);
        }

        [TestMethod]
        public async Task CallConnectorAsync_UserPolicies_RetainPerCallAndPerRetrySemantics()
        {
            var handler = ConnectorClientRetrySafetyTests.CreateResponseHandler(
                HttpStatusCode.InternalServerError,
                HttpStatusCode.OK);
            var perCallPolicy = new CountingPolicy();
            var perRetryPolicy = new CountingPolicy();
            var options = ConnectorClientRetrySafetyTests.CreateOptions(handler, maxRetries: 1);
            options.AddPolicy(perCallPolicy, HttpPipelinePosition.PerCall);
            options.AddPolicy(perRetryPolicy, HttpPipelinePosition.PerRetry);
            using var client = ConnectorClientRetrySafetyTests.CreateClient(options);

            await client
                .SendAsync(HttpMethod.Get)
                .ConfigureAwait(continueOnCapturedContext: false);

            Assert.AreEqual(expected: 1, actual: perCallPolicy.AsyncProcessCount);
            Assert.AreEqual(expected: 2, actual: perRetryPolicy.AsyncProcessCount);
        }

        [TestMethod]
        public async Task CallConnectorAsync_CustomRetryPolicy_RemainsAuthoritative()
        {
            var handler = ConnectorClientRetrySafetyTests.CreateResponseHandler(
                HttpStatusCode.InternalServerError,
                HttpStatusCode.OK);
            var customRetryPolicy = new CountingPolicy();
            var options = ConnectorClientRetrySafetyTests.CreateOptions(
                handler,
                maxRetries: 1,
                retryUnsafeHttpMethods: true);
            options.RetryPolicy = customRetryPolicy;
            using var client = ConnectorClientRetrySafetyTests.CreateClient(options);

            await Assert
                .ThrowsExactlyAsync<ConnectorException>(() => client.SendAsync(HttpMethod.Post))
                .ConfigureAwait(continueOnCapturedContext: false);

            Assert.AreEqual(expected: 1, actual: customRetryPolicy.AsyncProcessCount);
            Assert.AreEqual(expected: 1, actual: handler.AttemptCount);
        }

        [TestMethod]
        public async Task CallConnectorAsync_EligibleRetry_AuthenticatesEveryAttempt()
        {
            var authorizationHeaders = new List<string?>();
            var handler = new SequenceHttpMessageHandler(
                (request, _) =>
                {
                    authorizationHeaders.Add(request.Headers.Authorization?.ToString());
                    return Task.FromResult(new HttpResponseMessage(HttpStatusCode.InternalServerError));
                },
                (request, _) =>
                {
                    authorizationHeaders.Add(request.Headers.Authorization?.ToString());
                    return Task.FromResult(new HttpResponseMessage(HttpStatusCode.OK));
                });
            using var client = ConnectorClientRetrySafetyTests.CreateClient(handler, maxRetries: 1);

            await client
                .SendAsync(HttpMethod.Get)
                .ConfigureAwait(continueOnCapturedContext: false);

            CollectionAssert.AreEqual(
                expected: new[] { "Bearer mock-token", "Bearer mock-token" },
                actual: authorizationHeaders);
        }

        [TestMethod]
        public async Task CallConnectorAsync_JsonBodyWithUnsafeRetryOptIn_ReplaysIdenticalContent()
        {
            var requestBodies = new List<byte[]>();
            var handler = ConnectorClientRetrySafetyTests.CreateContentCapturingHandler(requestBodies);
            using var client = ConnectorClientRetrySafetyTests.CreateClient(
                handler,
                maxRetries: 1,
                retryUnsafeHttpMethods: true);

            await client
                .SendJsonAsync(HttpMethod.Post, new { Message = "hello" })
                .ConfigureAwait(continueOnCapturedContext: false);

            Assert.AreEqual(expected: 2, actual: requestBodies.Count);
            CollectionAssert.AreEqual(expected: requestBodies[0], actual: requestBodies[1]);
        }

        [TestMethod]
        public async Task CallConnectorAsync_BinaryBodyWithUnsafeRetryOptIn_ReplaysIdenticalContent()
        {
            var requestBodies = new List<byte[]>();
            var handler = ConnectorClientRetrySafetyTests.CreateContentCapturingHandler(requestBodies);
            using var client = ConnectorClientRetrySafetyTests.CreateClient(
                handler,
                maxRetries: 1,
                retryUnsafeHttpMethods: true);
            var body = new byte[] { 0x00, 0x7F, 0xFF };

            await client
                .SendBinaryAsync(HttpMethod.Post, body)
                .ConfigureAwait(continueOnCapturedContext: false);

            Assert.AreEqual(expected: 2, actual: requestBodies.Count);
            CollectionAssert.AreEqual(expected: body, actual: requestBodies[0]);
            CollectionAssert.AreEqual(expected: requestBodies[0], actual: requestBodies[1]);
        }

        private static SequenceHttpMessageHandler CreateResponseHandler(params HttpStatusCode[] statusCodes)
        {
            return new SequenceHttpMessageHandler(
                statusCodes
                    .Select<HttpStatusCode, Func<HttpRequestMessage, CancellationToken, Task<HttpResponseMessage>>>(
                        statusCode => (_, _) => Task.FromResult(new HttpResponseMessage(statusCode)))
                    .ToArray());
        }

        private static SequenceHttpMessageHandler CreateExceptionThenSuccessHandler()
        {
            return new SequenceHttpMessageHandler(
                (_, _) => throw new HttpRequestException("The outcome is unknown."),
                (_, _) => Task.FromResult(new HttpResponseMessage(HttpStatusCode.OK)));
        }

        private static SequenceHttpMessageHandler CreateContentCapturingHandler(List<byte[]> requestBodies)
        {
            return new SequenceHttpMessageHandler(
                async (request, cancellationToken) =>
                {
                    requestBodies.Add(await request.Content!
                        .ReadAsByteArrayAsync(cancellationToken)
                        .ConfigureAwait(continueOnCapturedContext: false));
                    return new HttpResponseMessage(HttpStatusCode.InternalServerError);
                },
                async (request, cancellationToken) =>
                {
                    requestBodies.Add(await request.Content!
                        .ReadAsByteArrayAsync(cancellationToken)
                        .ConfigureAwait(continueOnCapturedContext: false));
                    return new HttpResponseMessage(HttpStatusCode.OK);
                });
        }

        private static TestConnectorClient CreateClient(
            SequenceHttpMessageHandler handler,
            int maxRetries,
            bool retryUnsafeHttpMethods = false)
        {
            return ConnectorClientRetrySafetyTests.CreateClient(
                ConnectorClientRetrySafetyTests.CreateOptions(
                    handler,
                    maxRetries,
                    retryUnsafeHttpMethods));
        }

        private static ConnectorClientOptions CreateOptions(
            SequenceHttpMessageHandler handler,
            int maxRetries,
            bool retryUnsafeHttpMethods = false)
        {
            var options = new ConnectorClientOptions
            {
                RetryUnsafeHttpMethods = retryUnsafeHttpMethods,
                Transport = new HttpClientTransport(new HttpClient(handler)),
            };
            options.Retry.Delay = TimeSpan.Zero;
            options.Retry.MaxDelay = TimeSpan.Zero;
            options.Retry.MaxRetries = maxRetries;
            options.Retry.Mode = RetryMode.Fixed;
            options.Retry.NetworkTimeout = TimeSpan.FromSeconds(5);
            return options;
        }

        private static TestConnectorClient CreateClient(ConnectorClientOptions options)
        {
            var credential = new Mock<TokenCredential>();
            credential
                .Setup(mock => mock.GetTokenAsync(It.IsAny<TokenRequestContext>(), It.IsAny<CancellationToken>()))
                .ReturnsAsync(new AccessToken("mock-token", DateTimeOffset.MaxValue));

            return new TestConnectorClient(
                ConnectorClientRetrySafetyTests.ConnectionRuntimeUrl,
                credential.Object,
                options);
        }

        private sealed class TestConnectorClient : ConnectorClientBase
        {
            public TestConnectorClient(
                Uri connectionRuntimeUrl,
                TokenCredential credential,
                ConnectorClientOptions options)
                : base(connectionRuntimeUrl, credential, options)
            {
            }

            public override string ConnectorName => "TestConnector";

            public Task SendAsync(HttpMethod method, CancellationToken cancellationToken = default)
            {
                return this.CallConnectorAsync(method, "/operation", cancellationToken: cancellationToken);
            }

            public Task SendJsonAsync(HttpMethod method, object body)
            {
                return this.CallConnectorAsync(method, "/operation", body);
            }

            public Task SendBinaryAsync(HttpMethod method, byte[] body)
            {
                return this.CallConnectorAsync(
                    method,
                    "/operation",
                    body,
                    "application/octet-stream");
            }
        }

        private sealed class SequenceHttpMessageHandler : HttpMessageHandler
        {
            private readonly Queue<Func<HttpRequestMessage, CancellationToken, Task<HttpResponseMessage>>> _attempts;

            public SequenceHttpMessageHandler(
                params Func<HttpRequestMessage, CancellationToken, Task<HttpResponseMessage>>[] attempts)
            {
                this._attempts = new Queue<Func<HttpRequestMessage, CancellationToken, Task<HttpResponseMessage>>>(attempts);
            }

            public int AttemptCount { get; private set; }

            protected override Task<HttpResponseMessage> SendAsync(
                HttpRequestMessage request,
                CancellationToken cancellationToken)
            {
                this.AttemptCount++;
                return this._attempts.Dequeue().Invoke(request, cancellationToken);
            }
        }

        private sealed class CountingPolicy : HttpPipelinePolicy
        {
            public int AsyncProcessCount { get; private set; }

            public override void Process(
                HttpMessage message,
                ReadOnlyMemory<HttpPipelinePolicy> pipeline)
            {
                HttpPipelinePolicy.ProcessNext(message, pipeline);
            }

            public override async ValueTask ProcessAsync(
                HttpMessage message,
                ReadOnlyMemory<HttpPipelinePolicy> pipeline)
            {
                this.AsyncProcessCount++;
                await HttpPipelinePolicy
                    .ProcessNextAsync(message, pipeline)
                    .ConfigureAwait(continueOnCapturedContext: false);
            }
        }

        private sealed class ThrowOncePolicy : HttpPipelinePolicy
        {
            public int AsyncProcessCount { get; private set; }

            public override void Process(
                HttpMessage message,
                ReadOnlyMemory<HttpPipelinePolicy> pipeline)
            {
                throw new NotSupportedException("Synchronous test execution is not supported.");
            }

            public override async ValueTask ProcessAsync(
                HttpMessage message,
                ReadOnlyMemory<HttpPipelinePolicy> pipeline)
            {
                this.AsyncProcessCount++;
                if (this.AsyncProcessCount == 1)
                {
                    throw new IOException("The outcome is unknown.");
                }

                await HttpPipelinePolicy
                    .ProcessNextAsync(message, pipeline)
                    .ConfigureAwait(continueOnCapturedContext: false);
            }
        }
    }
}
