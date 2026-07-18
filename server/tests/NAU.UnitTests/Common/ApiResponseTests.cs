using NAU.Application.Common.Models;

namespace NAU.UnitTests.Common;

public class ApiResponseTests
{
    [Fact]
    public void Ok_wraps_data_and_sets_success()
    {
        var response = ApiResponse<string>.Ok("hello", "done");

        Assert.True(response.Success);
        Assert.Equal("hello", response.Data);
        Assert.Equal("done", response.Message);
        Assert.Null(response.Errors);
    }

    [Fact]
    public void Fail_carries_message_and_errors()
    {
        var errors = new[] { new ApiError("email", "EMAIL_TAKEN", "Email already registered.") };

        var response = ApiResponse<object>.Fail("Validation failed.", errors);

        Assert.False(response.Success);
        Assert.Null(response.Data);
        Assert.Equal("Validation failed.", response.Message);
        Assert.Single(response.Errors!);
    }
}

public class PagedResultTests
{
    [Theory]
    [InlineData(100, 20, 5)]
    [InlineData(101, 20, 6)]
    [InlineData(0, 20, 0)]
    public void TotalPages_is_computed_from_count_and_page_size(int total, int pageSize, int expectedPages)
    {
        var result = new PagedResult<int>([], 1, pageSize, total);

        Assert.Equal(expectedPages, result.TotalPages);
    }
}
