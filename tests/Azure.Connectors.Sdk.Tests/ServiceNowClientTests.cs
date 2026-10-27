//------------------------------------------------------------
// Copyright (c) Microsoft Corporation.  All rights reserved.
//------------------------------------------------------------

using System;
using System.Net;
using System.Net.Http;
using System.Text.Json;
using System.Threading;
using System.Threading.Tasks;
using Azure.Connectors.Sdk.ServiceNow;
using Azure.Connectors.Sdk.ServiceNow.Models;
using global::Azure.Core;
using global::Azure.Core.Pipeline;
using Microsoft.VisualStudio.TestTools.UnitTesting;
using Moq;
using Moq.Protected;

namespace Azure.Connectors.Sdk.Tests
{
    [TestClass]
    public class ServiceNowClientTests
    {
        [TestMethod]
        [Owner("daviburg")]
        public void Constructor_WithValidUrl_CreatesClient()
        {
            using var client = new ServiceNowClient("https://example.test/connection");
            Assert.IsNotNull(client);
        }

        [TestMethod]
        [Owner("daviburg")]
        public void Constructor_WithNullUrl_ThrowsArgumentNullException()
        {
            Assert.ThrowsExactly<ArgumentNullException>(() => new ServiceNowClient((string)null!));
        }

        [TestMethod]
        [Owner("daviburg")]
        public void Dispose_CalledTwice_DoesNotThrow()
        {
            var client = new ServiceNowClient("https://example.test/connection");
            client.Dispose();
            client.Dispose();
        }

        [TestMethod]
        [Owner("daviburg")]
        public async Task CreateRecordAsync_WithDynamicFields_SendsFlatSwaggerBodyAndReadsResponse()
        {
            var requestCount = 0;
            using var client = CreateMockedClient(async (request, cancellationToken) =>
            {
                requestCount++;
                Assert.AreEqual(expected: HttpMethod.Post, actual: request.Method);
                Assert.AreEqual(expected: "/connection/api/now/v2/table/incident", actual: request.RequestUri!.AbsolutePath);
                using var body = JsonDocument.Parse(await request.Content!
                    .ReadAsStringAsync(cancellationToken)
                    .ConfigureAwait(continueOnCapturedContext: false));
                Assert.AreEqual(expected: "Example incident", actual: body.RootElement.GetProperty("short_description").GetString());
                Assert.AreEqual(expected: 1, actual: body.RootElement.GetProperty("priority").GetInt32());
                Assert.AreEqual(expected: "external-123", actual: body.RootElement.GetProperty("u_external_id").GetString());
                Assert.AreEqual(expected: 3, actual: body.RootElement.GetPropertyCount());
                return new HttpResponseMessage(HttpStatusCode.Created)
                {
                    Content = new StringContent("{\"result\":{\"sys_id\":\"record-123\",\"number\":\"INC0000001\"}}")
                };
            });

            var input = new RecordBody();
            input.AdditionalProperties.Add("short_description", JsonSerializer.SerializeToElement("Example incident"));
            input.AdditionalProperties.Add("priority", JsonSerializer.SerializeToElement(1));
            input.AdditionalProperties.Add("u_external_id", JsonSerializer.SerializeToElement("external-123"));
            var response = await client
                .CreateRecordAsync(recordType: "incident", input: input)
                .ConfigureAwait(continueOnCapturedContext: false);

            Assert.AreEqual(expected: 1, actual: requestCount);
            Assert.AreEqual(expected: "record-123", actual: response.Result!.Value.GetProperty("sys_id").GetString());
            Assert.AreEqual(expected: "INC0000001", actual: response.Result!.Value.GetProperty("number").GetString());
        }

        [TestMethod]
        [Owner("daviburg")]
        public async Task UploadAttachmentFileAsync_WithBinaryInput_SendsRawBytesAndEncodedFileName()
        {
            var fileBytes = new byte[] { 0, 127, 128, 255 };
            var requestCount = 0;
            using var client = CreateMockedClient(async (request, cancellationToken) =>
            {
                requestCount++;
                Assert.AreEqual(expected: HttpMethod.Post, actual: request.Method);
                Assert.AreEqual(expected: "/connection/api/now/v1/attachment/file", actual: request.RequestUri!.AbsolutePath);
                Assert.AreEqual(
                    expected: "?table_name=incident&table_sys_id=record-123&file_name=sample%20%26%20test.bin",
                    actual: request.RequestUri.Query);
                Assert.AreEqual(expected: "application/octet-stream", actual: request.Content!.Headers.ContentType!.MediaType);
                var actualBytes = await request.Content
                    .ReadAsByteArrayAsync(cancellationToken)
                    .ConfigureAwait(continueOnCapturedContext: false);
                CollectionAssert.AreEqual(expected: fileBytes, actual: actualBytes);
                return new HttpResponseMessage(HttpStatusCode.Created)
                {
                    Content = new StringContent("{\"result\":{\"sys_id\":\"attachment-123\",\"file_name\":\"sample & test.bin\"}}")
                };
            });

            var response = await client
                .UploadAttachmentFileAsync(
                    input: fileBytes,
                    tableName: "incident",
                    sysId: "record-123",
                    fileName: "sample & test.bin")
                .ConfigureAwait(continueOnCapturedContext: false);

            Assert.AreEqual(expected: 1, actual: requestCount);
            Assert.AreEqual(expected: "attachment-123", actual: response.Result!.Value.GetProperty("sys_id").GetString());
        }

        [TestMethod]
        [Owner("daviburg")]
        public async Task GetRecordsAsync_WithQueryAndPaging_UsesSwaggerParametersAndReadsRecords()
        {
            var requestCount = 0;
            using var client = CreateMockedClient((request, cancellationToken) =>
            {
                requestCount++;
                Assert.AreEqual(expected: HttpMethod.Get, actual: request.Method);
                Assert.AreEqual(expected: "/connection/api/now/v2/table/incident", actual: request.RequestUri!.AbsolutePath);
                Assert.AreEqual(
                    expected: "?sysparm_query=active%3Dtrue%5Epriority%3D1&sysparm_limit=20&sysparm_offset=40&sysparm_fields=number%2Cshort_description",
                    actual: request.RequestUri.Query);
                return Task.FromResult(new HttpResponseMessage(HttpStatusCode.OK)
                {
                    Content = new StringContent("{\"result\":[{\"sys_id\":\"record-123\",\"number\":\"INC0000001\"}]}")
                });
            });

            var response = await client
                .GetRecordsAsync(
                    recordType: "incident",
                    query: "active=true^priority=1",
                    limit: 20,
                    offset: 40,
                    fields: "number,short_description")
                .ConfigureAwait(continueOnCapturedContext: false);

            Assert.AreEqual(expected: 1, actual: requestCount);
            Assert.AreEqual(expected: 1, actual: response.Result.Count);
            Assert.AreEqual(expected: "record-123", actual: response.Result[0]!.Value.GetProperty("sys_id").GetString());
        }

        [TestMethod]
        [Owner("daviburg")]
        public async Task GetRecordAsync_WithErrorResponse_ThrowsConnectorException()
        {
            using var client = CreateMockedClient((request, cancellationToken) =>
                Task.FromResult(new HttpResponseMessage(HttpStatusCode.Forbidden)
                {
                    Content = new StringContent("{\"error\":{\"message\":\"Access denied\",\"detail\":\"No permission\"}}")
                }));

            var exception = await Assert
                .ThrowsExactlyAsync<ConnectorException>(() => client.GetRecordAsync(recordType: "incident", systemId: "record-123"))
                .ConfigureAwait(continueOnCapturedContext: false);

            Assert.AreEqual(expected: 403, actual: exception.Status);
            StringAssert.Contains(value: exception.ResponseBody, substring: "Access denied");
        }

        [TestMethod]
        [Owner("daviburg")]
        public void RecordBody_WithDynamicFields_RoundTripsUnknownWireProperties()
        {
            const string recordJson = "{\"short_description\":\"Example incident\",\"priority\":1,\"u_custom\":{\"enabled\":true}}";
            var record = JsonSerializer.Deserialize<RecordBody>(recordJson)!;
            using var roundTrip = JsonDocument.Parse(JsonSerializer.Serialize(record));

            Assert.AreEqual(expected: 3, actual: record.AdditionalProperties.Count);
            Assert.AreEqual(expected: "Example incident", actual: roundTrip.RootElement.GetProperty("short_description").GetString());
            Assert.AreEqual(expected: 1, actual: roundTrip.RootElement.GetProperty("priority").GetInt32());
            Assert.IsTrue(roundTrip.RootElement.GetProperty("u_custom").GetProperty("enabled").GetBoolean());
            Assert.AreEqual(expected: 3, actual: roundTrip.RootElement.GetPropertyCount());
        }

        private static ServiceNowClient CreateMockedClient(Func<HttpRequestMessage, CancellationToken, Task<HttpResponseMessage>> sendAsync)
        {
            var credential = new Mock<TokenCredential>();
            credential.Setup(tokenCredential => tokenCredential.GetTokenAsync(It.IsAny<TokenRequestContext>(), It.IsAny<CancellationToken>()))
                .ReturnsAsync(new AccessToken("synthetic-token", DateTimeOffset.UtcNow.AddHours(1)));
            var handler = new Mock<HttpMessageHandler>();
            handler.Protected()
                .Setup<Task<HttpResponseMessage>>("SendAsync", ItExpr.IsAny<HttpRequestMessage>(), ItExpr.IsAny<CancellationToken>())
                .Returns(sendAsync);
            var options = new ConnectorClientOptions
            {
                Transport = new HttpClientTransport(new HttpClient(handler.Object))
            };
            options.Retry.MaxRetries = 0;
            return new ServiceNowClient(
                connectionRuntimeUrl: new Uri("https://example.test/connection"),
                credential: credential.Object,
                options: options);
        }
    }
}
