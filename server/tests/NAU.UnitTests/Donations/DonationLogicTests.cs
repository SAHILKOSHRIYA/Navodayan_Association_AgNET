using NAU.Application.Features.Donations;

namespace NAU.UnitTests.Donations;

public class FinancialYearTests
{
    [Theory]
    [InlineData("2026-04-01", "2026-27")]
    [InlineData("2026-12-31", "2026-27")]
    [InlineData("2027-03-31", "2026-27")]
    [InlineData("2027-04-01", "2027-28")]
    [InlineData("2026-01-15", "2025-26")]
    public void FinancialYear_uses_indian_april_boundary(string date, string expected) =>
        Assert.Equal(expected, DonationCapture.FinancialYear(
            DateTime.SpecifyKind(DateTime.Parse(date), DateTimeKind.Utc)));
}

public class CreateDonationValidatorTests
{
    private readonly CreateDonationValidator _v = new();

    private static CreateDonationOrderCommand Cmd(decimal amount, string name = "Rahul", string email = "r@example.com") =>
        new(null, new CreateDonationDto(Guid.NewGuid(), amount, name, email, false));

    [Theory]
    [InlineData(0, false)]
    [InlineData(0.5, false)]
    [InlineData(1, true)]
    [InlineData(500, true)]
    [InlineData(20_000_000, false)] // above sane cap
    public void Amount_bounds_enforced(decimal amount, bool valid) =>
        Assert.Equal(valid, _v.Validate(Cmd(amount)).IsValid);

    [Fact]
    public void Donor_name_and_valid_email_required()
    {
        Assert.False(_v.Validate(Cmd(100, name: "")).IsValid);
        Assert.False(_v.Validate(Cmd(100, email: "not-an-email")).IsValid);
    }
}
