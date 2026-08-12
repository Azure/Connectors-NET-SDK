//------------------------------------------------------------
// Copyright (c) Microsoft Corporation.  All rights reserved.
//------------------------------------------------------------

using System.Text.Json;
using Microsoft.VisualStudio.TestTools.UnitTesting;
using EtsyPaymentAdjustment = Azure.Connectors.Sdk.Etsy.Models.PaymentAdjustment;
using GitHubPullRequest = Azure.Connectors.Sdk.GitHub.Models.PullRequest;
using Office365MailTips = Azure.Connectors.Sdk.Office365.Models.MailTipsClientReceive;
using PlumsailWebhookData = Azure.Connectors.Sdk.Plumsail.Models.AddPowerAutomateWebhookData;
using SigningHubUploadDocument = Azure.Connectors.Sdk.SigningHub.Models.UploadDocument;
using TwitterOriginalTweet = Azure.Connectors.Sdk.Twitter.Models.OriginalTweetModel;
using TwitterTweet = Azure.Connectors.Sdk.Twitter.Models.TweetModel;
using WordPressPost = Azure.Connectors.Sdk.WordPress.Models.PostModel;
using WordPressPostResponse = Azure.Connectors.Sdk.WordPress.Models.PostResponse;

namespace Azure.Connectors.Sdk.Tests
{
    [TestClass]
    public class GeneratedPropertyCollisionTests
    {
        [TestMethod]
        public void EtsyPaymentAdjustment_CollidingAdjustmentFields_PreservesBothWireValues()
        {
            var model = new EtsyPaymentAdjustment
            {
                ShopTotalAdjustmentAmount = 10,
                BuyerTotalAdjustmentAmount = 20
            };

            var json = JsonSerializer.Serialize(model);
            var roundTripped = JsonSerializer.Deserialize<EtsyPaymentAdjustment>(json);

            Assert.IsNotNull(roundTripped);
            Assert.AreEqual(expected: 10, actual: roundTripped.ShopTotalAdjustmentAmount);
            Assert.AreEqual(expected: 20, actual: roundTripped.BuyerTotalAdjustmentAmount);
            StringAssert.Contains(json, "\"shop_total_adjustment_amount\":10");
            StringAssert.Contains(json, "\"buyer_total_adjustment_amount\":20");
        }

        [TestMethod]
        public void GitHubPullRequest_CollidingUrlFields_PreservesBothWireValues()
        {
            var model = new GitHubPullRequest
            {
                DiffUrl = "https://example.test/diff",
                PullRequestDiffUrl = "https://example.test/comments"
            };

            var json = JsonSerializer.Serialize(model);
            var roundTripped = JsonSerializer.Deserialize<GitHubPullRequest>(json);

            Assert.IsNotNull(roundTripped);
            Assert.AreEqual(expected: model.DiffUrl, actual: roundTripped.DiffUrl);
            Assert.AreEqual(expected: model.PullRequestDiffUrl, actual: roundTripped.PullRequestDiffUrl);
            StringAssert.Contains(json, "\"diff_url\":\"https://example.test/diff\"");
            StringAssert.Contains(json, "\"comments_url\":\"https://example.test/comments\"");
        }

        [TestMethod]
        public void Office365MailTips_CollidingSummaryFields_PreservesBothWireValues()
        {
            var model = new Office365MailTips
            {
                ExternalMemberCount = 4,
                IsModerated = true
            };

            var json = JsonSerializer.Serialize(model);
            var roundTripped = JsonSerializer.Deserialize<Office365MailTips>(json);

            Assert.IsNotNull(roundTripped);
            Assert.AreEqual(expected: 4, actual: roundTripped.ExternalMemberCount);
            Assert.AreEqual(expected: true, actual: roundTripped.IsModerated);
            StringAssert.Contains(json, "\"externalMemberCount\":4");
            StringAssert.Contains(json, "\"isModerated\":true");
        }

        [TestMethod]
        public void PlumsailWebhookData_CollidingSummaryFields_PreservesBothWireValues()
        {
            var model = new PlumsailWebhookData
            {
                ProcessId = "process-1",
                ProcessName = "https://example.test/hook"
            };

            var json = JsonSerializer.Serialize(model);
            var roundTripped = JsonSerializer.Deserialize<PlumsailWebhookData>(json);

            Assert.IsNotNull(roundTripped);
            Assert.AreEqual(expected: model.ProcessId, actual: roundTripped.ProcessId);
            Assert.AreEqual(expected: model.ProcessName, actual: roundTripped.ProcessName);
            StringAssert.Contains(json, "\"processId\":\"process-1\"");
            StringAssert.Contains(json, "\"hookUrl\":\"https://example.test/hook\"");
        }

        [TestMethod]
        public void SigningHubUploadDocument_CollidingDocumentIds_PreservesAllWireValues()
        {
            var model = new SigningHubUploadDocument
            {
                DocumentId = 101,
                Documentid = 102,
                DocumentId2 = 103
            };

            var json = JsonSerializer.Serialize(model);
            var roundTripped = JsonSerializer.Deserialize<SigningHubUploadDocument>(json);

            Assert.IsNotNull(roundTripped);
            Assert.AreEqual(expected: 101, actual: roundTripped.DocumentId);
            Assert.AreEqual(expected: 102, actual: roundTripped.Documentid);
            Assert.AreEqual(expected: 103, actual: roundTripped.DocumentId2);
            StringAssert.Contains(json, "\"documentId\":101");
            StringAssert.Contains(json, "\"documentid\":102");
            StringAssert.Contains(json, "\"document_id\":103");
        }

        [TestMethod]
        public void TwitterModels_CollidingCreatedAtFields_PreserveBothWireValues()
        {
            var tweet = new TwitterTweet
            {
                CreatedAt = "legacy-tweet-time",
                CreatedAtIso = "2026-08-11T12:00:00Z"
            };
            var originalTweet = new TwitterOriginalTweet
            {
                OriginalTweetCreatedAt = "legacy-original-time",
                CreatedAtIso = "2026-08-11T13:00:00Z"
            };

            var tweetJson = JsonSerializer.Serialize(tweet);
            var originalTweetJson = JsonSerializer.Serialize(originalTweet);
            var roundTrippedTweet = JsonSerializer.Deserialize<TwitterTweet>(tweetJson);
            var roundTrippedOriginalTweet = JsonSerializer.Deserialize<TwitterOriginalTweet>(originalTweetJson);

            Assert.IsNotNull(roundTrippedTweet);
            Assert.IsNotNull(roundTrippedOriginalTweet);
            Assert.AreEqual(expected: tweet.CreatedAt, actual: roundTrippedTweet.CreatedAt);
            Assert.AreEqual(expected: tweet.CreatedAtIso, actual: roundTrippedTweet.CreatedAtIso);
            Assert.AreEqual(expected: originalTweet.OriginalTweetCreatedAt, actual: roundTrippedOriginalTweet.OriginalTweetCreatedAt);
            Assert.AreEqual(expected: originalTweet.CreatedAtIso, actual: roundTrippedOriginalTweet.CreatedAtIso);
            StringAssert.Contains(tweetJson, "\"CreatedAt\":\"legacy-tweet-time\"");
            StringAssert.Contains(tweetJson, "\"CreatedAtIso\":\"2026-08-11T12:00:00Z\"");
            StringAssert.Contains(originalTweetJson, "\"CreatedAt\":\"legacy-original-time\"");
            StringAssert.Contains(originalTweetJson, "\"CreatedAtIso\":\"2026-08-11T13:00:00Z\"");
        }

        [TestMethod]
        public void WordPressModels_CollidingIdFields_PreserveBothWireValues()
        {
            var response = new WordPressPostResponse
            {
                Id = 11,
                Id2 = "response-guid"
            };
            var post = new WordPressPost
            {
                Id = 12,
                Id2 = "post-guid"
            };

            var responseJson = JsonSerializer.Serialize(response);
            var postJson = JsonSerializer.Serialize(post);
            var roundTrippedResponse = JsonSerializer.Deserialize<WordPressPostResponse>(responseJson);
            var roundTrippedPost = JsonSerializer.Deserialize<WordPressPost>(postJson);

            Assert.IsNotNull(roundTrippedResponse);
            Assert.IsNotNull(roundTrippedPost);
            Assert.AreEqual(expected: response.Id, actual: roundTrippedResponse.Id);
            Assert.AreEqual(expected: response.Id2, actual: roundTrippedResponse.Id2);
            Assert.AreEqual(expected: post.Id, actual: roundTrippedPost.Id);
            Assert.AreEqual(expected: post.Id2, actual: roundTrippedPost.Id2);
            StringAssert.Contains(responseJson, "\"ID\":11");
            StringAssert.Contains(responseJson, "\"guid\":\"response-guid\"");
            StringAssert.Contains(postJson, "\"ID\":12");
            StringAssert.Contains(postJson, "\"guid\":\"post-guid\"");
        }
    }
}
