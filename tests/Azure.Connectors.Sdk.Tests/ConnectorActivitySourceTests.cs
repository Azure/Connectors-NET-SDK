//------------------------------------------------------------
// Copyright (c) Microsoft Corporation.  All rights reserved.
//------------------------------------------------------------

using System;
using System.Diagnostics;
using System.Net;
using System.Net.Http;
using System.Text.Json;
using System.Threading;
using System.Threading.Tasks;
using Azure.Connectors.Sdk.Office365;
using Azure.Connectors.Sdk.Office365.Models;
using global::Azure.Core;
using global::Azure.Core.Pipeline;
using Microsoft.VisualStudio.TestTools.UnitTesting;

namespace Azure.Connectors.Sdk.Tests
{
    /// <summary>
    /// Validates that the generated per-connector ActivitySource tracing wrapper
    /// emits operation-level spans on success and sets error status on failure.
    /// </summary>
    [TestClass]
    public class ConnectorActivitySourceTests
    {
        [TestMethod]
        public async Task GeneratedMethod_WithActivityListener_EmitsOperationSpan()
        {
            // Arrange
            var capturedActivities = new System.Collections.Generic.List<Activity>();

            using var listener = new ActivityListener
            {
                ShouldListenTo = source => string.Equals(source.Name, "Azure.Connectors.Sdk.office365", StringComparison.Ordinal),
                Sample = (ref ActivityCreationOptions<ActivityContext> _) => ActivitySamplingResult.AllDataAndRecorded,
                ActivityStopped = activity => capturedActivities.Add(activity),
            };

            ActivitySource.AddActivityListener(listener);

            var (credential, options) = ConnectorTestHelpers.CreateMockedClientSetup(() =>
                new HttpResponseMessage(HttpStatusCode.OK)
                {
                    Content = new StringContent(
                        JsonSerializer.Serialize(new System.Collections.Generic.List<GraphOutlookCategory>()),
                        System.Text.Encoding.UTF8,
                        mediaType: "application/json"),
                });

            using var client = new Office365Client(
                connectionRuntimeUrl: new Uri("https://test.azure.com/connection"),
                credential: credential,
                options: options);

            // Act
            await client
                .GetOutlookCategoryNamesAsync(cancellationToken: CancellationToken.None)
                .ConfigureAwait(continueOnCapturedContext: false);

            // Assert — the generated tracing wrapper should emit an operation span
            Assert.IsTrue(capturedActivities.Count >= 1, "Expected at least one Activity from the connector ActivitySource.");

            var operationActivity = capturedActivities.Find(a =>
                string.Equals(a.DisplayName, "Office365Client.GetOutlookCategoryNamesAsync", StringComparison.Ordinal));
            Assert.IsNotNull(operationActivity, "Expected an Activity named 'Office365Client.GetOutlookCategoryNamesAsync'.");
            Assert.AreNotEqual(ActivityStatusCode.Error, operationActivity.Status);
        }

        [TestMethod]
        public async Task GeneratedMethod_OnException_SetsErrorStatus()
        {
            // Arrange
            var capturedActivities = new System.Collections.Generic.List<Activity>();

            using var listener = new ActivityListener
            {
                ShouldListenTo = source => string.Equals(source.Name, "Azure.Connectors.Sdk.office365", StringComparison.Ordinal),
                Sample = (ref ActivityCreationOptions<ActivityContext> _) => ActivitySamplingResult.AllDataAndRecorded,
                ActivityStopped = activity => capturedActivities.Add(activity),
            };

            ActivitySource.AddActivityListener(listener);

            var (credential, options) = ConnectorTestHelpers.CreateMockedClientSetup(() =>
                new HttpResponseMessage(HttpStatusCode.InternalServerError)
                {
                    Content = new StringContent("Internal Server Error"),
                });

            using var client = new Office365Client(
                connectionRuntimeUrl: new Uri("https://test.azure.com/connection"),
                credential: credential,
                options: options);

            // Act & Assert — the call should throw, and the span should have error status
            Exception? caught = null;
            try
            {
                await client
                    .GetOutlookCategoryNamesAsync(cancellationToken: CancellationToken.None)
                    .ConfigureAwait(continueOnCapturedContext: false);
            }
            catch (Exception ex)
            {
                caught = ex;
            }

            Assert.IsNotNull(caught, "Expected an exception from the 500 response.");
            Assert.IsTrue(capturedActivities.Count >= 1, "Expected at least one Activity from the connector ActivitySource.");

            var operationActivity = capturedActivities.Find(a =>
                string.Equals(a.DisplayName, "Office365Client.GetOutlookCategoryNamesAsync", StringComparison.Ordinal));
            Assert.IsNotNull(operationActivity, "Expected an Activity named 'Office365Client.GetOutlookCategoryNamesAsync'.");
            Assert.AreEqual(ActivityStatusCode.Error, operationActivity.Status);
        }
    }
}
