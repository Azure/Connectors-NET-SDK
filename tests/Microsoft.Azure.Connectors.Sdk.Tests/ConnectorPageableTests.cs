//------------------------------------------------------------
// Copyright (c) Microsoft Corporation.  All rights reserved.
//------------------------------------------------------------

using System.Collections.Generic;
using System.Text.Json.Serialization;
using System.Threading;
using System.Threading.Tasks;
using Azure;
using Microsoft.Azure.Connectors.Sdk;
using Microsoft.VisualStudio.TestTools.UnitTesting;

namespace Microsoft.Azure.Connectors.Sdk.Tests
{
    /// <summary>
    /// Tests for the AsyncPageable pagination infrastructure (CreatePageable).
    /// </summary>
    [TestClass]
    public class ConnectorPageableTests
    {
        private class TestItem
        {
            public string? Id { get; set; }
        }

        private class TestPage : IPageable<TestItem>
        {
            [JsonPropertyName("value")]
            public List<TestItem> Value { get; set; } = new();

            [JsonPropertyName("nextLink")]
            public string? NextLink { get; set; }
        }

        /// <summary>
        /// Minimal test client that exposes CreatePageable for testing.
        /// </summary>
        private class TestConnectorClient : ConnectorClientBase
        {
            public override string ConnectorName => "test";

            public AsyncPageable<TestItem> GetItemsAsync(
                Func<CancellationToken, Task<TestPage>> firstPageFunc,
                Func<string, CancellationToken, Task<TestPage>> nextPageFunc,
                CancellationToken cancellationToken = default)
            {
                return this.CreatePageable<TestPage, TestItem>(firstPageFunc, nextPageFunc, cancellationToken);
            }
        }

        [TestMethod]
        public async Task GetAsyncEnumerator_EmptyPage_YieldsNoItems()
        {
            // Arrange
            using var client = new TestConnectorClient();
            var pageable = client.GetItemsAsync(
                ct => Task.FromResult(new TestPage { Value = new List<TestItem>(), NextLink = null }),
                (nextLink, ct) => Task.FromResult<TestPage>(null!));

            // Act
            var items = new List<TestItem>();
            await foreach (var item in pageable.ConfigureAwait(continueOnCapturedContext: false))
            {
                items.Add(item);
            }

            // Assert
            Assert.AreEqual(0, items.Count);
        }

        [TestMethod]
        public async Task GetAsyncEnumerator_NullValue_YieldsNoItems()
        {
            // Arrange
            using var client = new TestConnectorClient();
            var pageable = client.GetItemsAsync(
                ct => Task.FromResult(new TestPage { Value = null!, NextLink = null }),
                (nextLink, ct) => Task.FromResult<TestPage>(null!));

            // Act
            var items = new List<TestItem>();
            await foreach (var item in pageable.ConfigureAwait(continueOnCapturedContext: false))
            {
                items.Add(item);
            }

            // Assert
            Assert.AreEqual(0, items.Count);
        }

        [TestMethod]
        public async Task GetAsyncEnumerator_SinglePage_YieldsAllItems()
        {
            // Arrange
            using var client = new TestConnectorClient();
            var pageable = client.GetItemsAsync(
                ct => Task.FromResult(new TestPage
                {
                    Value = new List<TestItem>
                    {
                        new TestItem { Id = "1" },
                        new TestItem { Id = "2" },
                        new TestItem { Id = "3" }
                    },
                    NextLink = null
                }),
                (nextLink, ct) => Task.FromResult<TestPage>(null!));

            // Act
            var items = new List<TestItem>();
            await foreach (var item in pageable.ConfigureAwait(continueOnCapturedContext: false))
            {
                items.Add(item);
            }

            // Assert
            Assert.AreEqual(3, items.Count);
            Assert.AreEqual("1", items[0].Id);
            Assert.AreEqual("2", items[1].Id);
            Assert.AreEqual("3", items[2].Id);
        }

        [TestMethod]
        public async Task GetAsyncEnumerator_MultiplePages_FollowsNextLinks()
        {
            // Arrange
            var callCount = 0;
            using var client = new TestConnectorClient();
            var pageable = client.GetItemsAsync(
                ct =>
                {
                    callCount++;
                    return Task.FromResult(new TestPage
                    {
                        Value = new List<TestItem> { new TestItem { Id = "1" } },
                        NextLink = "https://api.contoso.com/next?page=2"
                    });
                },
                (nextLink, ct) =>
                {
                    callCount++;
                    if (nextLink == "https://api.contoso.com/next?page=2")
                    {
                        return Task.FromResult(new TestPage
                        {
                            Value = new List<TestItem> { new TestItem { Id = "2" } },
                            NextLink = "https://api.contoso.com/next?page=3"
                        });
                    }

                    return Task.FromResult(new TestPage
                    {
                        Value = new List<TestItem> { new TestItem { Id = "3" } },
                        NextLink = null
                    });
                });

            // Act
            var items = new List<TestItem>();
            await foreach (var item in pageable.ConfigureAwait(continueOnCapturedContext: false))
            {
                items.Add(item);
            }

            // Assert
            Assert.AreEqual(3, items.Count);
            Assert.AreEqual("1", items[0].Id);
            Assert.AreEqual("2", items[1].Id);
            Assert.AreEqual("3", items[2].Id);
            Assert.AreEqual(3, callCount);
        }

        [TestMethod]
        public async Task AsPages_YieldsPageObjects()
        {
            // Arrange
            using var client = new TestConnectorClient();
            var pageable = client.GetItemsAsync(
                ct => Task.FromResult(new TestPage
                {
                    Value = new List<TestItem> { new TestItem { Id = "1" } },
                    NextLink = "https://api.contoso.com/next?page=2"
                }),
                (nextLink, ct) => Task.FromResult(new TestPage
                {
                    Value = new List<TestItem> { new TestItem { Id = "2" } },
                    NextLink = null
                }));

            // Act
            var pages = new List<Page<TestItem>>();
            await foreach (var page in pageable.AsPages().ConfigureAwait(continueOnCapturedContext: false))
            {
                pages.Add(page);
            }

            // Assert
            Assert.AreEqual(2, pages.Count);
            Assert.AreEqual("https://api.contoso.com/next?page=2", pages[0].ContinuationToken);
            Assert.IsNull(pages[1].ContinuationToken);
            Assert.AreEqual(1, pages[0].Values.Count);
            Assert.AreEqual(1, pages[1].Values.Count);
        }

        [TestMethod]
        public async Task GetAsyncEnumerator_WithCancellation_PassesTokenToFetchCalls()
        {
            // Arrange
            CancellationToken receivedToken = default;
            using var cts = new CancellationTokenSource();
            using var client = new TestConnectorClient();

            var pageable = client.GetItemsAsync(
                ct =>
                {
                    receivedToken = ct;
                    return Task.FromResult(new TestPage { Value = new List<TestItem>(), NextLink = null });
                },
                (nextLink, ct) => Task.FromResult<TestPage>(null!));

            // Act
            await foreach (var _ in pageable
                .WithCancellation(cts.Token)
                .ConfigureAwait(continueOnCapturedContext: false))
            {
                // No items expected
            }

            // Assert
            Assert.IsTrue(receivedToken.CanBeCanceled);
        }

        [TestMethod]
        public async Task GetAsyncEnumerator_MethodLevelToken_PassedToFetchCalls()
        {
            // Arrange
            CancellationToken receivedToken = default;
            using var cts = new CancellationTokenSource();
            using var client = new TestConnectorClient();

            var pageable = client.GetItemsAsync(
                ct =>
                {
                    receivedToken = ct;
                    return Task.FromResult(new TestPage { Value = new List<TestItem>(), NextLink = null });
                },
                (nextLink, ct) => Task.FromResult<TestPage>(null!),
                cts.Token);

            // Act
            await foreach (var _ in pageable.ConfigureAwait(continueOnCapturedContext: false))
            {
                // No items expected
            }

            // Assert
            Assert.IsTrue(receivedToken.CanBeCanceled);
        }

        [TestMethod]
        public void CreatePageable_ReturnsAsyncPageable()
        {
            // Arrange
            using var client = new TestConnectorClient();

            // Act
            var pageable = client.GetItemsAsync(
                ct => Task.FromResult(new TestPage()),
                (nextLink, ct) => Task.FromResult<TestPage>(null!));

            // Assert
            Assert.IsInstanceOfType<AsyncPageable<TestItem>>(pageable);
        }

        [TestMethod]
        public async Task AsPages_WithContinuationToken_ResumesFromToken()
        {
            // Arrange
            using var client = new TestConnectorClient();
            var pageable = client.GetItemsAsync(
                ct => Task.FromResult(new TestPage
                {
                    Value = new List<TestItem> { new TestItem { Id = "1" } },
                    NextLink = "https://api.contoso.com/next?page=2"
                }),
                (nextLink, ct) => Task.FromResult(new TestPage
                {
                    Value = new List<TestItem> { new TestItem { Id = "2" } },
                    NextLink = null
                }));

            // Act — resume from a continuation token, skipping first page
            var pages = new List<Page<TestItem>>();
            await foreach (var page in pageable
                .AsPages(continuationToken: "https://api.contoso.com/next?page=2")
                .ConfigureAwait(continueOnCapturedContext: false))
            {
                pages.Add(page);
            }

            // Assert — should get only the second page
            Assert.AreEqual(1, pages.Count);
            Assert.AreEqual("2", pages[0].Values[0].Id);
            Assert.IsNull(pages[0].ContinuationToken);
        }
    }
}
