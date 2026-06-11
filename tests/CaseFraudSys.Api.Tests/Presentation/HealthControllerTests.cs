using CaseFraudSys.Api.Application.DTOs;
using CaseFraudSys.Api.Infrastructure.Utils;
using CaseFraudSys.Api.Presentation.Controllers;
using Microsoft.AspNetCore.Mvc;
using Microsoft.Extensions.Logging.Abstractions;

namespace CaseFraudSys.Api.Tests.Presentation;

[Trait("Category", "Unit")]
public class HealthControllerTests
{
    [Fact]
    public void Get_ReturnsHealthyResponse()
    {
        var controller = new HealthController(NullLogger<HealthController>.Instance);

        var result = controller.Get();

        var ok = Assert.IsType<OkObjectResult>(result.Result);
        var body = Assert.IsType<ApiResponse<HealthStatusResponse>>(ok.Value);
        Assert.True(body.Success);
        Assert.Equal("healthy", body.Data!.Status);
        Assert.Equal("CaseFraudSys", body.Data.Service);
    }
}
