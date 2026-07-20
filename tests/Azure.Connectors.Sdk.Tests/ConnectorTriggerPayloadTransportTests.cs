//------------------------------------------------------------
// Copyright (c) Microsoft Corporation.  All rights reserved.
//------------------------------------------------------------

using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Text;
using System.Threading;
using System.Threading.Tasks;
using Azure.Connectors.Sdk.OneDriveForBusiness.Models;
using Microsoft.VisualStudio.TestTools.UnitTesting;

namespace Azure.Connectors.Sdk.Tests
{
    /// <summary>
    /// Tests for the transport-aware overload of <see cref="ConnectorTriggerPayload.ReadAsync{TPayload}(ConnectorTriggerTransport, ConnectorTriggerIdentity, IConnectorNamespaceTriggerConfigResolver, long, CancellationToken)"/>.
    /// </summary>
    [TestClass]
    public class ConnectorTriggerPayloadTransportTests
    {
        private const string ExpectedConnectorName = "onedriveforbusiness";
        private const string ExpectedOperationName = "OnNewFilesV2";
        private const string SubscriptionId = "11111111-2222-3333-4444-555555555555";
        private const string ResourceGroupName = "prod-connectors-rg";
        private const string ConnectorNamespaceName = "my-gateway";
        private const string TriggerConfigName = "email-trigger";
        private const string MetadataPayload = """
            {"body":{"value":[{"Id":"01ABC","Name":"report.docx","Path":"/Documents/report.docx","Size":1234,"IsFolder":false}]}}
            """;

        private static ConnectorTriggerIdentity ExpectedIdentity => new(
            ConnectorTriggerPayloadTransportTests.ExpectedConnectorName,
            ConnectorTriggerPayloadTransportTests.ExpectedOperationName);

        private static ConnectorNamespaceTriggerConfig MatchingTriggerConfig => new(
            ConnectorTriggerPayloadTransportTests.ExpectedConnectorName,
            ConnectorTriggerPayloadTransportTests.ExpectedOperationName);

        private static ConnectorTriggerTransport CreateTransport(
            string body,
            string subscriptionId = ConnectorTriggerPayloadTransportTests.SubscriptionId,
            string resourceGroupName = ConnectorTriggerPayloadTransportTests.ResourceGroupName,
            string connectorNamespaceName = ConnectorTriggerPayloadTransportTests.ConnectorNamespaceName,
            string triggerConfigName = ConnectorTriggerPayloadTransportTests.TriggerConfigName,
            string? correlationId = null,
            IDictionary<string, IEnumerable<string>>? extraHeaders = null)
        {
            var headers = new Dictionary<string, IEnumerable<string>>
            {
                [ConnectorTriggerHeaderNames.SubscriptionId] = new[] { subscriptionId },
                [ConnectorTriggerHeaderNames.ResourceGroupName] = new[] { resourceGroupName },
                [ConnectorTriggerHeaderNames.ConnectorNamespaceName] = new[] { connectorNamespaceName },
                [ConnectorTriggerHeaderNames.TriggerConfigName] = new[] { triggerConfigName },
            };

            if (correlationId is not null)
            {
                headers[ConnectorTriggerHeaderNames.CorrelationId] = new[] { correlationId };
            }

            if (extraHeaders is not null)
            {
                foreach (var header in extraHeaders)
                {
                    headers[header.Key] = header.Value;
                }
            }

            return new ConnectorTriggerTransport
            {
                Body = new MemoryStream(Encoding.UTF8.GetBytes(body)),
                Headers = headers,
            };
        }

        [TestMethod]
        public async Task ReadAsync_Transport_ResolvedIdentityMatches_ReturnsPayload()
        {
            // Arrange
            var transport = ConnectorTriggerPayloadTransportTests.CreateTransport(
                ConnectorTriggerPayloadTransportTests.MetadataPayload);
            var resolver = new StubTriggerConfigResolver(
                ConnectorTriggerPayloadTransportTests.MatchingTriggerConfig);

            // Act
            var payload = await ConnectorTriggerPayload
                .ReadAsync<OneDriveForBusinessOnNewFilesTriggerPayload>(
                    transport,
                    ConnectorTriggerPayloadTransportTests.ExpectedIdentity,
                    resolver)
                .ConfigureAwait(continueOnCapturedContext: false);

            // Assert
            Assert.IsNotNull(payload);
            Assert.AreEqual("report.docx", payload.Body?.Value?[0].Name);
            Assert.IsNotNull(resolver.LastRequestedResourceIdentity);
            var requestedResourceIdentity = resolver.LastRequestedResourceIdentity!;
            Assert.AreEqual(ConnectorTriggerPayloadTransportTests.SubscriptionId, requestedResourceIdentity.SubscriptionId);
            Assert.AreEqual(ConnectorTriggerPayloadTransportTests.ResourceGroupName, requestedResourceIdentity.ResourceGroupName);
            Assert.AreEqual(ConnectorTriggerPayloadTransportTests.ConnectorNamespaceName, requestedResourceIdentity.ConnectorNamespaceName);
            Assert.AreEqual(ConnectorTriggerPayloadTransportTests.TriggerConfigName, requestedResourceIdentity.TriggerConfigName);
        }

        [TestMethod]
        public async Task ReadAsync_Transport_HeaderNameCaseInsensitive_Validates()
        {
            // Arrange — the SDK, not the transport, performs OrdinalIgnoreCase lookup.
            var headers = new Dictionary<string, IEnumerable<string>>
            {
                ["X-MS-SUBSCRIPTION-ID"] = new[] { ConnectorTriggerPayloadTransportTests.SubscriptionId },
                ["X-MS-RESOURCE-GROUP"] = new[] { ConnectorTriggerPayloadTransportTests.ResourceGroupName },
                ["X-MS-GATEWAY-RESOURCE-NAME"] = new[] { ConnectorTriggerPayloadTransportTests.ConnectorNamespaceName },
                ["X-MS-TRIGGER-NAME"] = new[] { ConnectorTriggerPayloadTransportTests.TriggerConfigName },
            };

            var transport = new ConnectorTriggerTransport
            {
                Body = new MemoryStream(Encoding.UTF8.GetBytes(ConnectorTriggerPayloadTransportTests.MetadataPayload)),
                Headers = headers,
            };

            var resolver = new StubTriggerConfigResolver(
                ConnectorTriggerPayloadTransportTests.MatchingTriggerConfig);

            // Act
            var payload = await ConnectorTriggerPayload
                .ReadAsync<OneDriveForBusinessOnNewFilesTriggerPayload>(
                    transport,
                    ConnectorTriggerPayloadTransportTests.ExpectedIdentity,
                    resolver)
                .ConfigureAwait(continueOnCapturedContext: false);

            // Assert
            Assert.IsNotNull(payload);
        }

        [TestMethod]
        public async Task ReadAsync_Transport_SubscriptionHeaderMissing_ThrowsResourceIdentityException()
        {
            await this.AssertMissingHeaderThrowsAsync(ConnectorTriggerHeaderNames.SubscriptionId)
                .ConfigureAwait(continueOnCapturedContext: false);
        }

        [TestMethod]
        public async Task ReadAsync_Transport_ResourceGroupHeaderMissing_ThrowsResourceIdentityException()
        {
            await this.AssertMissingHeaderThrowsAsync(ConnectorTriggerHeaderNames.ResourceGroupName)
                .ConfigureAwait(continueOnCapturedContext: false);
        }

        [TestMethod]
        public async Task ReadAsync_Transport_ConnectorNamespaceHeaderMissing_ThrowsResourceIdentityException()
        {
            await this.AssertMissingHeaderThrowsAsync(ConnectorTriggerHeaderNames.ConnectorNamespaceName)
                .ConfigureAwait(continueOnCapturedContext: false);
        }

        [TestMethod]
        public async Task ReadAsync_Transport_TriggerConfigHeaderMissing_ThrowsResourceIdentityException()
        {
            await this.AssertMissingHeaderThrowsAsync(ConnectorTriggerHeaderNames.TriggerConfigName)
                .ConfigureAwait(continueOnCapturedContext: false);
        }

        [TestMethod]
        public async Task ReadAsync_Transport_ResolvedConnectorMismatch_ThrowsIdentityMismatch()
        {
            // Arrange
            const string correlationId = "abc-123-def";
            var transport = ConnectorTriggerPayloadTransportTests.CreateTransport(
                ConnectorTriggerPayloadTransportTests.MetadataPayload,
                correlationId: correlationId);
            var resolver = new StubTriggerConfigResolver(
                new ConnectorNamespaceTriggerConfig(
                    ConnectorName: "sharepointonline",
                    OperationName: ConnectorTriggerPayloadTransportTests.ExpectedOperationName));

            // Act
            var exception = await Assert.ThrowsExactlyAsync<ConnectorTriggerIdentityMismatchException>(
                async () => await ConnectorTriggerPayload
                    .ReadAsync<OneDriveForBusinessOnNewFilesTriggerPayload>(
                        transport,
                        ConnectorTriggerPayloadTransportTests.ExpectedIdentity,
                        resolver)
                    .ConfigureAwait(continueOnCapturedContext: false))
                .ConfigureAwait(continueOnCapturedContext: false);

            // Assert
            Assert.AreEqual("sharepointonline", exception.ResolvedConnectorName);
            Assert.AreEqual(ConnectorTriggerPayloadTransportTests.ExpectedOperationName, exception.ResolvedOperationName);
            Assert.AreEqual(correlationId, exception.CorrelationId);
            Assert.AreEqual(ConnectorTriggerPayloadTransportTests.ConnectorNamespaceName, exception.ResourceIdentity.ConnectorNamespaceName);
            StringAssert.Contains(exception.Message, "sharepointonline");
        }

        [TestMethod]
        public async Task ReadAsync_Transport_ResolvedOperationMismatch_ThrowsIdentityMismatch()
        {
            // Arrange
            var transport = ConnectorTriggerPayloadTransportTests.CreateTransport(
                ConnectorTriggerPayloadTransportTests.MetadataPayload);
            var resolver = new StubTriggerConfigResolver(
                new ConnectorNamespaceTriggerConfig(
                    ConnectorName: ConnectorTriggerPayloadTransportTests.ExpectedConnectorName,
                    OperationName: "OnUpdatedFilesV2"));

            // Act
            var exception = await Assert.ThrowsExactlyAsync<ConnectorTriggerIdentityMismatchException>(
                async () => await ConnectorTriggerPayload
                    .ReadAsync<OneDriveForBusinessOnNewFilesTriggerPayload>(
                        transport,
                        ConnectorTriggerPayloadTransportTests.ExpectedIdentity,
                        resolver)
                    .ConfigureAwait(continueOnCapturedContext: false))
                .ConfigureAwait(continueOnCapturedContext: false);

            // Assert
            Assert.AreEqual(ConnectorTriggerPayloadTransportTests.ExpectedConnectorName, exception.ResolvedConnectorName);
            Assert.AreEqual("OnUpdatedFilesV2", exception.ResolvedOperationName);
            StringAssert.Contains(exception.Message, "OnUpdatedFilesV2");
        }

        [TestMethod]
        public async Task ReadAsync_Transport_ResolverFailure_ThrowsConfigurationResolutionException()
        {
            // Arrange
            var transport = ConnectorTriggerPayloadTransportTests.CreateTransport(
                ConnectorTriggerPayloadTransportTests.MetadataPayload,
                correlationId: "trace-xyz");
            var resolver = new StubTriggerConfigResolver(
                new InvalidOperationException(message: "boom"));

            // Act
            var exception = await Assert.ThrowsExactlyAsync<ConnectorTriggerConfigurationResolutionException>(
                async () => await ConnectorTriggerPayload
                    .ReadAsync<OneDriveForBusinessOnNewFilesTriggerPayload>(
                        transport,
                        ConnectorTriggerPayloadTransportTests.ExpectedIdentity,
                        resolver)
                    .ConfigureAwait(continueOnCapturedContext: false))
                .ConfigureAwait(continueOnCapturedContext: false);

            // Assert
            Assert.IsInstanceOfType<InvalidOperationException>(exception.InnerException);
            Assert.AreEqual("trace-xyz", exception.CorrelationId);
            Assert.AreEqual(ConnectorTriggerPayloadTransportTests.TriggerConfigName, exception.ResourceIdentity.TriggerConfigName);
        }

        [TestMethod]
        public async Task ReadAsync_Transport_IdentityMismatch_ExceptionContainsNoSecrets()
        {
            // Arrange
            const string secretAuthToken = "secret-auth-token";
            const string secretCallbackUrl = "https://secret-callback.example.com/run?code=very-secret";
            const string secretLockToken = "lock-token-sensitive-value";
            var sensitiveHeaders = new Dictionary<string, IEnumerable<string>>(StringComparer.OrdinalIgnoreCase)
            {
                ["Authorization"] = new[] { secretAuthToken },
                ["x-ms-callback-url"] = new[] { secretCallbackUrl },
                ["x-ms-lock-token"] = new[] { secretLockToken },
            };

            var transport = ConnectorTriggerPayloadTransportTests.CreateTransport(
                ConnectorTriggerPayloadTransportTests.MetadataPayload,
                extraHeaders: sensitiveHeaders);
            var resolver = new StubTriggerConfigResolver(
                new ConnectorNamespaceTriggerConfig(
                    ConnectorName: "sharepointonline",
                    OperationName: "WrongOperation"));

            // Act
            var exception = await Assert.ThrowsExactlyAsync<ConnectorTriggerIdentityMismatchException>(
                async () => await ConnectorTriggerPayload
                    .ReadAsync<OneDriveForBusinessOnNewFilesTriggerPayload>(
                        transport,
                        ConnectorTriggerPayloadTransportTests.ExpectedIdentity,
                        resolver)
                    .ConfigureAwait(continueOnCapturedContext: false))
                .ConfigureAwait(continueOnCapturedContext: false);

            // Assert
            Assert.IsFalse(exception.Message.Contains(secretAuthToken, StringComparison.Ordinal));
            Assert.IsFalse(exception.Message.Contains(secretCallbackUrl, StringComparison.Ordinal));
            Assert.IsFalse(exception.Message.Contains(secretLockToken, StringComparison.Ordinal));
        }

        [TestMethod]
        public async Task ReadAsync_Transport_CancelledResolver_ThrowsOperationCanceledException()
        {
            // Arrange
            var transport = ConnectorTriggerPayloadTransportTests.CreateTransport(
                ConnectorTriggerPayloadTransportTests.MetadataPayload);
            using var cancellationSource = new CancellationTokenSource();
            await cancellationSource.CancelAsync().ConfigureAwait(continueOnCapturedContext: false);
            var resolver = new StubTriggerConfigResolver(cancellationSource.Token);

            // Act & Assert
            try
            {
                await ConnectorTriggerPayload
                    .ReadAsync<OneDriveForBusinessOnNewFilesTriggerPayload>(
                        transport,
                        ConnectorTriggerPayloadTransportTests.ExpectedIdentity,
                        resolver,
                        cancellationToken: cancellationSource.Token)
                    .ConfigureAwait(continueOnCapturedContext: false);
                Assert.Fail("Expected an OperationCanceledException.");
            }
            catch (OperationCanceledException ex)
            {
                Assert.IsNotNull(ex);
            }
        }

        [TestMethod]
        public async Task ReadAsync_Stream_ExistingOverload_StillWorks()
        {
            // Arrange
            using var stream = new MemoryStream(
                Encoding.UTF8.GetBytes(ConnectorTriggerPayloadTransportTests.MetadataPayload));

            // Act
            var payload = await ConnectorTriggerPayload
                .ReadAsync<OneDriveForBusinessOnNewFilesTriggerPayload>(stream)
                .ConfigureAwait(continueOnCapturedContext: false);

            // Assert
            Assert.IsNotNull(payload);
            Assert.AreEqual("report.docx", payload.Body?.Value?[0].Name);
        }

        private static ConnectorTriggerTransport CreateTransportWithoutHeader(string headerName)
        {
            var headers = new Dictionary<string, IEnumerable<string>>
            {
                [ConnectorTriggerHeaderNames.SubscriptionId] = new[] { ConnectorTriggerPayloadTransportTests.SubscriptionId },
                [ConnectorTriggerHeaderNames.ResourceGroupName] = new[] { ConnectorTriggerPayloadTransportTests.ResourceGroupName },
                [ConnectorTriggerHeaderNames.ConnectorNamespaceName] = new[] { ConnectorTriggerPayloadTransportTests.ConnectorNamespaceName },
                [ConnectorTriggerHeaderNames.TriggerConfigName] = new[] { ConnectorTriggerPayloadTransportTests.TriggerConfigName },
            };

            headers.Remove(headerName);

            return new ConnectorTriggerTransport
            {
                Body = new MemoryStream(Encoding.UTF8.GetBytes(ConnectorTriggerPayloadTransportTests.MetadataPayload)),
                Headers = headers,
            };
        }

        private async Task AssertMissingHeaderThrowsAsync(string headerName)
        {
            // Arrange
            var transport = ConnectorTriggerPayloadTransportTests.CreateTransportWithoutHeader(headerName);
            var resolver = new StubTriggerConfigResolver(
                ConnectorTriggerPayloadTransportTests.MatchingTriggerConfig);

            // Act
            var exception = await Assert.ThrowsExactlyAsync<ConnectorTriggerResourceIdentityException>(
                async () => await ConnectorTriggerPayload
                    .ReadAsync<OneDriveForBusinessOnNewFilesTriggerPayload>(
                        transport,
                        ConnectorTriggerPayloadTransportTests.ExpectedIdentity,
                        resolver)
                    .ConfigureAwait(continueOnCapturedContext: false))
                .ConfigureAwait(continueOnCapturedContext: false);

            // Assert
            StringAssert.Contains(exception.Message, headerName);
            Assert.IsFalse(exception.PresentResourceIdentityHeaderNames.Contains(headerName));
        }

        private sealed class StubTriggerConfigResolver : IConnectorNamespaceTriggerConfigResolver
        {
            private readonly ConnectorNamespaceTriggerConfig? _triggerConfig;
            private readonly Exception? _exception;
            private readonly CancellationToken _cancelledToken;
            private readonly bool _throwCancellation;

            public StubTriggerConfigResolver(ConnectorNamespaceTriggerConfig triggerConfig)
            {
                this._triggerConfig = triggerConfig;
            }

            public StubTriggerConfigResolver(Exception exception)
            {
                this._exception = exception;
            }

            public StubTriggerConfigResolver(CancellationToken cancelledToken)
            {
                this._cancelledToken = cancelledToken;
                this._throwCancellation = true;
            }

            public ConnectorNamespaceTriggerConfigResourceIdentity? LastRequestedResourceIdentity { get; private set; }

            public ValueTask<ConnectorNamespaceTriggerConfig> GetTriggerConfigAsync(
                ConnectorNamespaceTriggerConfigResourceIdentity resourceIdentity,
                CancellationToken cancellationToken = default)
            {
                this.LastRequestedResourceIdentity = resourceIdentity;

                if (this._throwCancellation)
                {
                    return ValueTask.FromCanceled<ConnectorNamespaceTriggerConfig>(this._cancelledToken);
                }

                if (this._exception is not null)
                {
                    return ValueTask.FromException<ConnectorNamespaceTriggerConfig>(this._exception);
                }

                return ValueTask.FromResult(this._triggerConfig!);
            }
        }
    }
}
