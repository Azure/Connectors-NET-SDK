//------------------------------------------------------------
// Copyright (c) Microsoft Corporation.  All rights reserved.
//------------------------------------------------------------

// NOTE(daviburg): These tests lock the exact name-to-wire mapping for Plumsail Timezone enum
// members that are affected by the normalization collision fix in AzureUX-BPM PR 16971205
// (issue #181). The Plumsail swagger contains both Etc/GMT+N and Etc/GMT-N values for the same N;
// the generator now assigns semantic Plus/Minus member names so both values in each pair are
// readable and distinct (e.g., EtcGMTPlus4 for Etc/GMT+4, EtcGMTMinus4 for Etc/GMT-4).
// Sign characters are expanded to Plus/Minus only when immediately followed by a digit; ordinary
// delimiter hyphens (my-value) are unaffected. Three-way groups (+N/-N/N) resolve similarly:
// EtcGMTPlus0/EtcGMTMinus0/EtcGMT0. Singleton values without a sign counterpart keep their
// natural names (EtcGMT13, EtcGMT14). This file guards against regression on the specific
// affected collision pairs regenerated from the 2026-08-28 ARM swagger snapshot (cache SHA256
// ED9C3FB8911D0F28C8D5038CE33ABEE38F7265D186249C8C66CC5C4B7B7CF2C8, BPM commits d5cb672..87a77e5).

using System.Text.Json;
using Microsoft.VisualStudio.TestTools.UnitTesting;
using PlumsailTimezone = Azure.Connectors.Sdk.Plumsail.Models.Timezone;

namespace Azure.Connectors.Sdk.Tests
{
    [TestClass]
    public class GeneratedEnumMemberTests
    {
        // NOTE(daviburg): UnsafeRelaxedJsonEscaping is required: the default encoder escapes '+'
        // to '\u002B', which would cause wire-value assertions like '"Etc/GMT+4"' to fail.
        private static readonly JsonSerializerOptions RelaxedEscapeOptions = new JsonSerializerOptions
        {
            Encoder = System.Text.Encodings.Web.JavaScriptEncoder.UnsafeRelaxedJsonEscaping,
        };

        [TestMethod]
        public void PlumsailTimezone_EtcGmtPlus4_MapsToEtcGmtPlusFour()
        {
            // NOTE(daviburg): EtcGMTPlus4 carries the Etc/GMT+4 wire string.
            var tz = PlumsailTimezone.EtcGMTPlus4;

            Assert.AreEqual(expected: "Etc/GMT+4", actual: (string)tz);
            var json = JsonSerializer.Serialize(tz, RelaxedEscapeOptions);
            Assert.AreEqual(expected: "\"Etc/GMT+4\"", actual: json);
        }

        [TestMethod]
        public void PlumsailTimezone_EtcGmtMinus4_MapsToEtcGmtMinusFour()
        {
            // NOTE(daviburg): EtcGMTMinus4 carries the Etc/GMT-4 wire string.
            var tz = PlumsailTimezone.EtcGMTMinus4;

            Assert.AreEqual(expected: "Etc/GMT-4", actual: (string)tz);
            var json = JsonSerializer.Serialize(tz, RelaxedEscapeOptions);
            Assert.AreEqual(expected: "\"Etc/GMT-4\"", actual: json);
        }

        [TestMethod]
        public void PlumsailTimezone_EtcGmtMinus6_MapsToEtcGmtMinusSix()
        {
            // NOTE(daviburg): EtcGMTMinus6 carries the Etc/GMT-6 wire string.
            var tz = PlumsailTimezone.EtcGMTMinus6;

            Assert.AreEqual(expected: "Etc/GMT-6", actual: (string)tz);
            var json = JsonSerializer.Serialize(tz, RelaxedEscapeOptions);
            Assert.AreEqual(expected: "\"Etc/GMT-6\"", actual: json);
        }

        [TestMethod]
        public void PlumsailTimezone_CollisionPairEtcGmt4_BothMembersDistinctWireValues()
        {
            var plus = PlumsailTimezone.EtcGMTPlus4;
            var minus = PlumsailTimezone.EtcGMTMinus4;

            Assert.AreNotEqual(notExpected: (string)plus, actual: (string)minus);
            Assert.AreEqual(expected: "Etc/GMT+4", actual: (string)plus);
            Assert.AreEqual(expected: "Etc/GMT-4", actual: (string)minus);
        }

        [TestMethod]
        public void PlumsailTimezone_CollisionPairEtcGmt6_BothMembersDistinctWireValues()
        {
            var plus = PlumsailTimezone.EtcGMTPlus6;
            var minus = PlumsailTimezone.EtcGMTMinus6;

            Assert.AreNotEqual(notExpected: (string)plus, actual: (string)minus);
            Assert.AreEqual(expected: "Etc/GMT+6", actual: (string)plus);
            Assert.AreEqual(expected: "Etc/GMT-6", actual: (string)minus);
        }

        [TestMethod]
        public void PlumsailTimezone_WireValueRoundTrip_PreservesExactStringValue()
        {
            var wireValue = "Etc/GMT+4";

            var deserialized = JsonSerializer.Deserialize<PlumsailTimezone>($"\"{wireValue}\"");

            Assert.AreEqual(expected: wireValue, actual: (string)deserialized);
        }

        [TestMethod]
        public void PlumsailTimezone_EqualsSameWireValue_ReturnsTrue()
        {
            var namedMember = PlumsailTimezone.EtcGMTPlus4;
            var constructedMember = new PlumsailTimezone("Etc/GMT+4");

            Assert.IsTrue(namedMember == constructedMember);
            Assert.IsTrue(namedMember.Equals(constructedMember));
        }

        [TestMethod]
        public void PlumsailTimezone_EqualsDifferentWireValue_ReturnsFalse()
        {
            var plusFour = PlumsailTimezone.EtcGMTPlus4;
            var minusFour = PlumsailTimezone.EtcGMTMinus4;

            Assert.IsTrue(plusFour != minusFour);
            Assert.IsFalse(plusFour.Equals(minusFour));
        }
    }
}
