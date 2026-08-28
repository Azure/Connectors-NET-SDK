//------------------------------------------------------------
// Copyright (c) Microsoft Corporation.  All rights reserved.
//------------------------------------------------------------

// NOTE(daviburg): These tests lock the exact name-to-wire mapping for Plumsail Timezone enum
// members that are affected by the normalization collision fix in AzureUX-BPM PR 16971205
// (issue #181). The Plumsail swagger contains both Etc/GMT+N and Etc/GMT-N values for the same N;
// the pre-fix generator silently dropped the second claimant and also assigned the clean identifier
// to the alphabetically-later -N value. The fix preserves both by allocating a numeric discriminator
// for the second claimant and deterministically assigns the unsuffixed name to the +N value (which
// sorts first since '+' < '-' in ASCII). This file guards against regression on the specific
// affected collision pairs regenerated from the 2026-08-28 ARM swagger snapshot (cache SHA256
// ED9C3FB8911D0F28C8D5038CE33ABEE38F7265D186249C8C66CC5C4B7B7CF2C8, BPM commits d5cb672..07b2718).

using System.Text.Json;
using Microsoft.VisualStudio.TestTools.UnitTesting;
using PlumsailTimezone = Azure.Connectors.Sdk.Plumsail.Models.Timezone;

namespace Azure.Connectors.Sdk.Tests
{
    [TestClass]
    public class GeneratedEnumMemberTests
    {
        private static readonly JsonSerializerOptions RelaxedEscapeOptions = new JsonSerializerOptions
        {
            Encoder = System.Text.Encodings.Web.JavaScriptEncoder.UnsafeRelaxedJsonEscaping,
        };

        [TestMethod]
        public void PlumsailTimezone_EtcGmt4_MapsToEtcGmtPlusFour()
        {
            // NOTE(daviburg): EtcGMT4 previously mapped to "Etc/GMT-4" (the first caller in Swagger
            // order). The fix assigns the unsuffixed name to the alphabetically earlier "+4" wire value.
            var tz = PlumsailTimezone.EtcGMT4;

            Assert.AreEqual(expected: "Etc/GMT+4", actual: (string)tz);
            var json = JsonSerializer.Serialize(tz, RelaxedEscapeOptions);
            Assert.AreEqual(expected: "\"Etc/GMT+4\"", actual: json);
        }

        [TestMethod]
        public void PlumsailTimezone_EtcGmt42_MapsToEtcGmtMinusFour()
        {
            // NOTE(daviburg): EtcGMT42 is the new discriminated member for "Etc/GMT-4", which was
            // previously silently dropped. Both +4 and -4 are now retained and distinct.
            var tz = PlumsailTimezone.EtcGMT42;

            Assert.AreEqual(expected: "Etc/GMT-4", actual: (string)tz);
            var json = JsonSerializer.Serialize(tz, RelaxedEscapeOptions);
            Assert.AreEqual(expected: "\"Etc/GMT-4\"", actual: json);
        }

        [TestMethod]
        public void PlumsailTimezone_EtcGmt62_MapsToEtcGmtMinusSix()
        {
            // NOTE(daviburg): "Etc/GMT-6" was silently dropped at baseline because it normalized
            // to the same identifier as "Etc/GMT+6". The fix recovers it as EtcGMT62.
            var tz = PlumsailTimezone.EtcGMT62;

            Assert.AreEqual(expected: "Etc/GMT-6", actual: (string)tz);
            var json = JsonSerializer.Serialize(tz, RelaxedEscapeOptions);
            Assert.AreEqual(expected: "\"Etc/GMT-6\"", actual: json);
        }

        [TestMethod]
        public void PlumsailTimezone_CollisionPairEtcGmt4_BothMembersDistinctWireValues()
        {
            var plus = PlumsailTimezone.EtcGMT4;
            var minus = PlumsailTimezone.EtcGMT42;

            Assert.AreNotEqual(notExpected: (string)plus, actual: (string)minus);
            Assert.AreEqual(expected: "Etc/GMT+4", actual: (string)plus);
            Assert.AreEqual(expected: "Etc/GMT-4", actual: (string)minus);
        }

        [TestMethod]
        public void PlumsailTimezone_CollisionPairEtcGmt6_BothMembersDistinctWireValues()
        {
            var plus = PlumsailTimezone.EtcGMT6;
            var minus = PlumsailTimezone.EtcGMT62;

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
            var namedMember = PlumsailTimezone.EtcGMT4;
            var constructedMember = new PlumsailTimezone("Etc/GMT+4");

            Assert.IsTrue(namedMember == constructedMember);
            Assert.IsTrue(namedMember.Equals(constructedMember));
        }

        [TestMethod]
        public void PlumsailTimezone_EqualsDifferentWireValue_ReturnsFalse()
        {
            var plusFour = PlumsailTimezone.EtcGMT4;
            var minusFour = PlumsailTimezone.EtcGMT42;

            Assert.IsTrue(plusFour != minusFour);
            Assert.IsFalse(plusFour.Equals(minusFour));
        }
    }
}
