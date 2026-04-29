//------------------------------------------------------------
// Copyright (c) Microsoft Corporation.  All rights reserved.
//------------------------------------------------------------

using System.Collections.Generic;
using System.Text.Json.Serialization;
using System.Threading;
using System.Threading.Tasks;
using Microsoft.Azure.Connectors.Sdk;
using Microsoft.VisualStudio.TestTools.UnitTesting;

namespace Microsoft.Azure.Connectors.Sdk.Tests
{
    /// <summary>
    /// Tests for the ConnectorPageable class.
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

        [TestMethod]
        public async Task GetAsyncEnumerator_EmptyPage_YieldsNoItems()
        {
            // Arrange
            var pageable = new ConnectorPageable<TestPage, TestItem>(
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
            var pageable = new ConnectorPageable<TestPage, TestItem>(
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
            var pageable = new ConnectorPageable<TestPage, TestItem>(
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
            var pageable = new ConnectorPageable<TestPage, TestItem>(
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
            var pageable = new ConnectorPageable<TestPage, TestItem>(
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
            var pages = new List<TestPage>();
            await foreach (var page in pageable.AsPages().ConfigureAwait(continueOnCapturedContext: false))
            {
                pages.Add(page);
            }

            // Assert
            Assert.AreEqual(2, pages.Count);
            Assert.AreEqual("https://api.contoso.com/next?page=2", pages[0].NextLink);
            Assert.IsNull(pages[1].NextLink);
            Assert.AreEqual(1, pages[0].Value.Count);
            Assert.AreEqual(1, pages[1].Value.Count);
        }

        [TestMethod]
        public void Constructor_NullFirstPageFunc_Throws()
        {
            // Arrange & Act & Assert
            Assert.ThrowsExactly<ArgumentNullException>(() =>
                new ConnectorPageable<TestPage, TestItem>(null!, (nextLink, ct) => Task.FromResult<TestPage>(null!)));
        }

        [TestMethod]
        public void Constructor_NullNextPageFunc_Throws()
        {
            // Arrange & Act & Assert
            Assert.ThrowsExactly<ArgumentNullException>(() =>
                new ConnectorPageable<TestPage, TestItem>(ct => Task.FromResult<TestPage>(null!), null!));
        }

        [TestMethod]
        public async Task GetAsyncEnumerator_CancellationToken_IsPassed()
        {
            // Arrange
            CancellationToken receivedToken = default;
            using var cts = new CancellationTokenSource();

            var pageable = new ConnectorPageable<TestPage, TestItem>(
                ct =>
                {
                    receivedToken = ct;
                    return Task.FromResult(new TestPage { Value = new List<TestItem>(), NextLink = null });
                },
                (nextLink, ct) => Task.FromResult<TestPage>(null!));

            // Act
            var enumerator = pageable.GetAsyncEnumerator(cts.Token);
            while (await enumerator.MoveNextAsync().ConfigureAwait(continueOnCapturedContext: false))
            {
                // No items expected
            }

            // Assert
            Assert.AreEqual(cts.Token, receivedToken);
        }
    }
}
