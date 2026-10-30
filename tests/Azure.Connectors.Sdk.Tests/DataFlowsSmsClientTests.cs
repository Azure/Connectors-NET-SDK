//------------------------------------------------------------
// Copyright (c) Microsoft Corporation.  All rights reserved.
//------------------------------------------------------------

using System;
using System.Net;
using System.Net.Http;
using System.Text.Json;
using System.Threading;
using System.Threading.Tasks;
using Azure.Connectors.Sdk.DataflowsSms;
using Azure.Connectors.Sdk.DataflowsSms.Models;
using global::Azure.Core;
using global::Azure.Core.Pipeline;
using Microsoft.VisualStudio.TestTools.UnitTesting;
using Moq;
using Moq.Protected;

namespace Azure.Connectors.Sdk.Tests
{
    [TestClass]
    public class DataFlowsSmsClientTests
    {
        [TestMethod]
        [Owner("daviburg")]
        public void Constructor_WithValidUrl_CreatesClient()
        {
            using var client = new DataflowsSmsClient("https://example.test/connection");
            Assert.IsNotNull(client);
        }

        [TestMethod]
        [Owner("daviburg")]
        public void Constructor_WithNullUrl_ThrowsArgumentNullException()
        {
            Assert.ThrowsExactly<ArgumentNullException>(() => new DataflowsSmsClient((string)null!));
        }

        [TestMethod]
        [Owner("daviburg")]
        public void Dispose_CalledTwice_DoesNotThrow()
        {
            var client = new DataflowsSmsClient("https://example.test/connection");
            client.Dispose();
            client.Dispose();
        }

        [TestMethod]
        [Owner("daviburg")]
        public async Task SendSmsAsync_WithTypedInput_SendsSwaggerBodyAndReadsResponse()
        {
            var requestCount = 0;
            using var client = CreateMockedClient(async (request, cancellationToken) =>
            {
                requestCount++;
                Assert.AreEqual(expected: HttpMethod.Post, actual: request.Method);
                Assert.AreEqual(expected: "/connection/sms/send", actual: request.RequestUri!.AbsolutePath);
                Assert.AreEqual(expected: "application/json", actual: request.Content!.Headers.ContentType!.MediaType);
                using var body = JsonDocument.Parse(await request.Content
                    .ReadAsStringAsync(cancellationToken)
                    .ConfigureAwait(continueOnCapturedContext: false));
                Assert.AreEqual(expected: "15555550123", actual: body.RootElement.GetProperty("recipient").GetString());
                Assert.AreEqual(expected: "Example", actual: body.RootElement.GetProperty("sender_id").GetString());
                Assert.AreEqual(expected: "Hello", actual: body.RootElement.GetProperty("message").GetString());
                Assert.AreEqual(expected: "plain", actual: body.RootElement.GetProperty("type").GetString());
                Assert.AreEqual(expected: "template-123", actual: body.RootElement.GetProperty("dlt_template_id").GetString());
                Assert.AreEqual(
                    expected: new DateTime(2026, 12, 1, 9, 0, 0, DateTimeKind.Utc),
                    actual: body.RootElement.GetProperty("schedule_time").GetDateTime());
                Assert.AreEqual(expected: 6, actual: body.RootElement.GetPropertyCount());
                return new HttpResponseMessage(HttpStatusCode.OK)
                {
                    Content = new StringContent("{\"status\":\"success\",\"data\":{\"uid\":\"message-123\"},\"message\":\"Accepted\"}")
                };
            });

            var response = await client
                .SendSmsAsync(DataflowsSmsModelFactory.SendSmsInput(
                    recipient: "15555550123",
                    senderId: "Example",
                    message: "Hello",
                    type: "plain",
                    scheduleTime: new DateTime(2026, 12, 1, 9, 0, 0, DateTimeKind.Utc),
                    dltTemplateId: "template-123"))
                .ConfigureAwait(continueOnCapturedContext: false);

            Assert.AreEqual(expected: 1, actual: requestCount);
            Assert.AreEqual(expected: Status.Success, actual: response.Status!.Value);
            Assert.AreEqual(expected: "Accepted", actual: response.Message);
            Assert.AreEqual(expected: "message-123", actual: response.Data!.Value.GetProperty("uid").GetString());
        }

        [TestMethod]
        [Owner("daviburg")]
        public async Task ListSmsAsync_WithPaging_UsesPageAndLimitQueryParameters()
        {
            var requestCount = 0;
            using var client = CreateMockedClient((request, cancellationToken) =>
            {
                requestCount++;
                Assert.AreEqual(expected: HttpMethod.Get, actual: request.Method);
                Assert.AreEqual(expected: "/connection/sms", actual: request.RequestUri!.AbsolutePath);
                Assert.AreEqual(expected: "?page=2&limit=25", actual: request.RequestUri.Query);
                return Task.FromResult(new HttpResponseMessage(HttpStatusCode.OK)
                {
                    Content = new StringContent("{\"status\":\"success\",\"data\":[{\"uid\":\"message-123\"}]}")
                });
            });

            var response = await client
                .ListSmsAsync(pageNumber: 2, pageSize: 25)
                .ConfigureAwait(continueOnCapturedContext: false);

            Assert.AreEqual(expected: 1, actual: requestCount);
            Assert.AreEqual(expected: "message-123", actual: response.Data!.Value[0].GetProperty("uid").GetString());
        }

        [TestMethod]
        [DataRow(false)]
        [DataRow(true)]
        [Owner("swapnilnagar")]
        public async Task SendSmsAsync_AmbiguousFailure_DoesNotRetryByDefault(bool transportFailure)
        {
            var attempts = 0;
            using var client = CreateMockedClient(
                sendAsync: (request, cancellationToken) =>
                {
                    attempts++;
                    Assert.AreEqual(expected: HttpMethod.Post, actual: request.Method);
                    if (transportFailure)
                    {
                        throw new HttpRequestException("Connection closed after SMS acceptance.");
                    }

                    return Task.FromResult(new HttpResponseMessage(HttpStatusCode.ServiceUnavailable)
                    {
                        Content = new StringContent("""{"message":"Response lost after SMS acceptance."}"""),
                    });
                },
                maxRetries: 3);

            if (transportFailure)
            {
                await Assert.ThrowsExactlyAsync<global::Azure.RequestFailedException>(
                    () => client.SendSmsAsync(new SendSmsInput { Recipient = "15555550123", Message = "Hello" }))
                    .ConfigureAwait(continueOnCapturedContext: false);
            }
            else
            {
                await Assert.ThrowsExactlyAsync<ConnectorException>(
                    () => client.SendSmsAsync(new SendSmsInput { Recipient = "15555550123", Message = "Hello" }))
                    .ConfigureAwait(continueOnCapturedContext: false);
            }

            Assert.AreEqual(expected: 1, actual: attempts);
            Assert.IsNull(typeof(DataflowsSmsClient).GetMethod("SendSMSGetAsync"));
            Assert.IsNotNull(typeof(DataflowsSmsClient).GetMethod("SendSmsGetAsync"));
        }

        [TestMethod]
        [Owner("daviburg")]
        public async Task SendSmsGetAsync_WithSuccess_PreservesGetRouteAndResponse()
        {
            var attempts = 0;
            using var client = CreateMockedClient((request, cancellationToken) =>
            {
                attempts++;
                Assert.AreEqual(expected: HttpMethod.Get, actual: request.Method);
                Assert.AreEqual(expected: "/connection/sms/send", actual: request.RequestUri!.AbsolutePath);
                StringAssert.Contains(value: request.RequestUri.Query, substring: "recipient=15555550123");
                return Task.FromResult(new HttpResponseMessage(HttpStatusCode.OK)
                {
                    Content = new StringContent("""{"status":"success","message":"Accepted"}"""),
                });
            });

            var response = await client.SendSmsGetAsync(recipient: "15555550123", senderId: "Example", message: "Hello")
                .ConfigureAwait(continueOnCapturedContext: false);

            Assert.AreEqual(expected: 1, actual: attempts);
            Assert.AreEqual(expected: "Accepted", actual: response.Message);
        }

        [TestMethod]
        [Owner("daviburg")]
        [DataRow(false, false)]
        [DataRow(true, false)]
        [DataRow(false, true)]
        [DataRow(true, true)]
        public async Task SendSmsGetAsync_AmbiguousFailure_OperationBlocklistPreventsRetries(bool transportFailure, bool retryUnsafeHttpMethods)
        {
            var attempts = 0;
            using var client = CreateMockedClient(
                sendAsync: (request, cancellationToken) =>
                {
                    attempts++;
                    Assert.AreEqual(expected: HttpMethod.Get, actual: request.Method);
                    if (transportFailure)
                    {
                        throw new HttpRequestException("Connection closed after GET SMS acceptance.");
                    }

                    return Task.FromResult(new HttpResponseMessage(HttpStatusCode.ServiceUnavailable)
                    {
                        Content = new StringContent("""{"message":"Response lost after SMS acceptance."}"""),
                    });
                },
                maxRetries: 3,
                retryUnsafeHttpMethods: retryUnsafeHttpMethods);

            if (transportFailure)
            {
                await Assert.ThrowsExactlyAsync<global::Azure.RequestFailedException>(
                    () => client.SendSmsGetAsync(recipient: "15555550123", senderId: "Example", message: "Hello"))
                    .ConfigureAwait(continueOnCapturedContext: false);
            }
            else
            {
                await Assert.ThrowsExactlyAsync<ConnectorException>(
                    () => client.SendSmsGetAsync(recipient: "15555550123", senderId: "Example", message: "Hello"))
                    .ConfigureAwait(continueOnCapturedContext: false);
            }

            Assert.AreEqual(expected: 1, actual: attempts);
        }

        [TestMethod]
        [Owner("daviburg")]
        public async Task ListSmsAsync_TransientResponse_RetainsNormalGetRetry()
        {
            var attempts = 0;
            using var client = CreateMockedClient(
                sendAsync: (request, cancellationToken) =>
                {
                    attempts++;
                    return Task.FromResult(new HttpResponseMessage(attempts == 1 ? HttpStatusCode.ServiceUnavailable : HttpStatusCode.OK)
                    {
                        Content = new StringContent("""{"status":"success","data":[]}"""),
                    });
                },
                maxRetries: 3);

            await client.ListSmsAsync().ConfigureAwait(continueOnCapturedContext: false);

            Assert.AreEqual(expected: 2, actual: attempts);
        }

        [TestMethod]
        [Owner("daviburg")]
        public async Task GetProfileAsync_WithErrorResponse_ThrowsConnectorException()
        {
            using var client = CreateMockedClient((request, cancellationToken) =>
                Task.FromResult(new HttpResponseMessage(HttpStatusCode.Unauthorized)
                {
                    Content = new StringContent("{\"message\":\"Invalid token\"}")
                }));

            var exception = await Assert
                .ThrowsExactlyAsync<ConnectorException>(() => client.GetProfileAsync())
                .ConfigureAwait(continueOnCapturedContext: false);

            Assert.AreEqual(expected: 401, actual: exception.Status);
            StringAssert.Contains(value: exception.ResponseBody, substring: "Invalid token");
        }

        [TestMethod]
        [Owner("daviburg")]
        public void SmsResponse_WithUnknownStatus_PreservesWireValue()
        {
            const string responseJson = "{\"status\":\"queued\",\"message\":\"Scheduled\",\"data\":{\"uid\":\"message-123\"}}";
            var response = JsonSerializer.Deserialize<SmsResponse>(responseJson)!;
            var roundTrip = JsonSerializer.Deserialize<SmsResponse>(JsonSerializer.Serialize(response))!;

            Assert.AreEqual(expected: "queued", actual: roundTrip.Status!.Value.ToString());
            Assert.AreEqual(expected: "Scheduled", actual: roundTrip.Message);
            Assert.AreEqual(expected: "message-123", actual: roundTrip.Data!.Value.GetProperty("uid").GetString());
        }

        private static DataflowsSmsClient CreateMockedClient(
            Func<HttpRequestMessage, CancellationToken, Task<HttpResponseMessage>> sendAsync,
            int maxRetries = 0,
            bool retryUnsafeHttpMethods = false)
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
                Transport = new HttpClientTransport(new HttpClient(handler.Object)),
                RetryUnsafeHttpMethods = retryUnsafeHttpMethods,
            };
            options.Retry.MaxRetries = maxRetries;
            options.Retry.Delay = TimeSpan.Zero;
            return new DataflowsSmsClient(
                connectionRuntimeUrl: new Uri("https://example.test/connection"),
                credential: credential.Object,
                options: options);
        }
    }
}
