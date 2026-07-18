using NAU.Application.Common;
using NAU.Application.Features.Campaigns;

namespace NAU.UnitTests.Campaigns;

public class SluggerTests
{
    [Theory]
    [InlineData("Library Renovation", "library-renovation")]
    [InlineData("  Flood Relief 2026!  ", "flood-relief-2026")]
    [InlineData("A/B  ---  C", "a-b-c")]
    [InlineData("!!!", "campaign")]
    public void Slugify_produces_url_friendly_slugs(string input, string expected) =>
        Assert.Equal(expected, Slugger.Slugify(input));
}

public class CampaignProgressTests
{
    [Theory]
    [InlineData(0, 1000000, 0)]
    [InlineData(620000, 1000000, 62)]
    [InlineData(1000000, 1000000, 100)]
    [InlineData(1500000, 1000000, 100)] // never exceeds 100%
    [InlineData(500, 0, 0)]             // guard divide-by-zero
    public void Progress_is_clamped_0_to_100(decimal raised, decimal goal, int expected) =>
        Assert.Equal(expected, CampaignTotals.Progress(raised, goal));
}

public class UpsertCampaignValidatorTests
{
    private readonly UpsertCampaignValidator _v = new();

    [Fact]
    public void Goal_must_be_positive_and_end_after_start()
    {
        var start = new DateOnly(2026, 1, 1);
        Assert.False(_v.Validate(new UpsertCampaignDto("T", null, 0, start, null, null)).IsValid);
        Assert.False(_v.Validate(new UpsertCampaignDto("T", null, 1000, start, start.AddDays(-1), null)).IsValid);
        Assert.True(_v.Validate(new UpsertCampaignDto("Library", null, 1000, start, start.AddDays(30), null)).IsValid);
    }
}
