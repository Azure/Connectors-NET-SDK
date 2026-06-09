//------------------------------------------------------------
// Copyright (c) Microsoft Corporation.  All rights reserved.
//------------------------------------------------------------

using System;
using System.IO;
using System.Text;
using System.Text.Json;
using System.Threading.Tasks;
using Azure.Connectors.Sdk.OneDriveForBusiness.Models;
using Microsoft.VisualStudio.TestTools.UnitTesting;

namespace Azure.Connectors.Sdk.Tests
{
    /// <summary>
    /// Tests for <see cref="ConnectorTriggerPayload"/> — reading metadata and binary-content
    /// trigger callbacks into typed payloads and file bytes.
    /// </summary>
    [TestClass]
    public class ConnectorTriggerPayloadTests
    {
        private const string MetadataPascalCasePayload = """
            {"body":{"value":[{"Id":"01ABC","Name":"report.docx","Path":"/Documents/report.docx","Size":1234,"IsFolder":false}]}}
            """;

        private const string MetadataCamelCasePayload = """
            {"body":{"value":[{"id":"01ABC","name":"report.docx","path":"/Documents/report.docx","size":1234,"isFolder":false}]}}
            """;

        [TestMethod]
        public void Read_MetadataPascalCase_PopulatesItem()
        {
            // Arrange & Act
            var payload = ConnectorTriggerPayload.Read<OneDriveForBusinessOnNewFilesTriggerPayload>(
                ConnectorTriggerPayloadTests.MetadataPascalCasePayload);

            // Assert
            Assert.IsNotNull(payload);
            Assert.IsNotNull(payload.Body);
            Assert.IsNotNull(payload.Body.Value);
            Assert.AreEqual(1, payload.Body.Value.Count);
            Assert.AreEqual("01ABC", payload.Body.Value[0].Id);
            Assert.AreEqual("report.docx", payload.Body.Value[0].Name);
        }

        [TestMethod]
        public void Read_MetadataCamelCase_PopulatesItemCaseInsensitively()
        {
            // Arrange & Act
            var payload = ConnectorTriggerPayload.Read<OneDriveForBusinessOnNewFilesTriggerPayload>(
                ConnectorTriggerPayloadTests.MetadataCamelCasePayload);

            // Assert — camelCase wire fields must still bind (regression guard against all-null items).
            Assert.IsNotNull(payload);
            Assert.IsNotNull(payload.Body);
            Assert.IsNotNull(payload.Body.Value);
            Assert.AreEqual(1, payload.Body.Value.Count);
            Assert.AreEqual("01ABC", payload.Body.Value[0].Id);
            Assert.AreEqual("report.docx", payload.Body.Value[0].Name);
        }

        [TestMethod]
        public async Task ReadAsync_MetadataStream_PopulatesItem()
        {
            // Arrange
            using var stream = new MemoryStream(
                Encoding.UTF8.GetBytes(ConnectorTriggerPayloadTests.MetadataPascalCasePayload));

            // Act
            var payload = await ConnectorTriggerPayload
                .ReadAsync<OneDriveForBusinessOnNewFilesTriggerPayload>(stream)
                .ConfigureAwait(continueOnCapturedContext: false);

            // Assert
            Assert.IsNotNull(payload);
            Assert.IsNotNull(payload.Body);
            Assert.IsNotNull(payload.Body.Value);
            Assert.AreEqual(1, payload.Body.Value.Count);
            Assert.AreEqual("report.docx", payload.Body.Value[0].Name);
        }

        [TestMethod]
        public void Read_NullJson_Throws()
        {
            // Arrange, Act & Assert
            Assert.ThrowsExactly<ArgumentNullException>(
                () => ConnectorTriggerPayload.Read<OneDriveForBusinessOnNewFilesTriggerPayload>(null!));
        }

        [TestMethod]
        public void Read_BinaryStringBody_ThrowsActionableJsonException()
        {
            // Arrange — a binary-content trigger delivers {"body":"<base64>"}.
            string base64 = Convert.ToBase64String(Encoding.UTF8.GetBytes("hello"));
            string payload = $$"""{"body":"{{base64}}"}""";

            // Act
            var exception = Assert.ThrowsExactly<JsonException>(
                () => ConnectorTriggerPayload.Read<OneDriveForBusinessOnNewFilesTriggerPayload>(payload));

            // Assert — the message must steer the developer to the binary-content helper.
            StringAssert.Contains(exception.Message, nameof(ConnectorTriggerPayload.TryReadBinaryContent));
            StringAssert.Contains(exception.Message, "binary-content trigger");
        }

        [TestMethod]
        public void TryReadBinaryContent_Base64StringBody_DecodesBytes()
        {
            // Arrange
            byte[] expected = Encoding.UTF8.GetBytes("hello from trigger test");
            string base64 = Convert.ToBase64String(expected);
            string payload = $$"""{"body":"{{base64}}"}""";

            // Act
            bool result = ConnectorTriggerPayload.TryReadBinaryContent(payload, out byte[] content);

            // Assert
            Assert.IsTrue(result);
            CollectionAssert.AreEqual(expected, content);
        }

        [TestMethod]
        public void TryReadBinaryContent_EmptyStringBody_ReturnsTrueWithEmptyContent()
        {
            // Arrange
            const string payload = """{"body":""}""";

            // Act
            bool result = ConnectorTriggerPayload.TryReadBinaryContent(payload, out byte[] content);

            // Assert
            Assert.IsTrue(result);
            Assert.AreEqual(0, content.Length);
        }

        [TestMethod]
        public void TryReadBinaryContent_MetadataObjectBody_ReturnsFalse()
        {
            // Arrange — a metadata callback has an object body, not a string.
            // Act
            bool result = ConnectorTriggerPayload.TryReadBinaryContent(
                ConnectorTriggerPayloadTests.MetadataPascalCasePayload,
                out byte[] content);

            // Assert
            Assert.IsFalse(result);
            Assert.AreEqual(0, content.Length);
        }

        [TestMethod]
        public void TryReadBinaryContent_NonBase64StringBody_ReturnsFalse()
        {
            // Arrange — a string body that is not valid base64.
            const string payload = """{"body":"not valid base64 !!!"}""";

            // Act
            bool result = ConnectorTriggerPayload.TryReadBinaryContent(payload, out byte[] content);

            // Assert
            Assert.IsFalse(result);
            Assert.AreEqual(0, content.Length);
        }

        [TestMethod]
        public void TryReadBinaryContent_MalformedJson_ReturnsFalse()
        {
            // Arrange — a Try* API must not throw on invalid JSON.
            const string payload = "this is not json {";

            // Act
            bool result = ConnectorTriggerPayload.TryReadBinaryContent(payload, out byte[] content);

            // Assert
            Assert.IsFalse(result);
            Assert.AreEqual(0, content.Length);
        }

        [TestMethod]
        public async Task ReadAsync_BodyExceedsLimit_ThrowsInvalidOperation()
        {
            // Arrange — a payload larger than the (tiny) configured limit.
            using var stream = new MemoryStream(
                Encoding.UTF8.GetBytes(ConnectorTriggerPayloadTests.MetadataPascalCasePayload));

            // Act & Assert
            await Assert.ThrowsExactlyAsync<InvalidOperationException>(
                async () => await ConnectorTriggerPayload
                    .ReadAsync<OneDriveForBusinessOnNewFilesTriggerPayload>(stream, maxBodySizeBytes: 8)
                    .ConfigureAwait(continueOnCapturedContext: false))
                .ConfigureAwait(continueOnCapturedContext: false);
        }

        [TestMethod]
        public async Task ReadBinaryContentAsync_DoesNotCloseCallerStream()
        {
            // Arrange
            byte[] expected = Encoding.UTF8.GetBytes("keep me open");
            string base64 = Convert.ToBase64String(expected);
            using var stream = new MemoryStream(Encoding.UTF8.GetBytes($$"""{"body":"{{base64}}"}"""));

            // Act
            byte[]? content = await ConnectorTriggerPayload
                .ReadBinaryContentAsync(stream)
                .ConfigureAwait(continueOnCapturedContext: false);

            // Assert — the helper must not take ownership of the caller's stream.
            Assert.IsNotNull(content);
            Assert.IsTrue(stream.CanRead, "The caller-owned stream must remain open after reading.");
        }

        [TestMethod]
        public async Task ReadBinaryContentAsync_MetadataStream_ReturnsNull()
        {
            // Arrange
            using var stream = new MemoryStream(
                Encoding.UTF8.GetBytes(ConnectorTriggerPayloadTests.MetadataPascalCasePayload));

            // Act
            byte[]? content = await ConnectorTriggerPayload
                .ReadBinaryContentAsync(stream)
                .ConfigureAwait(continueOnCapturedContext: false);

            // Assert — an object body is not binary content.
            Assert.IsNull(content);
        }
    }
}
