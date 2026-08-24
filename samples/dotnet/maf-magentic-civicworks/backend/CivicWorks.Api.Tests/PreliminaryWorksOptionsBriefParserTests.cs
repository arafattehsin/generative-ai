using CivicWorks.Api.Services;
using Xunit;

namespace CivicWorks.Api.Tests;

public sealed class PreliminaryWorksOptionsBriefParserTests
{
    private const string ValidBrief = """
        {
          "recommendation": "C",
          "summary": "Investigate before works design.",
          "options": [
            { "code": "A", "title": "Repair", "assessment": "Incomplete", "supportStatus": "Not supported" },
            { "code": "B", "title": "Renew", "assessment": "Premature", "supportStatus": "Evidence gap" },
            { "code": "C", "title": "Investigate first", "assessment": "Best supported", "supportStatus": "Recommended for review" }
          ],
          "claims": [
            { "claim": "The asset alignment is unresolved.", "evidenceReferences": ["AR-DN-44", "OBS-07"] }
          ],
          "openMatters": ["Heritage significance"]
        }
        """;

    [Fact]
    public void ValidLiveBriefIsParsed()
    {
        var brief = PreliminaryWorksOptionsBriefParser.Parse(ValidBrief);

        Assert.Equal("C", brief.Recommendation);
        Assert.Equal(3, brief.Options.Count);
        Assert.Equal(2, brief.Claims[0].EvidenceReferences.Count);
    }

    [Fact]
    public void UnsupportedModelTextIsRejectedInsteadOfSubstituted()
    {
        InvalidOperationException exception = Assert.Throws<InvalidOperationException>(() =>
            PreliminaryWorksOptionsBriefParser.Parse("I recommend investigating first."));

        Assert.Contains("will not substitute a scripted brief", exception.Message, StringComparison.OrdinalIgnoreCase);
    }

    [Fact]
    public void ClaimsWithoutEvidenceAreRejected()
    {
        string invalid = ValidBrief.Replace(
            "[\"AR-DN-44\", \"OBS-07\"]",
            "[]",
            StringComparison.Ordinal);

        InvalidOperationException exception = Assert.Throws<InvalidOperationException>(() =>
            PreliminaryWorksOptionsBriefParser.Parse(invalid));

        Assert.Contains("evidence references", exception.Message, StringComparison.OrdinalIgnoreCase);
    }
}
