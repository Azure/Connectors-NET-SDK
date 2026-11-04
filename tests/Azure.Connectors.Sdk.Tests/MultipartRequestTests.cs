//------------------------------------------------------------
// Copyright (c) Microsoft Corporation.  All rights reserved.
//------------------------------------------------------------

using System;
using System.Collections.Generic;
using System.Diagnostics.CodeAnalysis;
using System.Linq;
using System.Net;
using System.Net.Http;
using System.Net.Http.Headers;
using System.Text;
using System.Threading.Tasks;
using Azure.Connectors.Sdk.CloudmersiveConvert;
using Azure.Connectors.Sdk.Docuware;
using Azure.Connectors.Sdk.ServiceNow;
using Azure.Connectors.Sdk.ZohoSign;
using Microsoft.VisualStudio.TestTools.UnitTesting;

namespace Azure.Connectors.Sdk.Tests
{
    /// <summary>
    /// Verifies multipart bytes and headers sent by regenerated public clients.
    /// </summary>
    [TestClass]
    [TestCategory("CheckInTests")]
    [ExcludeFromCodeCoverage]
    public class MultipartRequestTests
    {
        /// <summary>
        /// Uploads bytes without JSON encoding and returns a binary response unchanged.
        /// </summary>
        [TestMethod]
        [Owner("swapnilnagar")]
        public async Task CloudmersiveUpload_PreservesFileAndBinaryResponse()
        {
            var content = new byte[] { 0, 127, 128, 255 };
            var (credential, options, capture) = ConnectorTestHelpers.CreateContentCapturingClientSetup(
                () => new HttpResponseMessage(HttpStatusCode.OK) { Content = new ByteArrayContent(content) });
            using var client = new CloudmersiveConvertClient(new Uri("https://example.test/connection"), credential, options);

            var response = await client
                .ConvertDocumentAutodetectToPdfAsync(
                    inputFileToPerformTheOperationOn: new MultipartFile(content, "example.docx", "application/custom"))
                .ConfigureAwait(continueOnCapturedContext: false);

            CollectionAssert.AreEqual(expected: content, actual: response);
            Assert.AreEqual(expected: HttpMethod.Post, actual: capture.Method);
            Assert.AreEqual(expected: "/connection/convert/autodetect/to/pdf", actual: capture.RequestUri!.AbsolutePath);
            var part = MultipartRequestTests.ParseParts(capture).Single();
            Assert.AreEqual(expected: "inputFile", actual: part.Name);
            Assert.AreEqual(expected: "example.docx", actual: part.FileName);
            Assert.AreEqual(expected: "application/custom", actual: part.ContentType);
            CollectionAssert.AreEqual(expected: content, actual: part.Content);
        }

        /// <summary>
        /// Preserves hidden form defaults and distinguishes omitted optional fields.
        /// </summary>
        /// <param name="includeOptionalFields">Whether to include the index and file.</param>
        [TestMethod]
        [Owner("swapnilnagar")]
        [DataRow(false)]
        [DataRow(true)]
        public async Task DocuwareUpload_PreservesDefaultsAndOptionalFields(bool includeOptionalFields)
        {
            var content = new byte[] { 0, 255 };
            var (credential, options, capture) = ConnectorTestHelpers.CreateContentCapturingClientSetup(
                () => new HttpResponseMessage(HttpStatusCode.OK) { Content = new StringContent("{\"DocumentId\":123}") });
            using var client = new DocuwareClient(new Uri("https://example.test/connection"), credential, options);

            var response = await client
                .StoreToFileCabinetAsync(
                    fileCabinet: "cabinet",
                    storeDialog: "dialog",
                    indexData: includeOptionalFields ? "{\"invoice\":\"123\"}" : null,
                    fileContents: includeOptionalFields ? new MultipartFile(content, "invoice.bin") : null)
                .ConfigureAwait(continueOnCapturedContext: false);

            Assert.AreEqual(expected: 123, actual: response.DocumentId);
            Assert.AreEqual(expected: "/connection/FileCabinets/cabinet/Documents", actual: capture.RequestUri!.AbsolutePath);
            Assert.AreEqual(expected: "?StoreDialogId=dialog", actual: capture.RequestUri.Query);
            var parts = MultipartRequestTests.ParseParts(capture).ToDictionary(part => part.Name, StringComparer.Ordinal);
            Assert.AreEqual(expected: includeOptionalFields ? 3 : 1, actual: parts.Count);
            CollectionAssert.AreEqual(expected: Encoding.UTF8.GetBytes("{}"), actual: parts["Default"].Content);
            if (includeOptionalFields)
            {
                CollectionAssert.AreEqual(expected: Encoding.UTF8.GetBytes("{\"invoice\":\"123\"}"), actual: parts["index"].Content);
                CollectionAssert.AreEqual(expected: content, actual: parts["file"].Content);
            }
        }

        /// <summary>
        /// Keeps the ServiceNow multipart route separate from its raw binary route.
        /// </summary>
        [TestMethod]
        [Owner("swapnilnagar")]
        public async Task ServiceNowUpload_SendsMultipartFileAndReadsAttachmentResponse()
        {
            var content = new byte[] { 0, 128, 255 };
            var (credential, options, capture) = ConnectorTestHelpers.CreateContentCapturingClientSetup(
                () => new HttpResponseMessage(HttpStatusCode.Created)
                {
                    Content = new StringContent("{\"result\":{\"sys_id\":\"attachment-123\"}}")
                });
            using var client = new ServiceNowClient(new Uri("https://example.test/connection"), credential, options);

            var response = await client
                .UploadAttachmentAsync(
                    attachmentContent: new MultipartFile(content, "attachment.bin"),
                    tableName: "incident",
                    tableId: "record-123")
                .ConfigureAwait(continueOnCapturedContext: false);

            Assert.AreEqual(expected: "attachment-123", actual: response.Result!.Value.GetProperty("sys_id").GetString());
            Assert.AreEqual(expected: "/connection/api/now/v1/attachment/upload", actual: capture.RequestUri!.AbsolutePath);
            var parts = MultipartRequestTests.ParseParts(capture).ToDictionary(part => part.Name, StringComparer.Ordinal);
            Assert.AreEqual(expected: 3, actual: parts.Count);
            var part = parts["attachment_content"];
            Assert.AreEqual(expected: "attachment.bin", actual: part.FileName);
            CollectionAssert.AreEqual(expected: content, actual: part.Content);
            CollectionAssert.AreEqual(expected: Encoding.UTF8.GetBytes("incident"), actual: parts["table_name"].Content);
            CollectionAssert.AreEqual(expected: Encoding.UTF8.GetBytes("record-123"), actual: parts["table_sys_id"].Content);
        }

        /// <summary>
        /// Accepts plain bytes using the default file metadata.
        /// </summary>
        [TestMethod]
        [Owner("swapnilnagar")]
        public async Task ZohoSignUpload_AcceptsBytesWithDefaultMetadata()
        {
            var content = new byte[] { 0, 255 };
            var (credential, options, capture) = ConnectorTestHelpers.CreateContentCapturingClientSetup(
                () => new HttpResponseMessage(HttpStatusCode.OK) { Content = new StringContent("{\"code\":0}") });
            using var client = new ZohoSignClient(new Uri("https://example.test/connection"), credential, options);

            var response = await client
                .CreateDocumentAsync(createADocument: content)
                .ConfigureAwait(continueOnCapturedContext: false);

            Assert.AreEqual(expected: 0, actual: response.Code);
            Assert.AreEqual(expected: "/connection/requests", actual: capture.RequestUri!.AbsolutePath);
            var part = MultipartRequestTests.ParseParts(capture).Single();
            Assert.AreEqual(expected: "file", actual: part.Name);
            Assert.AreEqual(expected: "blob", actual: part.FileName);
            Assert.AreEqual(expected: "application/octet-stream", actual: part.ContentType);
            CollectionAssert.AreEqual(expected: content, actual: part.Content);
        }

        /// <summary>
        /// Propagates multipart request errors without retrying unsafe operations by default.
        /// </summary>
        /// <param name="status">The backend error status.</param>
        [TestMethod]
        [Owner("swapnilnagar")]
        [DataRow(HttpStatusCode.BadRequest)]
        [DataRow(HttpStatusCode.ServiceUnavailable)]
        public async Task MultipartUpload_PropagatesErrorsWithoutUnsafeRetries(HttpStatusCode status)
        {
            var attempts = 0;
            var (credential, options, _) = ConnectorTestHelpers.CreateContentCapturingClientSetup(() =>
            {
                attempts++;
                return new HttpResponseMessage(status) { Content = new StringContent("Upload failed") };
            });
            options.Retry.MaxRetries = 3;
            using var client = new CloudmersiveConvertClient(new Uri("https://example.test/connection"), credential, options);

            var error = await Assert
                .ThrowsExactlyAsync<ConnectorException>(() => client.ConvertDocumentAutodetectToPdfAsync(new byte[] { 1 }))
                .ConfigureAwait(continueOnCapturedContext: false);

            Assert.AreEqual(expected: (int)status, actual: error.Status);
            Assert.AreEqual(expected: 1, actual: attempts);
        }

        /// <summary>
        /// Reads each generated MIME part using the boundary from the actual request header.
        /// </summary>
        /// <param name="capture">The request snapshot captured by the transport.</param>
        private static IReadOnlyList<MultipartPart> ParseParts(ConnectorTestHelpers.CapturedRequestContent capture)
        {
            Assert.AreEqual(expected: "multipart/form-data", actual: capture.ContentType);
            Assert.IsNotNull(capture.Body);
            Assert.IsNotNull(capture.ContentTypeHeader);
            var boundary = MediaTypeHeaderValue.Parse(capture.ContentTypeHeader).Parameters
                .Single(parameter => string.Equals(parameter.Name, "boundary", StringComparison.OrdinalIgnoreCase))
                .Value!.Trim('"');
            var sections = Encoding.Latin1.GetString(capture.Body).Split($"--{boundary}", StringSplitOptions.None);
            Assert.AreEqual(expected: string.Empty, actual: sections[0]);
            Assert.AreEqual(expected: "--\r\n", actual: sections[^1]);
            var parts = new List<MultipartPart>();
            foreach (var section in sections.Skip(1).SkipLast(1))
            {
                var separator = section.IndexOf("\r\n\r\n", StringComparison.Ordinal);
                Assert.IsTrue(condition: separator >= 0);
                var headers = section[2..separator].Split("\r\n", StringSplitOptions.None)
                    .Select(line => line.Split(':', 2))
                    .ToDictionary(header => header[0], header => header[1].Trim(), StringComparer.OrdinalIgnoreCase);
                var disposition = ContentDispositionHeaderValue.Parse(headers["Content-Disposition"]);
                parts.Add(new MultipartPart(
                    Name: disposition.Name!.Trim('"'),
                    FileName: disposition.FileName?.Trim('"'),
                    ContentType: MediaTypeHeaderValue.Parse(headers["Content-Type"]).MediaType!,
                    Content: Encoding.Latin1.GetBytes(section[(separator + 4)..^2])));
            }

            return parts;
        }

        /// <summary>
        /// A decoded request part used only by these wire-contract assertions.
        /// </summary>
        private sealed record MultipartPart(string Name, string? FileName, string ContentType, byte[] Content);
    }
}
