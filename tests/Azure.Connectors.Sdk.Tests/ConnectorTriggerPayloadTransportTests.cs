//------------------------------------------------------------
// Copyright (c) Microsoft Corporation.  All rights reserved.
//------------------------------------------------------------

using System;
using System.Collections.Generic;
using System.IO;
using System.Text;
using System.Threading.Tasks;
using Azure.Connectors.Sdk.OneDriveForBusiness.Models;
using Microsoft.VisualStudio.TestTools.UnitTesting;

namespace Azure.Connectors.Sdk.Tests
{
    /// <summary>
    /// Tests for the transport-aware overload of <see cref="ConnectorTriggerPayload.ReadAsync{TPayload}(ConnectorTriggerTransport, ConnectorTriggerIdentity, long, System.Threading.CancellationToken)"/>,
    /// covering identity validation, safe diagnostics, and backward compatibility.
    /// </summary>
    [TestClass]
    public class ConnectorTriggerPayloadTransportTests
    {
        private const string ExpectedConnectorName = "onedriveforbusiness";

        private const string ExpectedOperationName = "OnNewFilesV2";

        private const string MetadataPayload = """
            {"body":{"value":[{"Id":"01ABC","Name":"report.docx","Path":"/Documents/report.docx","Size":1234,"IsFolder":false}]}}
            """;

        // ------------------------------------------------------------------ //
        // Helpers                                                              //
        // ------------------------------------------------------------------ //

        private static ConnectorTriggerTransport CreateTransport(
            string body,
            string connectorName = ConnectorTriggerPayloadTransportTests.ExpectedConnectorName,
            string operationName = ConnectorTriggerPayloadTransportTests.ExpectedOperationName,
            string? correlationId = null,
            IDictionary<string, IEnumerable<string>>? extraHeaders = null)
        {
            var headers = new Dictionary<string, IEnumerable<string>>(StringComparer.OrdinalIgnoreCase)
            {
                [ConnectorTriggerHeaderNames.ConnectorName] = new[] { connectorName },
                [ConnectorTriggerHeaderNames.OperationName] = new[] { operationName },
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

        private static ConnectorTriggerIdentity ExpectedIdentity => new(
            ConnectorTriggerPayloadTransportTests.ExpectedConnectorName,
            ConnectorTriggerPayloadTransportTests.ExpectedOperationName);

        // ------------------------------------------------------------------ //
        // Happy path                                                           //
        // ------------------------------------------------------------------ //

        [TestMethod]
        public async Task ReadAsync_Transport_MatchingIdentity_ReturnsPayload()
        {
            // Arrange
            var transport = ConnectorTriggerPayloadTransportTests.CreateTransport(
                ConnectorTriggerPayloadTransportTests.MetadataPayload);

            // Act
            var payload = await ConnectorTriggerPayload
                .ReadAsync<OneDriveForBusinessOnNewFilesTriggerPayload>(
                    transport,
                    ConnectorTriggerPayloadTransportTests.ExpectedIdentity)
                .ConfigureAwait(continueOnCapturedContext: false);

            // Assert
            Assert.IsNotNull(payload);
            Assert.IsNotNull(payload.Body);
            Assert.IsNotNull(payload.Body.Value);
            Assert.AreEqual(1, payload.Body.Value.Count);
            Assert.AreEqual("report.docx", payload.Body.Value[0].Name);
        }

        // ------------------------------------------------------------------ //
        // Case-insensitive header-name and header-value matching              //
        // ------------------------------------------------------------------ //

        [TestMethod]
        public async Task ReadAsync_Transport_HeaderNameCaseInsensitive_Validates()
        {
            // Arrange — headers keyed with upper-case names in a plain Dictionary; the
            // ConnectorTriggerTransport default comparer normalises lookup.
            var headers = new Dictionary<string, IEnumerable<string>>(StringComparer.OrdinalIgnoreCase)
            {
                ["X-MS-GATEWAY-RESOURCE-NAME"] = new[] { ConnectorTriggerPayloadTransportTests.ExpectedConnectorName },
                ["X-MS-TRIGGER-NAME"] = new[] { ConnectorTriggerPayloadTransportTests.ExpectedOperationName },
            };

            var transport = new ConnectorTriggerTransport
            {
                Body = new MemoryStream(Encoding.UTF8.GetBytes(ConnectorTriggerPayloadTransportTests.MetadataPayload)),
                Headers = headers,
            };

            // Act — should not throw
            var payload = await ConnectorTriggerPayload
                .ReadAsync<OneDriveForBusinessOnNewFilesTriggerPayload>(
                    transport,
                    ConnectorTriggerPayloadTransportTests.ExpectedIdentity)
                .ConfigureAwait(continueOnCapturedContext: false);

            // Assert
            Assert.IsNotNull(payload);
        }

        [TestMethod]
        public async Task ReadAsync_Transport_HeaderValueCaseInsensitive_Validates()
        {
            // Arrange — connector name in uppercase; validation must be case-insensitive.
            var transport = ConnectorTriggerPayloadTransportTests.CreateTransport(
                ConnectorTriggerPayloadTransportTests.MetadataPayload,
                connectorName: ConnectorTriggerPayloadTransportTests.ExpectedConnectorName.ToUpperInvariant(),
                operationName: ConnectorTriggerPayloadTransportTests.ExpectedOperationName.ToUpperInvariant());

            // Act — should not throw
            var payload = await ConnectorTriggerPayload
                .ReadAsync<OneDriveForBusinessOnNewFilesTriggerPayload>(
                    transport,
                    ConnectorTriggerPayloadTransportTests.ExpectedIdentity)
                .ConfigureAwait(continueOnCapturedContext: false);

            // Assert
            Assert.IsNotNull(payload);
        }

        // ------------------------------------------------------------------ //
        // Missing identity headers                                            //
        // ------------------------------------------------------------------ //

        [TestMethod]
        public async Task ReadAsync_Transport_ConnectorNameHeaderMissing_ThrowsIdentityMismatch()
        {
            // Arrange — only the operation header is present.
            var headers = new Dictionary<string, IEnumerable<string>>(StringComparer.OrdinalIgnoreCase)
            {
                [ConnectorTriggerHeaderNames.OperationName] = new[] { ConnectorTriggerPayloadTransportTests.ExpectedOperationName },
            };

            var transport = new ConnectorTriggerTransport
            {
                Body = new MemoryStream(Encoding.UTF8.GetBytes(ConnectorTriggerPayloadTransportTests.MetadataPayload)),
                Headers = headers,
            };

            // Act & Assert
            var exception = await Assert.ThrowsExactlyAsync<ConnectorTriggerIdentityMismatchException>(
                async () => await ConnectorTriggerPayload
                    .ReadAsync<OneDriveForBusinessOnNewFilesTriggerPayload>(
                        transport,
                        ConnectorTriggerPayloadTransportTests.ExpectedIdentity)
                    .ConfigureAwait(continueOnCapturedContext: false))
                .ConfigureAwait(continueOnCapturedContext: false);

            Assert.IsNull(exception.ActualConnectorName);
            Assert.AreEqual(ConnectorTriggerPayloadTransportTests.ExpectedConnectorName, exception.ExpectedConnectorName);
            Assert.AreEqual(ConnectorTriggerPayloadTransportTests.ExpectedOperationName, exception.ExpectedOperationName);
            StringAssert.Contains(exception.Message, ConnectorTriggerHeaderNames.ConnectorName);
        }

        [TestMethod]
        public async Task ReadAsync_Transport_OperationNameHeaderMissing_ThrowsIdentityMismatch()
        {
            // Arrange — only the connector header is present.
            var headers = new Dictionary<string, IEnumerable<string>>(StringComparer.OrdinalIgnoreCase)
            {
                [ConnectorTriggerHeaderNames.ConnectorName] = new[] { ConnectorTriggerPayloadTransportTests.ExpectedConnectorName },
            };

            var transport = new ConnectorTriggerTransport
            {
                Body = new MemoryStream(Encoding.UTF8.GetBytes(ConnectorTriggerPayloadTransportTests.MetadataPayload)),
                Headers = headers,
            };

            // Act & Assert
            var exception = await Assert.ThrowsExactlyAsync<ConnectorTriggerIdentityMismatchException>(
                async () => await ConnectorTriggerPayload
                    .ReadAsync<OneDriveForBusinessOnNewFilesTriggerPayload>(
                        transport,
                        ConnectorTriggerPayloadTransportTests.ExpectedIdentity)
                    .ConfigureAwait(continueOnCapturedContext: false))
                .ConfigureAwait(continueOnCapturedContext: false);

            Assert.IsNull(exception.ActualOperationName);
            StringAssert.Contains(exception.Message, ConnectorTriggerHeaderNames.OperationName);
        }

        [TestMethod]
        public async Task ReadAsync_Transport_BothHeadersMissing_ThrowsIdentityMismatch()
        {
            // Arrange — no identity headers at all (e.g. routing mistake or non-Connector Namespace caller).
            var transport = new ConnectorTriggerTransport
            {
                Body = new MemoryStream(Encoding.UTF8.GetBytes(ConnectorTriggerPayloadTransportTests.MetadataPayload)),
                Headers = new Dictionary<string, IEnumerable<string>>(StringComparer.OrdinalIgnoreCase),
            };

            // Act & Assert
            var exception = await Assert.ThrowsExactlyAsync<ConnectorTriggerIdentityMismatchException>(
                async () => await ConnectorTriggerPayload
                    .ReadAsync<OneDriveForBusinessOnNewFilesTriggerPayload>(
                        transport,
                        ConnectorTriggerPayloadTransportTests.ExpectedIdentity)
                    .ConfigureAwait(continueOnCapturedContext: false))
                .ConfigureAwait(continueOnCapturedContext: false);

            Assert.IsNull(exception.ActualConnectorName);
            Assert.IsNull(exception.ActualOperationName);
            Assert.AreEqual(0, exception.PresentIdentityHeaderNames.Count);
            StringAssert.Contains(exception.Message, ConnectorTriggerHeaderNames.ConnectorName);
            StringAssert.Contains(exception.Message, ConnectorTriggerHeaderNames.OperationName);
        }

        // ------------------------------------------------------------------ //
        // Value mismatches                                                    //
        // ------------------------------------------------------------------ //

        [TestMethod]
        public async Task ReadAsync_Transport_ConnectorNameMismatch_ThrowsIdentityMismatch()
        {
            // Arrange — wrong connector, correct operation.
            var transport = ConnectorTriggerPayloadTransportTests.CreateTransport(
                ConnectorTriggerPayloadTransportTests.MetadataPayload,
                connectorName: "sharepoint",
                operationName: ConnectorTriggerPayloadTransportTests.ExpectedOperationName);

            // Act & Assert
            var exception = await Assert.ThrowsExactlyAsync<ConnectorTriggerIdentityMismatchException>(
                async () => await ConnectorTriggerPayload
                    .ReadAsync<OneDriveForBusinessOnNewFilesTriggerPayload>(
                        transport,
                        ConnectorTriggerPayloadTransportTests.ExpectedIdentity)
                    .ConfigureAwait(continueOnCapturedContext: false))
                .ConfigureAwait(continueOnCapturedContext: false);

            Assert.AreEqual("sharepoint", exception.ActualConnectorName);
            Assert.AreEqual(ConnectorTriggerPayloadTransportTests.ExpectedConnectorName, exception.ExpectedConnectorName);
            Assert.IsTrue(exception.PresentIdentityHeaderNames.Contains(ConnectorTriggerHeaderNames.ConnectorName));
            StringAssert.Contains(exception.Message, "sharepoint");
            StringAssert.Contains(exception.Message, ConnectorTriggerPayloadTransportTests.ExpectedConnectorName);
        }

        [TestMethod]
        public async Task ReadAsync_Transport_OperationNameMismatch_ThrowsIdentityMismatch()
        {
            // Arrange — correct connector, wrong operation.
            var transport = ConnectorTriggerPayloadTransportTests.CreateTransport(
                ConnectorTriggerPayloadTransportTests.MetadataPayload,
                connectorName: ConnectorTriggerPayloadTransportTests.ExpectedConnectorName,
                operationName: "OnUpdatedFilesV2");

            // Act & Assert
            var exception = await Assert.ThrowsExactlyAsync<ConnectorTriggerIdentityMismatchException>(
                async () => await ConnectorTriggerPayload
                    .ReadAsync<OneDriveForBusinessOnNewFilesTriggerPayload>(
                        transport,
                        ConnectorTriggerPayloadTransportTests.ExpectedIdentity)
                    .ConfigureAwait(continueOnCapturedContext: false))
                .ConfigureAwait(continueOnCapturedContext: false);

            Assert.AreEqual("OnUpdatedFilesV2", exception.ActualOperationName);
            Assert.AreEqual(ConnectorTriggerPayloadTransportTests.ExpectedOperationName, exception.ExpectedOperationName);
            StringAssert.Contains(exception.Message, "OnUpdatedFilesV2");
        }

        // ------------------------------------------------------------------ //
        // Correlation ID                                                      //
        // ------------------------------------------------------------------ //

        [TestMethod]
        public async Task ReadAsync_Transport_WithCorrelationId_IncludedInException()
        {
            // Arrange
            const string correlationId = "abc-123-def";
            var transport = ConnectorTriggerPayloadTransportTests.CreateTransport(
                ConnectorTriggerPayloadTransportTests.MetadataPayload,
                connectorName: "wrong-connector",
                correlationId: correlationId);

            // Act & Assert
            var exception = await Assert.ThrowsExactlyAsync<ConnectorTriggerIdentityMismatchException>(
                async () => await ConnectorTriggerPayload
                    .ReadAsync<OneDriveForBusinessOnNewFilesTriggerPayload>(
                        transport,
                        ConnectorTriggerPayloadTransportTests.ExpectedIdentity)
                    .ConfigureAwait(continueOnCapturedContext: false))
                .ConfigureAwait(continueOnCapturedContext: false);

            Assert.AreEqual(correlationId, exception.CorrelationId);
            StringAssert.Contains(exception.Message, correlationId);
        }

        [TestMethod]
        public async Task ReadAsync_Transport_MatchingIdentityWithCorrelationId_DoesNotThrow()
        {
            // Arrange
            var transport = ConnectorTriggerPayloadTransportTests.CreateTransport(
                ConnectorTriggerPayloadTransportTests.MetadataPayload,
                correlationId: "trace-xyz");

            // Act — correlation ID must not interfere with successful validation.
            var payload = await ConnectorTriggerPayload
                .ReadAsync<OneDriveForBusinessOnNewFilesTriggerPayload>(
                    transport,
                    ConnectorTriggerPayloadTransportTests.ExpectedIdentity)
                .ConfigureAwait(continueOnCapturedContext: false);

            // Assert
            Assert.IsNotNull(payload);
        }

        // ------------------------------------------------------------------ //
        // Safe diagnostics — no secrets in exception                          //
        // ------------------------------------------------------------------ //

        [TestMethod]
        public async Task ReadAsync_Transport_IdentityMismatch_ExceptionContainsNoSecrets()
        {
            // Arrange — add sensitive headers that must never appear in the exception.
            const string secretAuthToken = "******";
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
                connectorName: "wrong-connector",
                extraHeaders: sensitiveHeaders);

            // Act
            var exception = await Assert.ThrowsExactlyAsync<ConnectorTriggerIdentityMismatchException>(
                async () => await ConnectorTriggerPayload
                    .ReadAsync<OneDriveForBusinessOnNewFilesTriggerPayload>(
                        transport,
                        ConnectorTriggerPayloadTransportTests.ExpectedIdentity)
                    .ConfigureAwait(continueOnCapturedContext: false))
                .ConfigureAwait(continueOnCapturedContext: false);

            // Assert — sensitive header values must not appear in the exception message.
            Assert.IsFalse(exception.Message.Contains(secretAuthToken, StringComparison.Ordinal));
            Assert.IsFalse(exception.Message.Contains(secretCallbackUrl, StringComparison.Ordinal));
            Assert.IsFalse(exception.Message.Contains(secretLockToken, StringComparison.Ordinal));
        }

        // ------------------------------------------------------------------ //
        // PresentIdentityHeaderNames diagnostics                              //
        // ------------------------------------------------------------------ //

        [TestMethod]
        public async Task ReadAsync_Transport_BothHeadersPresent_PresentHeadersListedInException()
        {
            // Arrange — both headers present but values are wrong.
            var transport = ConnectorTriggerPayloadTransportTests.CreateTransport(
                ConnectorTriggerPayloadTransportTests.MetadataPayload,
                connectorName: "wrong-connector",
                operationName: "WrongOperation");

            // Act & Assert
            var exception = await Assert.ThrowsExactlyAsync<ConnectorTriggerIdentityMismatchException>(
                async () => await ConnectorTriggerPayload
                    .ReadAsync<OneDriveForBusinessOnNewFilesTriggerPayload>(
                        transport,
                        ConnectorTriggerPayloadTransportTests.ExpectedIdentity)
                    .ConfigureAwait(continueOnCapturedContext: false))
                .ConfigureAwait(continueOnCapturedContext: false);

            Assert.IsTrue(exception.PresentIdentityHeaderNames.Contains(ConnectorTriggerHeaderNames.ConnectorName));
            Assert.IsTrue(exception.PresentIdentityHeaderNames.Contains(ConnectorTriggerHeaderNames.OperationName));
        }

        // ------------------------------------------------------------------ //
        // Null argument guards                                                //
        // ------------------------------------------------------------------ //

        [TestMethod]
        public async Task ReadAsync_Transport_NullTransport_ThrowsArgumentNull()
        {
            // Act & Assert
            await Assert.ThrowsExactlyAsync<ArgumentNullException>(
                async () => await ConnectorTriggerPayload
                    .ReadAsync<OneDriveForBusinessOnNewFilesTriggerPayload>(
                        transport: null!,
                        expectedIdentity: ConnectorTriggerPayloadTransportTests.ExpectedIdentity)
                    .ConfigureAwait(continueOnCapturedContext: false))
                .ConfigureAwait(continueOnCapturedContext: false);
        }

        [TestMethod]
        public async Task ReadAsync_Transport_NullExpectedIdentity_ThrowsArgumentNull()
        {
            // Arrange
            var transport = ConnectorTriggerPayloadTransportTests.CreateTransport(
                ConnectorTriggerPayloadTransportTests.MetadataPayload);

            // Act & Assert
            await Assert.ThrowsExactlyAsync<ArgumentNullException>(
                async () => await ConnectorTriggerPayload
                    .ReadAsync<OneDriveForBusinessOnNewFilesTriggerPayload>(
                        transport,
                        expectedIdentity: null!)
                    .ConfigureAwait(continueOnCapturedContext: false))
                .ConfigureAwait(continueOnCapturedContext: false);
        }

        // ------------------------------------------------------------------ //
        // Default headers (empty) — all identity headers treated as absent   //
        // ------------------------------------------------------------------ //

        [TestMethod]
        public async Task ReadAsync_Transport_DefaultEmptyHeaders_ThrowsIdentityMismatch()
        {
            // Arrange — Transport with no Headers set (default empty dictionary).
            var transport = new ConnectorTriggerTransport
            {
                Body = new MemoryStream(Encoding.UTF8.GetBytes(ConnectorTriggerPayloadTransportTests.MetadataPayload)),
            };

            // Act & Assert — both identity headers absent.
            var exception = await Assert.ThrowsExactlyAsync<ConnectorTriggerIdentityMismatchException>(
                async () => await ConnectorTriggerPayload
                    .ReadAsync<OneDriveForBusinessOnNewFilesTriggerPayload>(
                        transport,
                        ConnectorTriggerPayloadTransportTests.ExpectedIdentity)
                    .ConfigureAwait(continueOnCapturedContext: false))
                .ConfigureAwait(continueOnCapturedContext: false);

            Assert.IsNull(exception.ActualConnectorName);
            Assert.IsNull(exception.ActualOperationName);
            Assert.IsNull(exception.CorrelationId);
        }

        // ------------------------------------------------------------------ //
        // Backward compatibility — existing Stream overloads still work       //
        // ------------------------------------------------------------------ //

        [TestMethod]
        public async Task ReadAsync_Stream_ExistingOverload_StillWorks()
        {
            // Arrange
            using var stream = new MemoryStream(
                Encoding.UTF8.GetBytes(ConnectorTriggerPayloadTransportTests.MetadataPayload));

            // Act — the body-only overload must remain unchanged.
            var payload = await ConnectorTriggerPayload
                .ReadAsync<OneDriveForBusinessOnNewFilesTriggerPayload>(stream)
                .ConfigureAwait(continueOnCapturedContext: false);

            // Assert
            Assert.IsNotNull(payload);
            Assert.AreEqual("report.docx", payload.Body?.Value?[0].Name);
        }
    }
}
