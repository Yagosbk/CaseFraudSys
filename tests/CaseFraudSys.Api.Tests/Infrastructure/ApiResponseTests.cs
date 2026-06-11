using CaseFraudSys.Api.Infrastructure.Utils;

namespace CaseFraudSys.Api.Tests.Infrastructure;

[Trait("Category", "Unit")]
public class ApiResponseTests
{
    [Fact]
    public void Ok_SetsSuccessAndData()
    {
        var response = ApiResponse<string>.Ok("payload", "mensagem");

        Assert.True(response.Success);
        Assert.Equal("payload", response.Data);
        Assert.Equal("mensagem", response.Message);
    }
}
