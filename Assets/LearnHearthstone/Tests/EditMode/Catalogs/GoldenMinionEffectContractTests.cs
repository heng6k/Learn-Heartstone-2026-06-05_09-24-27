using System.IO;
using System.Text.RegularExpressions;
using NUnit.Framework;

namespace LearnHearthstone.Tests.EditMode
{
    public sealed class GoldenMinionEffectContractTests
    {
        [Test]
        public void Ledger_CoversEverySoloMinionWithDifferentGoldenRulesText()
        {
            const string path = "Docs/data/golden-minion-effect-contracts.json";
            Assert.IsTrue(File.Exists(path));
            var json = File.ReadAllText(path);

            Assert.AreEqual(237, Regex.Matches(json, "\\\"cardId\\\"\\s*:").Count);
            StringAssert.Contains("\"count\": 237", json);
            StringAssert.DoesNotContain("\"implementationStatus\": \"NeedsImplementation\"", json);
            StringAssert.Contains("\"cardId\": \"BG33_825\"", json);
            StringAssert.Contains("\"cardId\": \"BG34_922\"", json);
            StringAssert.Contains("\"cardId\": \"BG30_121\"", json);
            StringAssert.Contains("\"cardId\": \"BG32_235\"", json);
            StringAssert.Contains("\"cardId\": \"BG21_018\"", json);
        }
    }
}
