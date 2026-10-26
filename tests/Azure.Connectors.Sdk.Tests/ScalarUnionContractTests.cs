//------------------------------------------------------------
// Copyright (c) Microsoft Corporation.  All rights reserved.
//------------------------------------------------------------

using System;
using System.Collections.Generic;
using System.Net;
using System.Net.Http;
using System.Text.Json;
using System.Threading.Tasks;
using Azure.Connectors.Sdk.Scalarunionfixture;
using Azure.Connectors.Sdk.Scalarunionfixture.Models;
using Microsoft.VisualStudio.TestTools.UnitTesting;

namespace Azure.Connectors.Sdk.Tests
{
    [TestClass]
    public class ScalarUnionContractTests
    {
        [TestMethod]
        [Owner("daviburg")]
        public async Task UpdateMapsAsync_MixedScalars_PreservesRequestAndResponseTypes()
        {
            const string responseJson = """{"scalar-map":{"text":"sample","integer":1,"number":1.5,"boolean":true},"nullableMap":{"nil":null}}""";
            var (credential, options, capture) = ConnectorTestHelpers.CreateContentCapturingClientSetup(
                responseFactory: () => new HttpResponseMessage(HttpStatusCode.OK) { Content = new StringContent(responseJson) });
            using var client = new ScalarunionfixtureClient(
                connectionRuntimeUrl: new Uri("https://example.invalid/connection"),
                credential: credential,
                options: options);
            var input = new Envelope
            {
                ScalarMap = new Dictionary<string, ScalarStringNumberBooleanValue>
                {
                    ["text"] = "sample",
                    ["integer"] = 1,
                    ["number"] = 1.5,
                    ["boolean"] = true,
                },
            };

            var response = await client.UpdateMapsAsync(input)
                .ConfigureAwait(continueOnCapturedContext: false);

            Assert.IsNotNull(capture.Body);
            using var request = JsonDocument.Parse(capture.Body);
            var sent = request.RootElement.GetProperty("scalar-map");
            Assert.AreEqual(expected: JsonValueKind.String, actual: sent.GetProperty("text").ValueKind);
            Assert.AreEqual(expected: JsonValueKind.Number, actual: sent.GetProperty("integer").ValueKind);
            Assert.AreEqual(expected: 1.5, actual: sent.GetProperty("number").GetDouble());
            Assert.AreEqual(expected: JsonValueKind.True, actual: sent.GetProperty("boolean").ValueKind);
            Assert.AreEqual(expected: "sample", actual: response.ScalarMap["text"].Value.GetString());
            Assert.AreEqual(expected: 1, actual: response.ScalarMap["integer"].Value.GetInt32());
            Assert.AreEqual(expected: 1.5, actual: response.ScalarMap["number"].Value.GetDouble());
            Assert.IsTrue(condition: response.ScalarMap["boolean"].Value.GetBoolean());
            Assert.AreEqual(expected: JsonValueKind.Null, actual: response.NullableMap["nil"].Value.ValueKind);
        }

        [TestMethod]
        [Owner("daviburg")]
        public void ScalarMap_EmptyAndMixedMaps_RoundTripsWithoutCoercion()
        {
            var empty = JsonSerializer.Deserialize<Envelope>("""{"scalar-map":{}}""");
            Assert.IsNotNull(empty);
            Assert.HasCount(expected: 0, collection: empty.ScalarMap);
            var integerValues = JsonSerializer.Deserialize<Envelope>("""{"scalar_map":{"whole":1.0,"large":1e100,"zero":0e-100}}""");
            Assert.IsNotNull(integerValues);
            Assert.HasCount(expected: 3, collection: integerValues.ScalarMap2);
            var structured = JsonSerializer.Deserialize<Envelope>("""{"structuredMap":{"item":{"wire-name":"sample"}},"emptyObject":{"nested":[1]}}""");
            Assert.IsNotNull(structured);
            Assert.AreEqual(expected: "sample", actual: structured.StructuredMap["item"].WireName);
            Assert.AreEqual(expected: 1, actual: structured.EmptyObject!.Value.GetProperty("nested")[0].GetInt32());
        }

        [TestMethod]
        [DataRow("{\"scalar-map\":{\"invalid\":{}}}")]
        [DataRow("{\"scalar-map\":{\"invalid\":[]}}")]
        [DataRow("{\"scalar-map\":{\"invalid\":null}}")]
        [DataRow("{\"scalar_map\":{\"invalid\":1.5}}")]
        [DataRow("{\"nullableAnyOf\":{\"invalid\":true}}")]
        [Owner("daviburg")]
        public void ScalarMap_UndeclaredValue_ThrowsJsonException(string payload)
        {
            Assert.ThrowsExactly<JsonException>(() => JsonSerializer.Deserialize<Envelope>(payload));
        }

        [TestMethod]
        [Owner("daviburg")]
        public void ScalarValue_DefaultAndUndeclaredNull_RejectsSerialization()
        {
            Assert.ThrowsExactly<JsonException>(() => JsonSerializer.Serialize(default(ScalarStringNumberBooleanValue)));
            Assert.ThrowsExactly<JsonException>(() =>
            {
                ScalarStringNumberBooleanValue value = (string)null!;
                return JsonSerializer.Serialize(value);
            });
        }

        [TestMethod]
        [Owner("daviburg")]
        public void DynamicMaps_MixedStructuredValues_RemainOpen()
        {
            const string values = """{"text":"sample","number":1.5,"flag":true,"nil":null,"object":{"nested":1},"array":[1]}""";
            var sql = JsonSerializer.Deserialize<Sql.Models.SqlPassThroughNativeQueryBody>("{\"actualParameters\":" + values + "}");
            var dynamics = JsonSerializer.Deserialize<DynamicsAX.Models.AxOnlineProcedureResult>("{\"OutputParameters\":" + values + "}");
            Assert.IsNotNull(sql);
            Assert.IsNotNull(dynamics);
            Assert.AreEqual(expected: values, actual: sql.ActualParameters!.Value.GetRawText());
            Assert.AreEqual(expected: values, actual: dynamics.OutputParameters!.Value.GetRawText());
        }
    }
}
