using NAU.Application.Features.Auth;

namespace NAU.UnitTests.Auth;

public class RegisterValidatorTests
{
    private readonly RegisterValidator _validator = new();

    [Theory]
    [InlineData("Sahil Koshriya", "sahil@example.com", "Test@1234", true)]
    [InlineData("", "sahil@example.com", "Test@1234", false)]          // empty name
    [InlineData("Sahil", "not-an-email", "Test@1234", false)]          // bad email
    [InlineData("Sahil", "sahil@example.com", "short1A", false)]       // < 8 chars
    [InlineData("Sahil", "sahil@example.com", "alllowercase1", false)] // no uppercase
    [InlineData("Sahil", "sahil@example.com", "ALLUPPERCASE1", false)] // no lowercase
    [InlineData("Sahil", "sahil@example.com", "NoDigitsHere", false)]  // no digit
    public void Register_password_and_field_policy(string name, string email, string password, bool expectedValid)
    {
        var result = _validator.Validate(new RegisterCommand(name, email, password));
        Assert.Equal(expectedValid, result.IsValid);
    }
}

public class LoginValidatorTests
{
    private readonly LoginValidator _validator = new();

    [Fact]
    public void Requires_email_and_password()
    {
        Assert.False(_validator.Validate(new LoginCommand("", "", null)).IsValid);
        Assert.False(_validator.Validate(new LoginCommand("bad-email", "pw", null)).IsValid);
        Assert.True(_validator.Validate(new LoginCommand("a@b.com", "pw", null)).IsValid);
    }
}

public class ResetPasswordValidatorTests
{
    [Fact]
    public void New_password_must_meet_policy()
    {
        var v = new ResetPasswordValidator();
        Assert.False(v.Validate(new ResetPasswordCommand("a@b.com", "tok", "weak")).IsValid);
        Assert.True(v.Validate(new ResetPasswordCommand("a@b.com", "tok", "Strong@123")).IsValid);
    }
}
