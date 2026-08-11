//------------------------------------------------------------
// Copyright (c) Microsoft Corporation.  All rights reserved.
//------------------------------------------------------------

using System.Globalization;
using System.Net;
using System.Net.Http;
using System.Text;
using System.Text.Json;
using System.Text.Json.Serialization;
using Azure.Connectors.Sdk.Etsy;
using Microsoft.VisualStudio.TestTools.UnitTesting;

namespace Azure.Connectors.Sdk.Tests
{
    [TestClass]
    [DoNotParallelize]
    public class InvariantWireFormattingTests
    {
        [TestMethod]
        public async Task ListingGetActiveAsync_NonInvariantCulture_FormatsDecimalInvariantly()
        {
            var originalCulture = CultureInfo.CurrentCulture;
            try
            {
                CultureInfo.CurrentCulture = CultureInfo.GetCultureInfo("de-DE");
                var (credential, options, getLastRequest) = ConnectorTestHelpers.CreateCapturingClientSetup(
                    () => new HttpResponseMessage(HttpStatusCode.OK)
                    {
                        Content = new StringContent("{}"),
                    });
                using var client = new EtsyClient(
                    connectionRuntimeUrl: new Uri("https://test.azure.com/connection"),
                    credential: credential,
                    options: options);

                await client
                    .ListingGetActiveAsync(
                        minimumPrice: 3.14,
                        cancellationToken: CancellationToken.None)
                    .ConfigureAwait(continueOnCapturedContext: false);

                var request = getLastRequest();
                Assert.IsNotNull(request);
                var requestUri = request.RequestUri;
                Assert.IsNotNull(requestUri);
                StringAssert.Contains(requestUri.Query, "min_price=3.14");
                Assert.IsFalse(requestUri.Query.Contains("3%2C14", StringComparison.Ordinal));
            }
            finally
            {
                CultureInfo.CurrentCulture = originalCulture;
            }
        }

        [TestMethod]
        public void Iso8601DateTimeConverter_NonInvariantCulture_RoundTripsInvariantly()
        {
            var originalCulture = CultureInfo.CurrentCulture;
            try
            {
                CultureInfo.CurrentCulture = CultureInfo.GetCultureInfo("ar-SA");
                var converterType = typeof(ConnectorClientBase).Assembly.GetType(
                    "Azure.Connectors.Sdk.Serialization.Iso8601DateTimeConverter",
                    throwOnError: true)!;
                var converter = (JsonConverter<DateTime>)Activator.CreateInstance(converterType)!;
                var value = new DateTime(
                    year: 2026,
                    month: 8,
                    day: 11,
                    hour: 12,
                    minute: 34,
                    second: 56,
                    millisecond: 789,
                    kind: DateTimeKind.Utc);
                using var stream = new MemoryStream();
                using (var writer = new Utf8JsonWriter(stream))
                {
                    converter.Write(writer, value, new JsonSerializerOptions());
                }

                var json = Encoding.UTF8.GetString(stream.ToArray());
                Assert.AreEqual(expected: "\"2026-08-11T12:34:56.789Z\"", actual: json);

                var reader = new Utf8JsonReader(Encoding.UTF8.GetBytes(json));
                Assert.IsTrue(reader.Read());
                var roundTripped = converter.Read(ref reader, typeof(DateTime), new JsonSerializerOptions());
                Assert.AreEqual(expected: value, actual: roundTripped);
            }
            finally
            {
                CultureInfo.CurrentCulture = originalCulture;
            }
        }
    }
}
