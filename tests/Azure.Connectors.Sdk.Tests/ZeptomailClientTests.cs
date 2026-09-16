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
using Azure.Connectors.Sdk.Zeptomail;
using Azure.Connectors.Sdk.Zeptomail.Models;
using global::Azure.Core;
using global::Azure.Core.Pipeline;
using Microsoft.VisualStudio.TestTools.UnitTesting;
using Moq;

namespace Azure.Connectors.Sdk.Tests
{
    [TestClass]
    public class ZeptomailClientTests
    {
        private static readonly Mock<TokenCredential> SharedMockCredential = CreateMockCredential();

        private sealed class RecordingHttpMessageHandler : HttpMessageHandler
        {
            private readonly Queue<HttpResponseMessage> _responses;

            public RecordingHttpMessageHandler(IEnumerable<HttpResponseMessage> responses)
            {
                this._responses = new Queue<HttpResponseMessage>(responses);
            }

            public List<HttpMethod> Methods { get; } = new List<HttpMethod>();

            public List<Uri> RequestUris { get; } = new List<Uri>();

            public List<string?> Bodies { get; } = new List<string?>();

            protected override async Task<HttpResponseMessage> SendAsync(
                HttpRequestMessage request,
                CancellationToken cancellationToken)
            {
                this.Methods.Add(request.Method);
                this.RequestUris.Add(request.RequestUri!);
                this.Bodies.Add(
                    request.Content == null
                        ? null
                        : await request.Content
                            .ReadAsStringAsync(cancellationToken)
                            .ConfigureAwait(continueOnCapturedContext: false));
                return this._responses.Dequeue();
            }
        }

        private static Mock<TokenCredential> CreateMockCredential()
        {
            var mock = new Mock<TokenCredential>();
            mock.Setup(credential => credential.GetTokenAsync(It.IsAny<TokenRequestContext>(), It.IsAny<CancellationToken>()))
                .ReturnsAsync(new AccessToken("mock-token", DateTimeOffset.UtcNow.AddHours(1)));
            return mock;
        }

        private static ZeptomailClient CreateMockedClient(HttpResponseMessage response)
        {
            return CreateMockedClient(new[] { response }, out _);
        }

        private static ZeptomailClient CreateMockedClient(
            IEnumerable<HttpResponseMessage> responses,
            out RecordingHttpMessageHandler handler)
        {
            handler = new RecordingHttpMessageHandler(responses);
            var options = new ConnectorClientOptions();
            options.Transport = new HttpClientTransport(new HttpClient(handler));
            options.Retry.MaxRetries = 0;
            return new ZeptomailClient(new Uri("https://test.azure.com/conn"), SharedMockCredential.Object, options);
        }

        [TestMethod]
        public void Constructor_WithValidUrl_ShouldCreateInstance()
        {
            using var client = new ZeptomailClient("https://test.azure.com/conn");
            Assert.AreEqual("zeptomail", client.ConnectorName);
        }

        [TestMethod]
        public void Constructor_WithNullUrl_ShouldThrowArgumentNullException()
        {
            Assert.ThrowsExactly<ArgumentNullException>(() => new ZeptomailClient((string)null!));
        }

        [TestMethod]
        public void Dispose_CalledTwice_ShouldNotThrow()
        {
            var client = new ZeptomailClient("https://test.azure.com/conn");
            client.Dispose();
            client.Dispose();
        }

        [TestMethod]
        public void SendTemplateMailInput_MixedMergeInfo_RoundTripsFixedAndAdditionalValues()
        {
            var mergeItem = JsonSerializer.SerializeToElement(new { key = "customer", value = "Ada", rank = 2 });
            var model = new SendTemplateMailInput
            {
                MergeInfo = new List<JsonElement?> { mergeItem }
            };

            var json = JsonSerializer.Serialize(model);
            var roundTripped = JsonSerializer.Deserialize<SendTemplateMailInput>(json);
            var item = roundTripped!.MergeInfo[0]!.Value;

            Assert.AreEqual("customer", item.GetProperty("key").GetString());
            Assert.AreEqual("Ada", item.GetProperty("value").GetString());
            Assert.AreEqual(2, item.GetProperty("rank").GetInt32());
        }

        [TestMethod]
        public void ReplyToAddress_BothRequestModels_PreserveCorrectedTypeAndWireName()
        {
            Assert.AreEqual(typeof(List<ReplyToAddress>), typeof(SendMailInput).GetProperty(nameof(SendMailInput.ReplyTo))!.PropertyType);
            Assert.AreEqual(typeof(List<ReplyToAddress>), typeof(SendTemplateMailInput).GetProperty(nameof(SendTemplateMailInput.ReplyTo))!.PropertyType);

            var model = new SendMailInput
            {
                ReplyTo = new List<ReplyToAddress>
                {
                    new ReplyToAddress { Address = "reply@example.com", Name = "Reply" }
                }
            };

            StringAssert.Contains(
                JsonSerializer.Serialize(model),
                "\"reply_to\":[{\"address\":\"reply@example.com\",\"name\":\"Reply\"}]",
                StringComparison.Ordinal);
        }

        [TestMethod]
        public async Task PublicActions_WithSuccessfulResponses_SendExpectedRequests()
        {
            using var getMailAgentResponse = CreateSuccessResponse();
            using var getProcessedEmailsResponse = CreateSuccessResponse();
            using var sendMailResponse = CreateSuccessResponse();
            using var sendTemplateMailResponse = CreateSuccessResponse();
            using var processedMailStatsResponse = CreateSuccessResponse();
            using var client = CreateMockedClient(
                new[]
                {
                    getMailAgentResponse,
                    getProcessedEmailsResponse,
                    sendMailResponse,
                    sendTemplateMailResponse,
                    processedMailStatsResponse
                },
                out var handler);
            var replyTo = new List<ReplyToAddress>
            {
                new ReplyToAddress { Address = "reply@example.com", Name = "Reply" }
            };
            var fromAddress = new FromAddress
            {
                Address = JsonSerializer.SerializeToElement(new { address = "sender@example.com" }),
                Name = "Sender"
            };
            var mergeItem = JsonSerializer.SerializeToElement(new { key = "customer", value = "Ada", rank = 2 });

            var mailAgents = await client
                .GetMailAgentAsync(CancellationToken.None)
                .ConfigureAwait(continueOnCapturedContext: false);
            var processedEmails = await client
                .GetProcessedEmailsAsync(
                    mailAgentKey: "agent+key",
                    subject: "Quarterly report",
                    from: "sender@example.com",
                    to: "recipient@example.com",
                    dateFrom: "2026-09-01/00:00",
                    dateTo: "2026-09-02/00:00",
                    requestId: "request/42",
                    showHardbounces: true,
                    showSoftbounces: false,
                    cancellationToken: CancellationToken.None)
                .ConfigureAwait(continueOnCapturedContext: false);
            var sentMail = await client
                .SendMailAsync(
                    input: new SendMailInput
                    {
                        MailAgent = "agent",
                        From = fromAddress,
                        ReplyTo = replyTo,
                        Subject = "Hello",
                        Body = "<p>Hello</p>"
                    },
                    cancellationToken: CancellationToken.None)
                .ConfigureAwait(continueOnCapturedContext: false);
            var sentTemplateMail = await client
                .SendTemplateMailAsync(
                    input: new SendTemplateMailInput
                    {
                        MailAgent = "agent",
                        MailTemplate = "template",
                        From = fromAddress,
                        ReplyTo = replyTo,
                        MergeInfo = new List<JsonElement?> { mergeItem }
                    },
                    cancellationToken: CancellationToken.None)
                .ConfigureAwait(continueOnCapturedContext: false);
            var mailStats = await client
                .ProcessedMailStatsAsync(
                    mailAgentName: "Agent Name",
                    fromDate: "2026-09-01/00:00",
                    toDate: "2026-09-02/00:00",
                    cancellationToken: CancellationToken.None)
                .ConfigureAwait(continueOnCapturedContext: false);

            Assert.IsNotNull(mailAgents);
            Assert.IsNotNull(processedEmails);
            Assert.IsNotNull(sentMail);
            Assert.IsNotNull(sentTemplateMail);
            Assert.IsNotNull(mailStats);
            CollectionAssert.AreEqual(
                new[] { HttpMethod.Get, HttpMethod.Get, HttpMethod.Post, HttpMethod.Post, HttpMethod.Get },
                handler.Methods);
            CollectionAssert.AreEqual(
                new[]
                {
                    "https://test.azure.com/conn/portal/v1.0/mailagents",
                    "https://test.azure.com/conn/v1.0/email?mailagent_key=agent%2Bkey&subject=Quarterly%20report&from=sender%40example.com&to=recipient%40example.com&date_from=2026-09-01%2F00%3A00&date_to=2026-09-02%2F00%3A00&request_id=request%2F42&is_hb=True&is_sb=False",
                    "https://test.azure.com/conn/v1.0/email",
                    "https://test.azure.com/conn/v1.0/email/template",
                    "https://test.azure.com/conn/v1.0/stats/email?mailagent=Agent%20Name&from_time=2026-09-01%2F00%3A00&to_time=2026-09-02%2F00%3A00"
                },
                handler.RequestUris.Select(requestUri => requestUri.AbsoluteUri).ToArray());

            using var sendMailDocument = JsonDocument.Parse(handler.Bodies[2]!);
            var sendMailBody = sendMailDocument.RootElement;
            Assert.AreEqual("agent", sendMailBody.GetProperty("mailagent_key").GetString());
            Assert.AreEqual("reply@example.com", sendMailBody.GetProperty("reply_to")[0].GetProperty("address").GetString());
            using var sendTemplateMailDocument = JsonDocument.Parse(handler.Bodies[3]!);
            var sendTemplateMailBody = sendTemplateMailDocument.RootElement;
            Assert.AreEqual("template", sendTemplateMailBody.GetProperty("mail_template_key").GetString());
            Assert.AreEqual(2, sendTemplateMailBody.GetProperty("merge_key_detail")[0].GetProperty("rank").GetInt32());
        }

        [TestMethod]
        public async Task SendTemplateMailAsync_WithErrorResponse_ThrowsConnectorException()
        {
            using var responseMessage = new HttpResponseMessage
            {
                StatusCode = HttpStatusCode.BadRequest,
                Content = new StringContent("{\"error\":\"Bad request\"}")
            };
            using var client = CreateMockedClient(responseMessage);

            await Assert.ThrowsExactlyAsync<ConnectorException>(() =>
                client.SendTemplateMailAsync(new SendTemplateMailInput(), CancellationToken.None))
                .ConfigureAwait(continueOnCapturedContext: false);
        }

        private static HttpResponseMessage CreateSuccessResponse()
        {
            return new HttpResponseMessage
            {
                StatusCode = HttpStatusCode.OK,
                Content = new StringContent("{}")
            };
        }
    }
}
